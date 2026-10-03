using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class ResearchPlanItemConfiguration
    : IEntityTypeConfiguration<ResearchPlanItem>
{
    public void Configure(EntityTypeBuilder<ResearchPlanItem> builder)
    {
        builder.ToTable("noi_dung_ke_hoach", "nckh");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .HasColumnName("id");

        builder.Property(item => item.ResearchPlanId)
            .HasColumnName("ke_hoach_id")
            .IsRequired();

        builder.Property(item => item.ProductTypeId)
            .HasColumnName("loai_san_pham_id")
            .IsRequired();

        builder.Property(item => item.Name)
            .HasColumnName("ten_noi_dung")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(item => item.CommittedQuantity)
            .HasColumnName("so_luong_cam_ket")
            .IsRequired();

        builder.Property(item => item.Deadline)
            .HasColumnName("han_hoan_thanh")
            .HasColumnType("date");

        builder.Property(item => item.Status)
            .HasColumnName("trang_thai")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(item => item.Result)
            .HasColumnName("ket_qua");

        builder.Property(item => item.DifficultyProposal)
            .HasColumnName("kho_khan_de_xuat");

        builder.Property(item => item.UpdatedAt)
            .HasColumnName("cap_nhat_luc")
            .HasColumnType("timestamptz")
            .IsRequired();
    }
}