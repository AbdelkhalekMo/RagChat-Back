using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chatbot.Models
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.HasKey(i => i.Id);
            builder.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(50);
            builder.Property(i => i.CustomerName).IsRequired().HasMaxLength(100);
            builder.Property(i => i.Total).HasColumnType("decimal(10,2)");
            builder.Property(i => i.Status).HasDefaultValue(InvoiceStatus.Draft);
            builder.Property(i => i.PaymentDate).IsRequired(false);
            builder.Property(i => i.CreatedAt).HasColumnType("datetime(6)").IsRequired(); 
            builder.Property(i => i.UpdatedAt).HasColumnType("datetime(6)").IsRequired(false);
            builder.Property(i => i.IsDeleted).HasDefaultValue(false);
            builder.HasMany(i => i.InvoiceDetails)
                   .WithOne(d => d.Invoice)
                   .HasForeignKey(d => d.InvoiceId);
        }
    }
}