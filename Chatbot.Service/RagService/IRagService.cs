namespace Chatbot.Services
{
    public interface IRagService
    {
        Task<string> ProcessNaturalQueryAsync(string query, string language);
    }
}
