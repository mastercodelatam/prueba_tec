using BotApi.Models;

namespace BotApi.Services;

public class ConversationEngine
{
    private readonly ConversationStateService _stateService;
    private readonly TicketApiClient _ticketApiClient;
    private readonly ILogger<ConversationEngine> _logger;

    public ConversationEngine(
        ConversationStateService stateService,
        TicketApiClient ticketApiClient,
        ILogger<ConversationEngine> logger)
    {
        _stateService = stateService;
        _ticketApiClient = ticketApiClient;
        _logger = logger;
    }

    public async Task<string> ProcessMessageAsync(string conversationId, string message)
    {
        var state = _stateService.GetOrCreate(conversationId);

        // Detectar intención de cancelar (prioridad máxima)
        var intent = IntentDetector.Detect(message);
        if (intent.Intent == Intent.Cancel)
        {
            return HandleCancel(state);
        }

        // Si hay un flujo activo, continuar con él
        if (state.ActiveFlow != FlowType.None)
        {
            return await HandleActiveFlowAsync(state, message);
        }

        // No hay flujo activo, procesar según intención
        return intent.Intent switch
        {
            Intent.CreateTicket => StartCreateTicketFlow(state),
            Intent.CheckTicketStatus => await HandleCheckTicketStatusAsync(intent.TicketId!),
            Intent.Greeting => GetGreetingResponse(),
            Intent.Help => GetHelpResponse(),
            _ => GetUnknownResponse()
        };
    }

    private string HandleCancel(ConversationState state)
    {
        if (state.ActiveFlow == FlowType.None)
        {
            return "No hay ningún proceso activo para cancelar. ¿En qué puedo ayudarte?";
        }

        state.Reset();
        _stateService.Update(state);
        return "✓ Proceso cancelado. Los datos han sido eliminados. ¿En qué más puedo ayudarte?";
    }

    private string StartCreateTicketFlow(ConversationState state)
    {
        state.ActiveFlow = FlowType.CreateTicket;
        state.CurrentStep = CreateTicketStep.AskingName;
        state.TicketData = new TicketData();
        _stateService.Update(state);

        return "¡Perfecto! Voy a ayudarte a crear un ticket de soporte.\n\n" +
               "Por favor, indícame tu **nombre completo**:";
    }

    private async Task<string> HandleActiveFlowAsync(ConversationState state, string message)
    {
        if (state.ActiveFlow == FlowType.CreateTicket)
        {
            return await HandleCreateTicketFlowAsync(state, message);
        }

        return GetUnknownResponse();
    }

    private async Task<string> HandleCreateTicketFlowAsync(ConversationState state, string message)
    {
        switch (state.CurrentStep)
        {
            case CreateTicketStep.AskingName:
                return HandleNameStep(state, message);

            case CreateTicketStep.AskingEmail:
                return HandleEmailStep(state, message);

            case CreateTicketStep.AskingDescription:
                return HandleDescriptionStep(state, message);

            case CreateTicketStep.Confirmation:
                return await HandleConfirmationStepAsync(state, message);

            default:
                return GetUnknownResponse();
        }
    }

    private string HandleNameStep(ConversationState state, string message)
    {
        var name = message.Trim();

        if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
        {
            return "Por favor, ingresa un nombre válido (al menos 2 caracteres):";
        }

        state.TicketData.Name = name;
        state.CurrentStep = CreateTicketStep.AskingEmail;
        _stateService.Update(state);

        return $"Gracias, {name}.\n\nAhora, por favor indícame tu **correo electrónico**:";
    }

    private string HandleEmailStep(ConversationState state, string message)
    {
        var email = message.Trim();

        if (!EmailValidator.IsValid(email))
        {
            return "⚠️ El formato del correo electrónico no es válido.\n\n" +
                   "Por favor, ingresa un correo electrónico válido (ejemplo: usuario@dominio.com):";
        }

        state.TicketData.Email = email;
        state.CurrentStep = CreateTicketStep.AskingDescription;
        _stateService.Update(state);

        return "Perfecto.\n\nAhora, por favor describe el **problema o solicitud** que deseas reportar:";
    }

    private string HandleDescriptionStep(ConversationState state, string message)
    {
        var description = message.Trim();

        if (string.IsNullOrWhiteSpace(description) || description.Length < 10)
        {
            return "Por favor, proporciona una descripción más detallada (al menos 10 caracteres):";
        }

        state.TicketData.Description = description;
        state.CurrentStep = CreateTicketStep.Confirmation;
        _stateService.Update(state);

        return "Excelente. Aquí está el resumen de tu ticket:\n\n" +
               "━━━━━━━━━━━━━━━━━━━━━━━━\n" +
               $"**Nombre:** {state.TicketData.Name}\n" +
               $"**Email:** {state.TicketData.Email}\n" +
               $"**Descripción:** {state.TicketData.Description}\n" +
               "━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
               "¿Deseas **confirmar** la creación del ticket? (Responde **sí** o **no**)";
    }

    private async Task<string> HandleConfirmationStepAsync(ConversationState state, string message)
    {
        var response = message.Trim().ToLowerInvariant();

        var affirmativeResponses = new[] { "sí", "si", "yes", "confirmar", "ok", "dale", "adelante", "confirmo" };
        var negativeResponses = new[] { "no", "cancelar", "nope" };

        if (affirmativeResponses.Any(r => response.Contains(r)))
        {
            return await CreateTicketAsync(state);
        }

        if (negativeResponses.Any(r => response.Contains(r)))
        {
            state.Reset();
            _stateService.Update(state);
            return "✓ Creación de ticket cancelada. Los datos han sido eliminados.\n\n¿En qué más puedo ayudarte?";
        }

        return "Por favor, responde **sí** para confirmar o **no** para cancelar:";
    }

    private async Task<string> CreateTicketAsync(ConversationState state)
    {
        try
        {
            var result = await _ticketApiClient.CreateTicketAsync(
                state.TicketData.Name,
                state.TicketData.Email,
                state.TicketData.Description);

            state.Reset();
            _stateService.Update(state);

            if (result == null)
            {
                return "⚠️ Hubo un error al crear el ticket. Por favor, intenta nuevamente más tarde.";
            }

            return $"✅ **¡Ticket creado exitosamente!**\n\n" +
                   $"Tu número de ticket es: **{result.Id}**\n\n" +
                   $"Puedes consultar el estado de tu ticket en cualquier momento escribiendo:\n" +
                   $"\"ver estado del ticket {result.Id}\"\n\n" +
                   "¿Hay algo más en lo que pueda ayudarte?";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear ticket");
            return "⚠️ Hubo un error al conectar con el servicio de tickets. Por favor, intenta nuevamente más tarde.";
        }
    }

    private async Task<string> HandleCheckTicketStatusAsync(string ticketId)
    {
        try
        {
            var ticket = await _ticketApiClient.GetTicketStatusAsync(ticketId);

            if (ticket == null)
            {
                return $"⚠️ No se encontró el ticket **{ticketId}**.\n\n" +
                       "Por favor, verifica el número de ticket e intenta nuevamente.";
            }

            return $"📋 **Estado del Ticket {ticket.Id}**\n\n" +
                   "━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                   $"**Estado:** {ticket.Status}\n" +
                   $"**Solicitante:** {ticket.Name}\n" +
                   $"**Descripción:** {ticket.Description}\n" +
                   $"**Fecha de creación:** {ticket.CreatedAt:dd/MM/yyyy HH:mm}\n" +
                   "━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                   "¿Hay algo más en lo que pueda ayudarte?";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar ticket {TicketId}", ticketId);
            return "⚠️ Hubo un error al consultar el estado del ticket. Por favor, intenta nuevamente más tarde.";
        }
    }

    private static string GetGreetingResponse()
    {
        return "¡Hola! 👋 Soy el bot de soporte.\n\n" +
               "Puedo ayudarte con:\n" +
               "• **Crear un ticket** de soporte\n" +
               "• **Consultar el estado** de un ticket existente\n\n" +
               "¿Qué te gustaría hacer hoy?";
    }

    private static string GetHelpResponse()
    {
        return "📚 **Centro de Ayuda**\n\n" +
               "Estas son las acciones que puedo realizar:\n\n" +
               "1️⃣ **Crear ticket**: Escribe \"quiero crear un ticket\" o \"crear ticket\"\n" +
               "2️⃣ **Ver estado de ticket**: Escribe \"ver estado del ticket TCK-123\"\n" +
               "3️⃣ **Cancelar**: En cualquier momento escribe \"cancelar\" para detener el proceso actual\n\n" +
               "¿En qué puedo ayudarte?";
    }

    private static string GetUnknownResponse()
    {
        return "No entendí tu mensaje. 🤔\n\n" +
               "Puedo ayudarte con:\n" +
               "• **Crear un ticket**: Escribe \"crear ticket\"\n" +
               "• **Ver estado de ticket**: Escribe \"ver estado del ticket TCK-123\"\n" +
               "• **Ayuda**: Escribe \"ayuda\" para más opciones";
    }
}
