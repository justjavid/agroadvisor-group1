using Microsoft.AspNetCore.Mvc;
using Service.DTOs.ChatBotDTOs;
using Service.Services.ChatBot.Interfaces;

namespace AgroAdvisor.Controllers;

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
    [ProducesResponseType(typeof(ChatResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatResponseDto>> Ask([FromBody] ChatRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _chatService.AskAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{sessionId:guid}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<ChatMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ChatMessageDto>>> GetSessionMessages(Guid sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var messages = await _chatService.GetSessionMessagesAsync(sessionId, cancellationToken);
            return Ok(messages);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("sessions/{userId}")]
    [ProducesResponseType(typeof(IReadOnlyList<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Guid>>> GetUserSessions(string userId, CancellationToken cancellationToken)
    {
        var sessions = await _chatService.GetUserSessionsAsync(userId, cancellationToken);
        return Ok(sessions);
    }

    [HttpDelete("{sessionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSession(Guid sessionId, [FromQuery] string userId, CancellationToken cancellationToken)
    {
        try
        {
            await _chatService.DeleteSessionAsync(sessionId, userId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
