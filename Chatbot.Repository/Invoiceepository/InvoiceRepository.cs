using Chatbot.Models;
using Chatbot.Repositories;
using Data;
using Microsoft.EntityFrameworkCore;

public class InvoiceRepository : BaseRepository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(ChatbotContext context) : base(context) { }

    public async Task<Invoice> GetByIdWithDetailsAsync(int id) =>
        await _dbSet.AsNoTracking()
            .Include(i => i.InvoiceDetails)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

    public async Task<Invoice> GetInvoiceByNumberWithDetailsAsync(string invoiceNumber) =>
        await _dbSet.AsNoTracking()
            .Include(i => i.InvoiceDetails)
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber && !i.IsDeleted);

    public async Task<IEnumerable<Invoice>> GetAllWithDetailsAsync(int pageNumber = 1, int pageSize = 10) // Pagination
    {
        return await _dbSet.AsNoTracking()
            .Include(i => i.InvoiceDetails)
            .Where(i => !i.IsDeleted)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Invoice>> GetInvoicesByCustomerWithDetailsAsync(string customerName) =>
        await _dbSet.AsNoTracking()
            .Include(i => i.InvoiceDetails)
            .Where(i => !i.IsDeleted && i.CustomerName.ToLower() == customerName.ToLower())
            .ToListAsync();

    public async Task<decimal> GetTotalInvoicesThisMonthAsync() =>
        await _dbSet.AsNoTracking()
            .Where(i => !i.IsDeleted && i.Date.Month == DateTime.Now.Month && i.Date.Year == DateTime.Now.Year)
            .SumAsync(i => i.Total);

    public async Task<int> GetInvoiceCountLastWeekAsync()
    {
        var lastWeek = DateTime.Now.AddDays(-7);
        return await _dbSet.AsNoTracking().CountAsync(i => !i.IsDeleted && i.Date >= lastWeek);
    }

    public async Task<Invoice> GetInvoiceByNumberAsync(string invoiceNumber) =>
        await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(i => !i.IsDeleted && i.InvoiceNumber == invoiceNumber);

    public async Task<decimal> GetTotalUnpaidInvoicesAsync() =>
        await _dbSet.AsNoTracking()
            .Where(i => !i.IsDeleted &&
              (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.Overdue) &&
              i.PaymentDate == null)
            .SumAsync(i => i.Total);

    public async Task<decimal> GetAverageInvoiceValueThisMonthAsync() =>
        await _dbSet.AsNoTracking()
            .Where(i => !i.IsDeleted && i.Date.Month == DateTime.Now.Month && i.Date.Year == DateTime.Now.Year)
            .AverageAsync(i => i.Total);

    public async Task<int> GetOverdueInvoicesCountAsync()
    {
        var today = DateTime.Now;
        return await _dbSet.AsNoTracking().CountAsync(i => !i.IsDeleted &&
              (i.Status == InvoiceStatus.Overdue ||
               (i.Status == InvoiceStatus.Issued && i.Date.AddDays(30) < today && !i.PaymentDate.HasValue)));
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _dbSet.AsNoTracking().CountAsync(i => !i.IsDeleted);
    }
    public async Task<IEnumerable<Invoice>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet.AsNoTracking()
            .Where(i => !i.IsDeleted && i.Date >= startDate && i.Date <= endDate)
            .ToListAsync();
    }
}