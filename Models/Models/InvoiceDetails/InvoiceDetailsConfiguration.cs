using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chatbot.Models
{
    public class InvoiceDetailsConfiguration : IEntityTypeConfiguration<InvoiceDetails>
    {
        public void Configure(EntityTypeBuilder<InvoiceDetails> builder)
        {
            builder.HasKey(d => d.Id);
            builder.Property(d => d.InvoiceId).IsRequired();
            builder.Property(d => d.Item).IsRequired().HasMaxLength(100);
            builder.Property(d => d.Price).HasColumnType("decimal(10,2)");
            builder.Property(d => d.Quantity).IsRequired();
            builder.Property(d => d.CreatedAt).HasColumnType("datetime(6)").IsRequired(); 
            builder.Property(d => d.UpdatedAt).HasColumnType("datetime(6)").IsRequired(false);
            builder.Property(d => d.IsDeleted).HasDefaultValue(false);
        }
    }
}