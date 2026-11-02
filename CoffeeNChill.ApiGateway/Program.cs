
using CoffeeNChill.ApiGateway.Interfaces;
using CoffeeNChill.ApiGateway.Middleware;
using CoffeeNChill.ApiGateway.Models;
using CoffeeNChill.ApiGateway.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// STEP 1: REGISTER API CONTROLLERS
// =====================================================
//
// Enables all existing controllers, including:
//
// - Authentication
// - Menu management
// - Order management
// - Document management
// - Staff invitations
//
// Also discovers the new StaffManagementController.
// =====================================================

builder.Services.AddControllers();

// =====================================================
// STEP 2: REGISTER AZURE TABLE STORAGE SERVICES
// =====================================================
//
// UserTableService supports:
//
// - Customer registration
// - Invited Staff registration
// - User retrieval
// - Staff account listing
// - Staff account status management
// =====================================================

builder.Services.AddSingleton<
    IUserTableService,
    UserTableService>();

// =====================================================
// STEP 2A: REGISTER STAFF INVITATION STORAGE
// =====================================================
//
// Preserve the existing invitation storage service.
// =====================================================

builder.Services.AddSingleton<
    IStaffInvitationTableService,
    StaffInvitationTableService>();

// =====================================================
// STEP 2B: REGISTER STAFF INVITATION SERVICE
// =====================================================
//
// Preserve invitation creation, listing,
// revocation and acceptance.
// =====================================================

builder.Services.AddScoped<
    IStaffInvitationService,
    StaffInvitationService>();

// =====================================================
// STEP 3: REGISTER AUTHENTICATION SERVICES
// =====================================================

builder.Services.AddScoped<
    IAuthService,
    AuthService>();

builder.Services.AddScoped<
    IJwtTokenService,
    JwtTokenService>();

builder.Services.AddScoped<
    IPasswordHasher<UserEntity>,
    PasswordHasher<UserEntity>>();

// =====================================================
// STEP 4: REGISTER AZURE FUNCTIONS HTTP CLIENT
// =====================================================
//
// Preserve the existing named HTTP client.
//
// The base URL must match the Azure Functions
// host address configured in appsettings.
// =====================================================

string functionsBaseUrl =
    builder.Configuration["Functions:BaseUrl"]
    ?? throw new InvalidOperationException(
        "Functions:BaseUrl is missing.");

if (!Uri.TryCreate(
        functionsBaseUrl,
        UriKind.Absolute,
        out Uri? functionsUri)
    || (functionsUri.Scheme != Uri.UriSchemeHttp
        && functionsUri.Scheme != Uri.UriSchemeHttps))
{
    throw new InvalidOperationException(
        "Functions:BaseUrl must be a valid HTTP or HTTPS URL.");
}

Uri functionsBaseAddress =
    new Uri(
        functionsUri.AbsoluteUri.TrimEnd('/') + "/");

builder.Services.AddHttpClient(
    "AzureFunctions",
    client =>
    {
        client.BaseAddress = functionsBaseAddress;

        client.Timeout = TimeSpan.FromSeconds(60);
    });

// =====================================================
// STEP 5: READ JWT CONFIGURATION
// =====================================================

string jwtIssuer =
    builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "Jwt:Issuer is missing.");

string jwtAudience =
    builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "Jwt:Audience is missing.");

string jwtKey =
    builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Jwt:Key is missing.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "JWT signing key must be at least 32 bytes.");
}

// =====================================================
// STEP 6: CONFIGURE JWT AUTHENTICATION
// =====================================================
//
// Standard JWT validation checks:
//
// 1. Issuer
// 2. Audience
// 3. Signing key
// 4. Expiration
//
// Additional POE Part 3 validation:
//
// 5. User still exists
// 6. User registration is still active
// 7. User account has not been disabled
// 8. JWT role matches the stored account role
//
// This ensures that disabling a Staff account
// also invalidates previously issued JWTs
// for subsequent authenticated requests.
// =====================================================

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Preserve the existing claim mapping.
        options.MapInboundClaims = true;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                ValidateLifetime = true,

                // Reject expired tokens without
                // the default clock-skew allowance.
                ClockSkew = TimeSpan.Zero
            };

        // =============================================
        // NEW: VALIDATE ACCOUNT STATUS AFTER JWT
        // =============================================
        //
        // The token signature is validated first.
        //
        // Next, the current user record is retrieved
        // from Azure Table Storage.
        //
        // This prevents a disabled Staff member
        // from continuing to use an old JWT.
        //
        // Account status is never trusted solely
        // from claims stored inside the token.
        // =============================================

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                ClaimsPrincipal? principal =
                    context.Principal;

                if (principal == null)
                {
                    context.Fail(
                        "Authenticated principal is missing.");

                    return;
                }

                // The JWT generator uses ClaimTypes.Email
                // and ClaimTypes.Role.
                string? email =
                    principal.FindFirst(
                        ClaimTypes.Email)?.Value;

                string? role =
                    principal.FindFirst(
                        ClaimTypes.Role)?.Value;

                if (string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(role))
                {
                    context.Fail(
                        "Required account claims are missing.");

                    return;
                }

                // Only trusted application roles.
                if (role != "Customer" &&
                    role != "Staff" &&
                    role != "Admin")
                {
                    context.Fail(
                        "Invalid account role.");

                    return;
                }

                // Resolve the existing storage service
                // from the current HTTP request scope.
                IUserTableService userService =
                    context.HttpContext.RequestServices
                        .GetRequiredService<
                            IUserTableService>();

                try
                {
                    // GetUserByEmailAsync checks:
                    //
                    // - Active email registration
                    // - Existing user record
                    // - IsActive account status
                    UserEntity? user =
                        await userService.GetUserByEmailAsync(
                            email);

                    if (user == null)
                    {
                        context.Fail(
                            "Account is unavailable or disabled.");

                        return;
                    }

                    // Prevent a token from being used
                    // with a different stored role.
                    if (!string.Equals(
                            user.PartitionKey,
                            role,
                            StringComparison.Ordinal))
                    {
                        context.Fail(
                            "Account role no longer matches.");

                        return;
                    }

                    // Explicit defence against
                    // disabled accounts.
                    if (user.IsActive == false)
                    {
                        context.Fail(
                            "Account has been disabled.");

                        return;
                    }

                    // No context.Fail() call means
                    // the token remains authenticated.
                }
                catch (Exception)
                {
                    // Fail closed when account status
                    // cannot be verified.
                    //
                    // Do not allow a request through
                    // merely because Azure Table
                    // Storage is unavailable.
                    //
                    // Operational logging and detailed
                    // error handling can be added
                    // without exposing storage errors
                    // to API clients.
                    context.Fail(
                        "Account status could not be verified.");
                }
            }
        };
    });

// =====================================================
// STEP 7: ENABLE ROLE-BASED AUTHORISATION
// =====================================================
//
// Examples:
//
// [Authorize(Roles = "Admin")]
//
// [Authorize(Roles = "Staff,Admin")]
//
// [Authorize(Roles = "Customer")]
// =====================================================

builder.Services.AddAuthorization();

// =====================================================
// STEP 8: CONFIGURE SWAGGER
// =====================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "CoffeeNChill API Gateway",

            Version = "v1",

            Description =
                "CoffeeNChill API Gateway with Azure " +
                "Storage, Azure Functions, JWT authentication, " +
                "Staff invitations and Admin account management."
        });

    // ================================================
    // INCLUDE XML DOCUMENTATION WHEN AVAILABLE
    // ================================================

    string xmlFile =
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";

    string xmlPath =
        Path.Combine(
            AppContext.BaseDirectory,
            xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // ================================================
    // CONFIGURE JWT AUTHORISATION IN SWAGGER
    // ================================================

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",

            Type = SecuritySchemeType.Http,

            Scheme = "bearer",

            BearerFormat = "JWT",

            In = ParameterLocation.Header,

            Description =
                "Enter the JWT access token returned " +
                "by POST /api/auth/login."
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
});

// =====================================================
// STEP 9: BUILD THE APPLICATION
// =====================================================
//
// All dependency injection registrations
// must be completed before builder.Build().
// =====================================================

var app = builder.Build();

// =====================================================
// STEP 10: DEVELOPMENT-ONLY ACCOUNT PROVISIONING
// =====================================================
//
// Preserve the existing commands:
//
// dotnet run -- --provision-staff
//
// dotnet run -- --provision-admin
//
// These commands work only in Development.
// They do not expose public provisioning endpoints.
// =====================================================

bool provisionStaffRequested =
    args.Contains(
        "--provision-staff",
        StringComparer.OrdinalIgnoreCase);

bool provisionAdminRequested =
    args.Contains(
        "--provision-admin",
        StringComparer.OrdinalIgnoreCase);

if (provisionStaffRequested || provisionAdminRequested)
{
    // ================================================
    // STEP 10.1: RESTRICT TO DEVELOPMENT
    // ================================================

    if (!app.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "Account provisioning is permitted only " +
            "in the Development environment.");
    }

    if (provisionStaffRequested && provisionAdminRequested)
    {
        throw new InvalidOperationException(
            "Specify either --provision-staff " +
            "or --provision-admin, not both.");
    }

    string role =
        provisionAdminRequested
            ? "Admin"
            : "Staff";

    Console.WriteLine();

    Console.WriteLine(
        "========================================");

    Console.WriteLine(
        $" CoffeeNChill {role} Account Provisioning");

    Console.WriteLine(
        "========================================");

    Console.WriteLine();

    // ================================================
    // STEP 10.2: READ ACCOUNT DETAILS
    // ================================================

    Console.Write($"{role} email: ");

    string email =
        Console.ReadLine()?.Trim()
        ?? string.Empty;

    Console.Write($"{role} full name: ");

    string fullName =
        Console.ReadLine()?.Trim()
        ?? string.Empty;

    // ================================================
    // STEP 10.3: READ PASSWORD WITHOUT ECHO
    // ================================================

    Console.Write($"{role} password: ");

    var passwordBuilder = new StringBuilder();

    while (true)
    {
        ConsoleKeyInfo key =
            Console.ReadKey(intercept: true);

        if (key.Key == ConsoleKey.Enter)
        {
            break;
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (passwordBuilder.Length > 0)
            {
                passwordBuilder.Length--;
            }

            continue;
        }

        if (!char.IsControl(key.KeyChar))
        {
            passwordBuilder.Append(key.KeyChar);
        }
    }

    Console.WriteLine();

    string password =
        passwordBuilder.ToString();

    // ================================================
    // STEP 10.4: PROVISION TRUSTED ACCOUNT
    // ================================================

    using (var scope = app.Services.CreateScope())
    {
        var passwordHasher =
            scope.ServiceProvider
                .GetRequiredService<
                    IPasswordHasher<UserEntity>>();

        try
        {
            if (provisionAdminRequested)
            {
                // Preserve Admin provisioning.
                await AdminAccountProvisioner
                    .ProvisionAsync(
                        app.Configuration,
                        passwordHasher,
                        email,
                        fullName,
                        password);
            }
            else
            {
                // Preserve Staff provisioning.
                await StaffAccountProvisioner
                    .ProvisionAsync(
                        app.Configuration,
                        passwordHasher,
                        email,
                        fullName,
                        password);
            }

            Console.WriteLine();

            Console.WriteLine(
                $"{role} account created successfully.");

            Console.WriteLine($"Role: {role}");

            Console.WriteLine(
                "Email: " +
                email.Trim().ToLowerInvariant());

            Console.WriteLine();

            Console.WriteLine(
                "You can now log in through Swagger.");
        }
        catch (Exception ex)
        {
            Console.WriteLine();

            Console.WriteLine(
                $"{role} provisioning failed:");

            Console.WriteLine(ex.Message);
        }
    }

    // Provisioning does not start the web server.
    return;
}

// =====================================================
// STEP 11: GLOBAL EXCEPTION HANDLING
// =====================================================

app.UseMiddleware<
    ExceptionHandlingMiddleware>();


// =====================================================
// STEP 12: ENABLE SWAGGER IN ALL ENVIRONMENTS
// =====================================================
//
// Works in:
// 1. Local Visual Studio development
// 2. Local Docker containers
// 3. Oracle Cloud production deployment
//
// In Production, public access to Swagger must be
// protected by the reverse proxy (Caddy).
// =====================================================

app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "CoffeeNChill API Gateway v1");

    options.RoutePrefix = "swagger";
});




// =====================================================
// STEP 13: ENABLE HTTPS REDIRECTION
// =====================================================

app.UseHttpsRedirection();

// =====================================================
// STEP 14: AUTHENTICATION AND AUTHORISATION
// =====================================================
//
// Authentication must run before authorisation.
// =====================================================

app.UseAuthentication();

app.UseAuthorization();

// =====================================================
// STEP 15: MAP CONTROLLERS
// =====================================================

app.MapControllers();

// =====================================================
// STEP 16: START API GATEWAY
// =====================================================

app.Run();
