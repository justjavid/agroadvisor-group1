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

    // 🔹 SEND MESSAGE
    [HttpPost]
    public async Task<ActionResult<ChatResponseDto>> Ask(
        [FromBody] ChatRequestDto request,
        CancellationToken ct)
    {
        var result = await _chatService.AskAsync(request, ct);
        return Ok(result);
    }

    // 🔹 GET USER SESSIONS
    [HttpGet("users/{userId}/sessions")]
    public async Task<ActionResult<IReadOnlyList<ChatSessionDto>>> GetUserSessions(
        string userId)
    {
        var sessions = await _chatService.GetUserSessionsAsync(userId);
        return Ok(sessions);
    }

    // 🔹 GET SESSION MESSAGES
    [HttpGet("sessions/{sessionId}/messages")]
    public async Task<IActionResult> GetSessionMessages(
        Guid sessionId,
        [FromQuery] string userId)
    {
        var result = await _chatService.GetSessionMessagesAsync(sessionId, userId);
        return Ok(result);
    }

    // 🔹 DELETE SESSION (soft delete)
    [HttpDelete("sessions/{sessionId}")]
    public async Task<IActionResult> DeleteSession(
        Guid sessionId,
        [FromQuery] string userId)
    {
        await _chatService.DeleteSessionAsync(sessionId, userId);
        return NoContent();
    }
}