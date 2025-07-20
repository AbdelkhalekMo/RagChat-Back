namespace Chatbot.Models
{
    public class Invoice : BaseEntity
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime Date { get; set; }
        public string CustomerName { get; set; }
        public decimal Total { get; set; }
        public InvoiceStatus Status { get; set; }
        public DateTime? PaymentDate { get; set; }
        public ICollection<InvoiceDetails> InvoiceDetails { get; set; }
    }
}