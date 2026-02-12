namespace BotApi.Models;

public enum FlowType
{
    None,
    CreateTicket
}

public enum CreateTicketStep
{
    None,
    AskingName,
    AskingEmail,
    AskingDescription,
    Confirmation
}

public class TicketData
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ConversationState
{
    public string ConversationId { get; set; } = string.Empty;
    public FlowType ActiveFlow { get; set; } = FlowType.None;
    public CreateTicketStep CurrentStep { get; set; } = CreateTicketStep.None;
    public TicketData TicketData { get; set; } = new();

    public void Reset()
    {
        ActiveFlow = FlowType.None;
        CurrentStep = CreateTicketStep.None;
        TicketData = new TicketData();
    }
}
