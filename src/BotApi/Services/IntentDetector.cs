using System.Text.RegularExpressions;

namespace BotApi.Services;

public enum Intent
{
    Unknown,
    CreateTicket,
    Cancel,
    CheckTicketStatus,
    Greeting,
    Help
}

public class IntentResult
{
    public Intent Intent { get; set; }
    public string? TicketId { get; set; }
}

public static class IntentDetector
{
    private static readonly Regex TicketStatusRegex = new(
        @"(?:ver|consultar|estado|status|revisar).*(?:ticket|tck)[- ]?(\w+-?\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex TicketIdOnlyRegex = new(
        @"(TCK-\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly string[] CreateTicketPhrases = new[]
    {
        "crear ticket",
        "crear un ticket",
        "quiero crear un ticket",
        "necesito crear un ticket",
        "abrir ticket",
        "nuevo ticket",
        "reportar problema",
        "tengo un problema"
    };

    private static readonly string[] CancelPhrases = new[]
    {
        "cancelar",
        "cancel",
        "salir",
        "terminar",
        "abandonar"
    };

    private static readonly string[] GreetingPhrases = new[]
    {
        "hola",
        "hello",
        "hi",
        "buenos días",
        "buenas tardes",
        "buenas noches",
        "hey"
    };

    private static readonly string[] HelpPhrases = new[]
    {
        "ayuda",
        "help",
        "qué puedes hacer",
        "opciones",
        "menú"
    };

    public static IntentResult Detect(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return new IntentResult { Intent = Intent.Unknown };
        }

        var normalizedMessage = message.Trim().ToLowerInvariant();

        // Detectar cancelación primero (tiene prioridad)
        if (CancelPhrases.Any(phrase => normalizedMessage.Contains(phrase)))
        {
            return new IntentResult { Intent = Intent.Cancel };
        }

        // Detectar consulta de estado de ticket
        var ticketMatch = TicketStatusRegex.Match(message);
        if (ticketMatch.Success)
        {
            var ticketId = ticketMatch.Groups[1].Value.ToUpperInvariant();
            if (!ticketId.StartsWith("TCK-"))
            {
                ticketId = $"TCK-{ticketId}";
            }
            return new IntentResult { Intent = Intent.CheckTicketStatus, TicketId = ticketId };
        }

        // Detectar solo ID de ticket mencionado
        var ticketIdMatch = TicketIdOnlyRegex.Match(message);
        if (ticketIdMatch.Success && (normalizedMessage.Contains("estado") || normalizedMessage.Contains("ver") || normalizedMessage.Contains("consultar")))
        {
            return new IntentResult
            {
                Intent = Intent.CheckTicketStatus,
                TicketId = ticketIdMatch.Groups[1].Value.ToUpperInvariant()
            };
        }

        // Detectar creación de ticket
        if (CreateTicketPhrases.Any(phrase => normalizedMessage.Contains(phrase)))
        {
            return new IntentResult { Intent = Intent.CreateTicket };
        }

        // Detectar saludo
        if (GreetingPhrases.Any(phrase => normalizedMessage.StartsWith(phrase) || normalizedMessage == phrase))
        {
            return new IntentResult { Intent = Intent.Greeting };
        }

        // Detectar ayuda
        if (HelpPhrases.Any(phrase => normalizedMessage.Contains(phrase)))
        {
            return new IntentResult { Intent = Intent.Help };
        }

        return new IntentResult { Intent = Intent.Unknown };
    }
}
