using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class ResearchPlanHistoryConfiguration : IEntityTypeConfiguration<ResearchPlanHistory>
{
    public void Configure(EntityTypeBuilder<ResearchPlanHistory> builder)
    {
        builder.ToTable("lich_su_ke_hoach", "nckh");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ResearchPlanId).HasColumnName("ke_hoach_id").IsRequired();
        builder.Property(x => x.ActorAccountId).HasColumnName("nguoi_thuc_hien_id").IsRequired();
        builder.Property(x => x.FromStatus).HasColumnName("trang_thai_cu").HasMaxLength(30).IsRequired();
        builder.Property(x => x.ToStatus).HasColumnName("trang_thai_moi").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Reason).HasColumnName("ly_do").HasMaxLength(1000).IsRequired();
        builder.Property(x => x.OccurredAt).HasColumnName("thoi_diem").IsRequired();
        builder.HasIndex(x => new { x.ResearchPlanId, x.OccurredAt });
    }
}
