using Microsoft.AspNetCore.Mvc;
using BotApi.Models;
using BotApi.Services;

namespace BotApi.Controllers;

[ApiController]
[Route("[controller]")]
public class MessagesController : ControllerBase
{
    private readonly ConversationEngine _conversationEngine;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(ConversationEngine conversationEngine, ILogger<MessagesController> logger)
    {
        _conversationEngine = conversationEngine;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<MessageResponse>> PostMessage([FromBody] MessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ConversationId))
        {
            return BadRequest(new { error = "conversationId es requerido" });
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "message es requerido" });
        }

        _logger.LogInformation(
            "Mensaje recibido - ConversationId: {ConversationId}, Message: {Message}",
            request.ConversationId,
            request.Message);

        try
        {
            var response = await _conversationEngine.ProcessMessageAsync(
                request.ConversationId,
                request.Message);

            return Ok(new MessageResponse
            {
                ConversationId = request.ConversationId,
                Response = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando mensaje");
            return StatusCode(500, new { error = "Error interno del servidor" });
        }
    }
}
