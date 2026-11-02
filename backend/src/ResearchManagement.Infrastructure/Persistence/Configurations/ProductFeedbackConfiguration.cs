using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;
public sealed class ProductFeedbackConfiguration : IEntityTypeConfiguration<ProductFeedback>
{
    public void Configure(EntityTypeBuilder<ProductFeedback> b)
    {
        b.ToTable("phan_hoi_san_pham_ung_dung", "nckh"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ProductId).HasColumnName("san_pham_id");
        b.Property(x => x.SenderAccountId).HasColumnName("nguoi_gui_id");
        b.Property(x => x.Content).HasColumnName("noi_dung");
        b.Property(x => x.CreatedAt).HasColumnName("thoi_diem").HasColumnType("timestamptz");
        b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Account>().WithMany().HasForeignKey(x => x.SenderAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
