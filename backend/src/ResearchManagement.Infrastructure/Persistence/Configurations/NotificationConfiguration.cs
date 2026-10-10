
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration
    : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("thong_bao", "nckh");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.RecipientAccountId)
            .HasColumnName("nguoi_nhan_id")
            .IsRequired();

        builder.Property(x => x.ResearchPlanItemId)
            .HasColumnName("noi_dung_id");

        builder.Property(x => x.FeedbackId)
            .HasColumnName("phan_hoi_id");

        builder.Property(x => x.ResearchPlanId)
            .HasColumnName("ke_hoach_id");

        builder.Property(x => x.ScientificProductId)
            .HasColumnName("san_pham_id");

        builder.Property(x => x.Type)
            .HasColumnName("loai")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Content)
            .HasColumnName("noi_dung")
            .IsRequired();

        builder.Property(x => x.DeduplicationKey)
            .HasColumnName("khoa_chong_trung")
            .HasMaxLength(191)
            .IsRequired();

        builder.Property(x => x.Channel)
            .HasColumnName("kenh")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.DeliveryStatus)
            .HasColumnName("trang_thai_gui")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.RetryCount)
            .HasColumnName("so_lan_thu")
            .IsRequired();

        builder.Property(x => x.ScheduledAt)
            .HasColumnName("gui_sau")
            .IsRequired();

        builder.Property(x => x.DeliveredUntil)
            .HasColumnName("giu_den");

        builder.Property(x => x.SentAt)
            .HasColumnName("da_gui_luc");

        builder.Property(x => x.ReadAt)
            .HasColumnName("da_doc_luc");

        builder.Property(x => x.DeliveryError)
            .HasColumnName("loi_gui");

        builder.HasIndex(x => new
        {
            x.RecipientAccountId,
            x.Channel,
            x.DeduplicationKey
        }).IsUnique();

        builder.Ignore(x => x.IsRead);
    }
}
