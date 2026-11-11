using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class ProductClassificationConfiguration : IEntityTypeConfiguration<ProductClassification>
{
    public void Configure(EntityTypeBuilder<ProductClassification> b)
    {
        b.ToTable("phan_loai_san_pham", "nckh"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ProductTypeId).HasColumnName("loai_san_pham_id");
        b.Property(x => x.Code).HasColumnName("ma_phan_loai").HasMaxLength(50);
        b.Property(x => x.Name).HasColumnName("ten_phan_loai").HasMaxLength(300);
        b.Property(x => x.IsActive).HasColumnName("dang_su_dung");
        b.HasIndex(x => new { x.ProductTypeId, x.Code }).IsUnique();
        b.HasOne<ProductType>().WithMany().HasForeignKey(x => x.ProductTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}
