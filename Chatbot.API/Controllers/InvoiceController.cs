using Chatbot.DTOs.Invoices;
using Chatbot.Services;
using Microsoft.AspNetCore.Mvc;
using Chatbot.DTOs;

namespace Chatbot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        #region CRUD Endpoints
        [HttpGet("{id}")]
        public async Task<ActionResult<InvoiceDTO>> GetInvoice(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            return invoice == null ? NotFound() : Ok(invoice);
        }

        [HttpGet("all")]
        public async Task<ActionResult<ApiResponse<IEnumerable<InvoiceDTO>>>> GetAllInvoices([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var invoices = await _invoiceService.GetAllInvoicesAsync(pageNumber, pageSize);
            var totalItems = await _invoiceService.GetTotalInvoicesCountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            return Ok(new ApiResponse<IEnumerable<InvoiceDTO>>
            {
                success = true,
                data = invoices,
                message = "تم جلب الفواتير بنجاح",
                totalPages = totalPages
            });
        }

        [HttpGet("by-number/{invoiceNumber}")]
        public async Task<ActionResult<InvoiceDTO>> GetInvoiceByNumber(string invoiceNumber)
        {
            var invoice = await _invoiceService.GetInvoiceByNumberAsync(invoiceNumber);
            return invoice == null ? NotFound() : Ok(invoice);
        }

        [HttpPost]
        public async Task<ActionResult<InvoiceDTO>> CreateInvoice(CreateInvoiceDTO invoiceDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var invoice = await _invoiceService.AddInvoiceAsync(invoiceDto);
            return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, invoice);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<InvoiceDTO>> UpdateInvoice(int id, UpdateInvoiceDTO invoiceDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var invoice = await _invoiceService.UpdateInvoiceAsync(id, invoiceDto);
            return invoice == null ? NotFound() : Ok(invoice);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteInvoice(int id)
        {
            var result = await _invoiceService.DeleteInvoiceAsync(id);
            return result ? Ok() : NotFound();
        }
        #endregion
    }
}