using Chatbot.DTOs.Invoices;
using Chatbot.Services;
using Microsoft.AspNetCore.Mvc;

[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IRagService _ragService;
    public ChatController(IRagService ragService) { _ragService = ragService; }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] UserMessageDto input)
    {
        var response = await _ragService.ProcessNaturalQueryAsync(input.Message, input.Language);
        return Ok(new { response });
    }
}
