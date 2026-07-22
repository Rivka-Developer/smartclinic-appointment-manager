using AppointmentManager.Application.DTOs.Chat;

namespace AppointmentManager.Application.Interfaces;

public interface IChatBotService
{
    Task<string> GetReplyAsync(string userMessage, List<ChatMessage> history);
}
