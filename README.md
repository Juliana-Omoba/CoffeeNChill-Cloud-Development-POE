# CoffeeNChill Canteen Management System

## CLDV6212 Portfolio of Evidence: Part 1 and Part 2

CoffeeNChill is a cloud-enabled canteen management system developed
incrementally for the CLDV6212 Portfolio of Evidence (POE). The project
modernises a paper-based campus canteen using Azure Storage, Azure
Functions, Azurite, Docker, Docker Hub, Azure Storage Queues and Docker
Compose.

## 1. Project Overview

The original CoffeeNChill environment relies on paper menus, handwritten
orders and locally stored operational documents. The solution introduces
a digital menu, centralised staff-document management and asynchronous
order processing.

**Part 1** establishes the storage and HTTP Function foundation. **Part
2** extends the application with Azure Queue Storage, background order
processing, order-status tracking, poison-message handling and Docker
Compose orchestration.

------------------------------------------------------------------------

# Part 1: Azure Functions, Storage and Docker

## 2. Part 1 Components

Part 1 includes:

-   Azure Functions using .NET 8 isolated worker;
-   Azure Table Storage/Azurite for menu data;
-   centralised staff-document storage;
-   HTTP-triggered Azure Functions;
-   Postman testing;
-   Azurite for local Azure Storage emulation;
-   Docker containerisation; and
-   Docker Hub image version `v1.0`.

## 3. Menu Management

Menu items are stored in the `MenuItems` table.

  -----------------------------------------------------------------------
  Field                               Purpose
  ----------------------------------- -----------------------------------
  `PartitionKey`                      Menu category, such as Hot Drinks,
                                      Cold Drinks, Pastries or Sandwiches

  `RowKey`                            Unique item SKU/ID

  `Name`                              Menu item name

  `Description`                       Item description

  `Price`                             Item price

  `IsAvailable`                       Availability status
  -----------------------------------------------------------------------

### Menu Endpoints

  ---------------------------------------------------------------------------------
  Method                  Endpoint                          Purpose
  ----------------------- --------------------------------- -----------------------
  POST                    `/api/menu`                       Create a menu item

  GET                     `/api/menu`                       Return all menu items

  GET                     `/api/menu/category/{category}`   Filter menu items by
                                                            category

  PUT                     `/api/menu/{category}/{id}`       Update a menu item

  DELETE                  `/api/menu/{category}/{id}`       Delete a menu item
  ---------------------------------------------------------------------------------

## 4. Staff Documents

Part 1 provides centralised storage for operational documents such as
recipes, equipment instructions and health and safety documentation.

  --------------------------------------------------------------------------------------
  Method                  Endpoint                               Purpose
  ----------------------- -------------------------------------- -----------------------
  POST                    `/api/documents/upload`                Upload a staff document

  GET                     `/api/documents`                       List stored staff
                                                                 documents

  GET                     `/api/documents/download/{fileName}`   Download a selected
                                                                 document
  --------------------------------------------------------------------------------------

These endpoints are tested through Postman.

## 5. Part 1 Docker Image

The Part 1 Functions image is:

``` text
babyade/coffeenchill-functions:v1.0
```

Build and publish it from the Functions project directory:

``` powershell
docker build -t babyade/coffeenchill-functions:v1.0 .
docker images
docker login
docker push babyade/coffeenchill-functions:v1.0
```

Part 1 demonstrates standalone container execution. Azurite and the
Functions application are started individually rather than through
Docker Compose.

------------------------------------------------------------------------

# Part 2: Queue Triggers and Docker Compose

## 6. Part 2 Overview

Part 2 introduces asynchronous order processing for peak-hour demand.
The HTTP request does not wait for the complete order-preparation
workflow. Instead, the application validates and queues the order,
returns a response to the client, and processes the order in the
background.

The Part 2 Functions image is:

``` text
babyade/coffeenchill-functions:v2.0
```

## 7. Part 2 Architecture

``` text
Postman / Client
       |
       | POST /api/orders/queue
       v
QueueOrder HTTP Function
       |
       | Validate request
       | Generate timestamp
       | Serialise Order to JSON
       v
order-processing-queue
       |
       | QueueTrigger
       v
ProcessOrderQueue
       |
       | Deserialise JSON
       | Create/update order
       v
Orders Azure Table
       |
       v
Received -> Preparing -> Ready -> Collected
```

The producer and consumer are decoupled by Azure Queue Storage.

## 8. Queue Producer

The producer endpoint is:

``` text
POST /api/orders/queue
```

`QueueOrder` performs the following tasks:

1.  reads the incoming JSON request;
2.  validates the request;
3.  generates `OrderTimestamp` using server time;
4.  creates the `Order` object;
5.  sends the order through `OrderQueueService`;
6.  returns `202 Accepted` for a valid queued order; and
7.  logs unexpected failures.

Example request:

``` json
{
  "OrderId": "ORD-2026-001",
  "CustomerName": "Jane Smith",
  "SelectedItemSKUs": ["COF-003", "PAS-104"],
  "TotalPrice": 75.00
}
```

### Producer Validation

The producer checks that:

-   `OrderId` is supplied;
-   `CustomerName` is supplied;
-   `SelectedItemSKUs` is not null or empty;
-   SKU values are not blank;
-   `TotalPrice` is greater than zero; and
-   the request contains valid JSON.

Invalid client requests are rejected with `400 Bad Request` before
entering the queue.

## 9. Queue Service and Base64 Encoding

The queue is:

``` text
order-processing-queue
```

`OrderQueueService`:

-   creates the queue if required;
-   serialises the `Order` object to JSON;
-   uses Base64 message encoding;
-   sends the JSON message to Queue Storage; and
-   logs and rethrows queue-related failures.

The queue client uses:

``` csharp
MessageEncoding = QueueMessageEncoding.Base64
```

## 10. Queue Consumer

`ProcessOrderQueue` is the background consumer:

``` csharp
[QueueTrigger("order-processing-queue",
    Connection = "AzureWebJobsStorage")]
```

It is not called directly by Postman. A message arriving on
`order-processing-queue` triggers it automatically.

The consumer:

1.  receives the queue message;
2.  deserialises the JSON into an `Order`;
3.  checks essential order information;
4.  creates an `OrderEntity`;
5.  stores the order in the `Orders` table with status `Received`; and
6.  advances the status through the order lifecycle.

## 11. Orders Table and Lifecycle

The `Orders` table uses:

-   `PartitionKey`: order date in `yyyy-MM-dd` format;
-   `RowKey`: `OrderId`;
-   customer details;
-   selected SKU information;
-   total price;
-   order timestamp;
-   status; and
-   last-updated information.

The lifecycle is:

``` text
Received -> Preparing -> Ready -> Collected
```

## 12. Two Layers of Protection

### Layer 1: Producer Validation

Producer validation runs **before** a message enters the queue.

``` text
Invalid client request
        |
        v
QueueOrder validation
        |
        v
400 Bad Request
        |
        X
Does not enter queue
```

This prevents known malformed or incomplete client requests from
reaching the queue.

### Layer 2: Consumer Failure Handling

Consumer failure handling operates **after** a message has reached the
queue.

The consumer logs the exception and rethrows it:

``` csharp
catch (Exception ex)
{
    _logger.LogError(
        ex,
        "Failed to process queue message: {QueueMessage}",
        queueMessage);

    throw;
}
```

Rethrowing tells the Azure Functions runtime that processing failed,
allowing retry and poison-message handling.

Both layers are necessary. Producer validation protects the API
boundary. Consumer failure handling protects background processing when
a queued message still cannot be processed.

## 13. Poison Queue Handling

A poison message is a queue message that repeatedly fails processing.

``` text
Unprocessable message
        |
        v
order-processing-queue
        |
        v
ProcessOrderQueue
        |
        v
Exception
        |
        v
LogError + throw
        |
        v
Runtime retries
        |
        v
Repeated failure
        |
        v
order-processing-queue-poison
```

### Why a Bad Message Is Introduced Manually for Testing

Under normal use, producer validation stops malformed requests before
they enter the queue. Therefore, sending malformed JSON through Postman
proves producer validation, but does not exercise the consumer's failure
path.

To test the second layer deliberately, an invalid message is inserted
directly into the **normal** queue through Microsoft Azure Storage
Explorer.

Example:

``` text
POISON-TEST-001 - THIS IS NOT A VALID ORDER
```

It must be inserted into:

``` text
order-processing-queue
```

It must **not** be manually inserted into:

``` text
order-processing-queue-poison
```

The Function attempts to process the message, logs and rethrows the
failure, and the runtime retries it. Its later appearance in
`order-processing-queue-poison` provides evidence of poison-message
handling.

------------------------------------------------------------------------

# Docker and Docker Compose

## 14. Build and Publish Part 2

From the Functions project directory:

``` powershell
docker build -t babyade/coffeenchill-functions:v2.0 .
docker images
docker login
docker push babyade/coffeenchill-functions:v2.0
```

## 15. Docker Compose Architecture

Part 2 uses a root `docker-compose.yml` to coordinate:

1.  **Azurite**, emulating Azure Blob, Queue and Table services; and
2.  **Functions**, running the CoffeeNChill HTTP and Queue-triggered
    Functions.

The Functions service references the Docker Hub image:

``` yaml
image: babyade/coffeenchill-functions:v2.0
```

The Compose configuration also uses:

-   a custom bridge network;
-   an Azurite persistent volume;
-   published ports;
-   `AzureWebJobsStorage`;
-   `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`; and
-   `depends_on` for the Azurite service.

### Local Ports

  Service                          Port
  ----------------------------- -------
  Azurite Blob                    10000
  Azurite Queue                   10001
  Azurite Table                   10002
  CoffeeNChill Functions HTTP      7071

## 16. Running Part 2

Prerequisites include Docker Desktop, Postman, Microsoft Azure Storage
Explorer and Git. The .NET 8 SDK is required for local
development/building.

From the CoffeeNChill root folder containing `docker-compose.yml`:

``` powershell
docker compose config
docker compose pull
docker compose up
```

Keep the `docker compose up` PowerShell window open to observe Functions
and Azurite logs.

To stop the foreground Compose process:

``` text
Ctrl + C
```

To remove the Compose containers:

``` powershell
docker compose down
```

To verify the containers in a second PowerShell window:

``` powershell
docker ps
```

To follow only Functions logs:

``` powershell
docker compose logs --follow functions
```

Press `Ctrl + C` to stop following the logs without stopping the
containers.

------------------------------------------------------------------------

# Testing

## 17. Postman Configuration

Local base URL:

``` text
http://localhost:7071
```

Recommended environment variable:

``` text
baseUrl = http://localhost:7071
```

Order endpoint:

``` text
{{baseUrl}}/api/orders/queue
```

### Part 2 Tests

The Postman collection should demonstrate:

1.  valid order -\> `202 Accepted`;
2.  missing `OrderId` -\> `400 Bad Request`;
3.  missing `CustomerName` -\> `400 Bad Request`;
4.  empty SKU list -\> `400 Bad Request`;
5.  blank SKU -\> `400 Bad Request`;
6.  zero `TotalPrice` -\> `400 Bad Request`;
7.  negative `TotalPrice` -\> `400 Bad Request`;
8.  malformed JSON -\> `400 Bad Request`; and
9.  another valid order -\> `202 Accepted`.

Use unique order IDs for successful tests to avoid duplicate Azure Table
row keys.

## 18. Verify End-to-End Queue Processing

For a valid order:

1.  submit the order through Postman;
2.  confirm `202 Accepted`;
3.  observe the Docker/Functions logs;
4.  confirm `ProcessOrderQueue` executes;
5.  open Microsoft Azure Storage Explorer;
6.  inspect the `Orders` table; and
7.  verify the order reaches `Collected`.

## 19. Poison Queue Test

1.  Keep `docker compose up` running.
2.  Open Microsoft Azure Storage Explorer.
3.  Open `order-processing-queue`.
4.  Select **Add Message**.
5.  enter a deliberately invalid message;
6.  use Base64 encoding as expected by the implementation;
7.  watch the Functions logs;
8.  allow the retry behaviour to occur;
9.  refresh the queues; and
10. verify the failed test message appears in
    `order-processing-queue-poison`.

The invalid message is manually inserted only into the normal queue. The
system should be responsible for its eventual poison-queue isolation.

------------------------------------------------------------------------

# Repository Information

## 20. Suggested Project Structure

``` text
CoffeeNChill/
|
|-- CoffeeNChill.Functions/
|   |-- Functions/
|   |-- Interfaces/
|   |-- Models/
|   |-- Services/
|   |-- Program.cs
|   |-- Dockerfile
|   `-- ...
|
|-- docs/
|   |-- Postman collection
|   `-- supporting evidence
|
|-- docker-compose.yml
|-- README.md
`-- ...
```

Important Part 2 classes include `CreateOrderRequest`, `Order`,
`OrderEntity`, `IOrderQueueService`, `OrderQueueService`,
`IOrderTableService`, `OrderTableService`, `OrderFunctions`, and
`ProcessOrderQueue`.

## 21. Technologies

  -----------------------------------------------------------------------
  Technology                          Purpose
  ----------------------------------- -----------------------------------
  C# / .NET 8                         Application development

  Azure Functions isolated worker     HTTP and Queue-triggered functions

  Azure Table Storage                 Menu and order records

  Azure Queue Storage                 Asynchronous order messages

  Azurite                             Local Azure Storage emulation

  Docker                              Containerisation

  Docker Hub                          Versioned image publishing

  Docker Compose                      Multi-container orchestration

  Postman                             API and validation testing

  Azure Storage Explorer              Queue/table inspection and
                                      poison-message testing

  Git / GitHub                        Version control and collaboration
  -----------------------------------------------------------------------

## 22. Docker Image Versions

  POE Part   Functions Image
  ---------- ---------------------------------------
  Part 1     `babyade/coffeenchill-functions:v1.0`
  Part 2     `babyade/coffeenchill-functions:v2.0`

## 23. Git and GitHub

Use meaningful commits distributed across group members. Examples
include:

``` text
Add menu CRUD Azure Functions
Implement staff document operations
Add order queue producer validation
Implement ProcessOrderQueue queue trigger
Add Orders table lifecycle updates
Add poison queue failure logging
Add Docker Compose orchestration
Update README with Part 2 setup and testing
```

The exported Postman collection should be committed in the `/docs`
folder.

## 24. Demonstration Evidence

The final demonstration should include Part 1 menu/document operations
and standalone Docker execution, followed by Part 2 evidence showing:

-   `docker compose up`;
-   Docker Hub `v2.0` image use/pull;
-   Postman valid and invalid tests;
-   queue message placement;
-   automatic `ProcessOrderQueue` execution;
-   the `Orders` table;
-   `Received -> Preparing -> Ready -> Collected`;
-   failure logging/retries;
-   poison-queue handling; and
-   the versioned Docker Hub images.

## 25. Submission Links

Replace the placeholders before final submission:

``` text
GitHub Repository: [ADD GITHUB REPOSITORY LINK HERE]
Docker Hub Repository: [ADD DOCKER HUB REPOSITORY LINK HERE]
YouTube Demonstration: [ADD YOUTUBE VIDEO LINK HERE]
```

## 26. Summary

Part 1 establishes the CoffeeNChill menu, document-management, storage
and containerisation foundation. Part 2 extends the system with
asynchronous order processing.

The Part 2 design separates order acceptance from background processing.
Producer validation stops known invalid client requests before queue
placement, while consumer failure handling protects the background
worker when a queued message cannot be processed. Docker Compose then
coordinates Azurite and the Functions application as a single local
application stack.
