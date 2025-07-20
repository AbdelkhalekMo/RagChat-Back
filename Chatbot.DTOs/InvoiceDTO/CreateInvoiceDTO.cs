using Chatbot.Models;
using System.ComponentModel.DataAnnotations;


namespace Chatbot.DTOs.Invoices
{
    public class CreateInvoiceDTO
    {
        #region Required Properties
        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; }

        [Required]
        public decimal Total { get; set; }
        #endregion

        #region Optional Properties
        public InvoiceStatus Status { get; set; }
        public DateTime? PaymentDate { get; set; }
        public List<CreateInvoiceDetailsDTO> InvoiceDetails { get; set; }
        #endregion
    }

    public class CreateInvoiceDetailsDTO
    {
        #region Required Properties
        [Required]
        [StringLength(100)]
        public string Item { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        public decimal Price { get; set; }
        #endregion
    }
}