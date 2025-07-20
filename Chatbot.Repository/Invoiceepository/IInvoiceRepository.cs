using Chatbot.Models;


namespace Chatbot.Repositories
{
    public interface IInvoiceRepository : IRepository<Invoice>
    {
        Task<Invoice> GetByIdWithDetailsAsync(int id);
        Task<Invoice> GetInvoiceByNumberWithDetailsAsync(string invoiceNumber);
        Task<IEnumerable<Invoice>> GetAllWithDetailsAsync(int pageNumber = 1, int pageSize = 10); 
        Task<int> GetTotalCountAsync();
        Task<IEnumerable<Invoice>> GetInvoicesByCustomerWithDetailsAsync(string customerName);
        Task<decimal> GetTotalInvoicesThisMonthAsync();
        Task<int> GetInvoiceCountLastWeekAsync();
        Task<Invoice> GetInvoiceByNumberAsync(string invoiceNumber);
        Task<decimal> GetTotalUnpaidInvoicesAsync();
        Task<decimal> GetAverageInvoiceValueThisMonthAsync();
        Task<int> GetOverdueInvoicesCountAsync();
        Task<IEnumerable<Invoice>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate);

    }
}