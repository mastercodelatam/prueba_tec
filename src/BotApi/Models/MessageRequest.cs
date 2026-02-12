namespace BotApi.Models;

public class MessageRequest
{
    public string ConversationId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
