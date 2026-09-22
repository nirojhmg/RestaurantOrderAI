using Microsoft.AspNetCore.Mvc;

namespace RestaurantOrderAI.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly OrderTakingAI _ai;

        public ChatController(OrderTakingAI ai)
        {
            _ai = ai;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.SessionId) || string.IsNullOrEmpty(request.Message))
            {
                return BadRequest("SessionId and Message are required.");
            }

            var response = await _ai.ProcessMessageAsync(request.SessionId, request.Message);
            return Ok(new { response });
        }
    }

    public class ChatRequest
    {
        public string SessionId { get; set; }
        public string Message { get; set; }
    }
}
