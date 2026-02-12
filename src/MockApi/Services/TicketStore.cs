using System.Collections.Concurrent;
using MockApi.Models;

namespace MockApi.Services;

public class TicketStore
{
    private readonly ConcurrentDictionary<string, Ticket> _tickets = new();
    private int _ticketCounter = 101;

    public TicketStore()
    {
        // Pre-cargar algunos tickets de ejemplo
        var ticket1 = new Ticket
        {
            Id = "TCK-100",
            Name = "Usuario Ejemplo",
            Email = "ejemplo@test.com",
            Description = "Problema de ejemplo para pruebas",
            Status = "Abierto",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        _tickets.TryAdd(ticket1.Id, ticket1);

        var ticket2 = new Ticket
        {
            Id = "TCK-101",
            Name = "Juan Pérez",
            Email = "juan@test.com",
            Description = "No puedo acceder a mi cuenta",
            Status = "En Progreso",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _tickets.TryAdd(ticket2.Id, ticket2);
    }

    public Ticket CreateTicket(string name, string email, string description)
    {
        var id = $"TCK-{Interlocked.Increment(ref _ticketCounter)}";
        var ticket = new Ticket
        {
            Id = id,
            Name = name,
            Email = email,
            Description = description,
            Status = "Abierto",
            CreatedAt = DateTime.UtcNow
        };
        _tickets.TryAdd(id, ticket);
        return ticket;
    }

    public Ticket? GetTicket(string id)
    {
        _tickets.TryGetValue(id, out var ticket);
        return ticket;
    }
}
