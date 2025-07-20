namespace Chatbot.Models
{
    public class InvoiceDetails : BaseEntity
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public string Item { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public Invoice Invoice { get; set; }
    }
}