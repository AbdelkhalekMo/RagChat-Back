namespace Chatbot.DTOs.Invoices
{
    public class InvoiceDetailsDTO
    {
        public int Id { get; set; }
        public string Item { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}