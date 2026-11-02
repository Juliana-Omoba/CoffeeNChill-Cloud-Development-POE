
//using Microsoft.AspNetCore.Mvc;

//namespace CoffeeNChill.ApiGateway.Controllers
//{
//    /// <summary>
//    /// Temporary endpoints for testing API behaviour.
//    /// </summary>
//    [ApiController]
//    [Route("api/diagnostics")]
//    public class DiagnosticsController : ControllerBase
//    {
//        /// <summary>
//        /// Generates an intentional exception to test
//        /// the global exception-handling middleware.
//        /// </summary>
//        /// <returns>An error response handled by middleware.</returns>
//        [HttpGet("test-error")]
//        [ProducesResponseType(
//            StatusCodes.Status500InternalServerError)]
//        public IActionResult TestError()
//        {
//            throw new InvalidOperationException(
//                "Intentional error for middleware testing."
//            );
//        }
//    }
//}
