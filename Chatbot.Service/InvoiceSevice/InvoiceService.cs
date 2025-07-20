using Chatbot.DTOs.Invoices;
using Chatbot.Models;
using Chatbot.Repositories;
using Data;
using System.ComponentModel.DataAnnotations;

namespace Chatbot.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly ChatbotContext _context;

        public InvoiceService(IInvoiceRepository invoiceRepository, ChatbotContext context)
        {
            _invoiceRepository = invoiceRepository;
            _context = context;
        }

        #region CRUD Operations
        public async Task<InvoiceDTO> GetInvoiceByIdAsync(int id)
        {
            var invoice = await _invoiceRepository.GetByIdWithDetailsAsync(id);
            return MapToDto(invoice);
        }

        public async Task<IEnumerable<InvoiceDTO>> GetAllInvoicesAsync(int pageNumber = 1, int pageSize = 10) // Pagination
        {
            if (pageNumber < 1 || pageSize < 1)
                throw new ArgumentException("PageNumber and PageSize must be positive.");

            var invoices = await _invoiceRepository.GetAllWithDetailsAsync(pageNumber, pageSize);
            return invoices.Select(MapToDto);
        }

        public async Task<int> GetTotalInvoicesCountAsync()
        {
            return await _invoiceRepository.GetTotalCountAsync();
        }

        public async Task<InvoiceDTO> AddInvoiceAsync(CreateInvoiceDTO invoiceDto)
        {
            if (invoiceDto == null || !IsValidInvoice(invoiceDto))
                throw new ArgumentException("Invalid invoice data.");
            var invoice = MapToEntity(invoiceDto);
            await _invoiceRepository.AddAsync(invoice); 
            return MapToDto(invoice);
        }

        public async Task<InvoiceDTO> GetInvoiceByNumberAsync(string invoiceNumber)
        {
            var invoice = await _invoiceRepository.GetInvoiceByNumberWithDetailsAsync(invoiceNumber);
            return MapToDto(invoice);
        }

        public async Task<InvoiceDTO> UpdateInvoiceAsync(int id, UpdateInvoiceDTO invoiceDto)
        {
            var existingInvoice = await _invoiceRepository.GetByIdWithDetailsAsync(id);
            if (existingInvoice == null) return null;

            UpdateFromDto(existingInvoice, invoiceDto);

            if (invoiceDto.InvoiceDetails != null)
            {
                foreach (var detail in existingInvoice.InvoiceDetails.ToList())
                {
                    if (!invoiceDto.InvoiceDetails.Any(d => d.Id == detail.Id))
                    {
                        detail.IsDeleted = true;
                    }
                }
                foreach (var dtoDetail in invoiceDto.InvoiceDetails)
                {
                    var existingDetail = existingInvoice.InvoiceDetails
                        .FirstOrDefault(d => d.Id == dtoDetail.Id);
                    if (existingDetail != null)
                    {
                        existingDetail.Item = dtoDetail.Item;
                        existingDetail.Quantity = dtoDetail.Quantity;
                        existingDetail.Price = dtoDetail.Price;
                    }
                    else
                    {
                        var newDetail = new InvoiceDetails
                        {
                            InvoiceId = id,
                            Item = dtoDetail.Item,
                            Quantity = dtoDetail.Quantity,
                            Price = dtoDetail.Price,
                            CreatedAt = DateTime.Now
                        };
                        existingInvoice.InvoiceDetails.Add(newDetail);
                    }
                }
            }

            await _invoiceRepository.UpdateAsync(existingInvoice); 
            return MapToDto(existingInvoice);
        }

        public async Task<bool> DeleteInvoiceAsync(int id)
        {
            var invoice = await _invoiceRepository.GetByIdWithDetailsAsync(id);
            if (invoice == null) return false;

            await _invoiceRepository.DeleteAsync(invoice); 
            return true;
        }
        #endregion

        #region Private Methods
        private bool IsValidInvoice(object invoiceDto)
        {
            return Validator.TryValidateObject(invoiceDto, new ValidationContext(invoiceDto), null, true);
        }

        private InvoiceDTO MapToDto(Invoice invoice)
        {
            if (invoice == null) return null;
            return new InvoiceDTO
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                Date = invoice.Date,
                CustomerName = invoice.CustomerName,
                Total = invoice.Total,
                Status = invoice.Status,
                PaymentDate = invoice.PaymentDate,
                InvoiceDetails = invoice.InvoiceDetails?
                    .Where(d => !d.IsDeleted)
                    .Select(d => new InvoiceDetailsDTO
                    {
                        Id = d.Id,
                        Item = d.Item,
                        Quantity = d.Quantity,
                        Price = d.Price
                    }).ToList()
            };
        }

        private Invoice MapToEntity(CreateInvoiceDTO invoiceDto)
        {
            if (invoiceDto == null) return null;
            return new Invoice
            {
                InvoiceNumber = invoiceDto.InvoiceNumber,
                Date = invoiceDto.Date,
                CustomerName = invoiceDto.CustomerName,
                Total = invoiceDto.Total,
                Status = invoiceDto.Status,
                PaymentDate = invoiceDto.PaymentDate,
                InvoiceDetails = invoiceDto.InvoiceDetails?.Select(d => new InvoiceDetails
                {
                    Item = d.Item,
                    Quantity = d.Quantity,
                    Price = d.Price
                }).ToList()
            };
        }

        private void UpdateFromDto(Invoice invoice, UpdateInvoiceDTO invoiceDto)
        {
            if (invoiceDto == null) return;
            invoice.InvoiceNumber = invoiceDto.InvoiceNumber;
            invoice.Date = invoiceDto.Date;
            invoice.CustomerName = invoiceDto.CustomerName;
            invoice.Total = invoiceDto.Total;
            invoice.Status = invoiceDto.Status;
            invoice.PaymentDate = invoiceDto.PaymentDate;
        }
        #endregion
    }
}