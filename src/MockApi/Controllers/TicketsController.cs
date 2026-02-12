using Microsoft.AspNetCore.Mvc;
using MockApi.Models;
using MockApi.Services;

namespace MockApi.Controllers;

[ApiController]
[Route("tickets")]
public class TicketsController : ControllerBase
{
    private readonly TicketStore _ticketStore;
    private readonly TokenService _tokenService;

    public TicketsController(TicketStore ticketStore, TokenService tokenService)
    {
        _ticketStore = ticketStore;
        _tokenService = tokenService;
    }

    [HttpPost]
    public IActionResult CreateTicket([FromBody] CreateTicketRequest request)
    {
        if (!ValidateAuth())
        {
            return Unauthorized(new { error = "invalid_token" });
        }

        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new { error = "missing_fields", message = "Todos los campos son requeridos" });
        }

        var ticket = _ticketStore.CreateTicket(request.Name, request.Email, request.Description);

        return Ok(new CreateTicketResponse
        {
            Id = ticket.Id,
            Message = "Ticket creado exitosamente"
        });
    }

    [HttpGet("{id}")]
    public IActionResult GetTicket(string id)
    {
        if (!ValidateAuth())
        {
            return Unauthorized(new { error = "invalid_token" });
        }

        var ticket = _ticketStore.GetTicket(id);

        if (ticket == null)
        {
            return NotFound(new { error = "ticket_not_found", message = $"No se encontró el ticket {id}" });
        }

        return Ok(new TicketStatusResponse
        {
            Id = ticket.Id,
            Status = ticket.Status,
            Name = ticket.Name,
            Description = ticket.Description,
            CreatedAt = ticket.CreatedAt
        });
    }

    private bool ValidateAuth()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        return _tokenService.ValidateToken(authHeader ?? string.Empty);
    }
}
