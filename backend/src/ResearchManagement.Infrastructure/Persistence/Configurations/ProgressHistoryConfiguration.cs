using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class ProgressHistoryConfiguration
    : IEntityTypeConfiguration<ProgressHistory>
{
    public void Configure(EntityTypeBuilder<ProgressHistory> builder)
    {
        builder.ToTable("lich_su_tien_do", "nckh");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.ResearchPlanItemId)
            .HasColumnName("noi_dung_id")
            .IsRequired();

        builder.Property(x => x.UpdatedByAccountId)
            .HasColumnName("nguoi_cap_nhat_id")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("trang_thai")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Result)
            .HasColumnName("ket_qua");

        builder.Property(x => x.DifficultyProposal)
            .HasColumnName("kho_khan_de_xuat");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("thoi_diem")
            .IsRequired();
    }
}