namespace Chatbot.Services
{
    using Chatbot.DTOs.Invoices;

    public interface IInvoiceService
    {
        #region CRUD Operations
        Task<InvoiceDTO> GetInvoiceByIdAsync(int id);
        Task<IEnumerable<InvoiceDTO>> GetAllInvoicesAsync(int pageNumber = 1, int pageSize = 10);
        Task<int> GetTotalInvoicesCountAsync();
        Task<InvoiceDTO> AddInvoiceAsync(CreateInvoiceDTO invoiceDto);
        Task<InvoiceDTO> GetInvoiceByNumberAsync(string invoiceNumber);
        Task<InvoiceDTO> UpdateInvoiceAsync(int id, UpdateInvoiceDTO invoiceDto);
        Task<bool> DeleteInvoiceAsync(int id);
        #endregion
    }
}