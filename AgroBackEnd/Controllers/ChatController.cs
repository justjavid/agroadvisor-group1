using Microsoft.AspNetCore.Mvc;
using Service.DTOs.Chat;
using Service.Services.Interfaces;

namespace AgroBackEnd.Controllers;

[ApiController]
[Route("api/chats")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponseDto>> Ask(
        [FromBody] ChatRequestDto request,
        CancellationToken ct)
    {
        var result = await _chatService.AskAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("users/{userId}/sessions")]
    public async Task<ActionResult<IReadOnlyList<ChatSessionDto>>> GetUserSessions(
        string userId,
        CancellationToken ct)
    {
        var sessions = await _chatService.GetUserSessionsAsync(userId, ct);
        return Ok(sessions);
    }

    [HttpGet("sessions/{sessionId}/messages")]
    public async Task<ActionResult<IReadOnlyList<ChatMessageDto>>> GetSessionMessages(
        Guid sessionId,
        [FromQuery] string userId,
        CancellationToken ct)
    {
        var result = await _chatService.GetSessionMessagesAsync(sessionId, userId, ct);
        return Ok(result);
    }

    [HttpDelete("sessions/{sessionId}")]
    public async Task<IActionResult> DeleteSession(
        Guid sessionId,
        [FromQuery] string userId,
        CancellationToken ct)
    {
        await _chatService.DeleteSessionAsync(sessionId, userId, ct);
        return NoContent();
    }
}