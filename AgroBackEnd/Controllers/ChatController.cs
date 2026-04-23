using Microsoft.AspNetCore.Mvc;
using Service.DTOs.Chat;
using Service.Services.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponseDto>> Ask(ChatRequestDto request, CancellationToken ct)
    {
        var result = await _chatService.AskAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("sessions/{userId}")]
    public async Task<IActionResult> GetUserSessions(string userId)
    {
        var result = await _chatService.GetUserSessionsAsync(userId);
        return Ok(result);
    }

    [HttpGet("messages/{sessionId}")]
    public async Task<IActionResult> GetSessionMessages(Guid sessionId)
    {
        var result = await _chatService.GetSessionMessagesAsync(sessionId);
        return Ok(result);
    }

    [HttpDelete("{sessionId}")]
    public async Task<IActionResult> DeleteSession(Guid sessionId, [FromQuery] string userId)
    {
        await _chatService.DeleteSessionAsync(sessionId, userId);
        return NoContent();
    }
}