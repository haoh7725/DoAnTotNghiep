using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class ResearchTargetConfiguration
    : IEntityTypeConfiguration<ResearchTarget>
{
    public void Configure(EntityTypeBuilder<ResearchTarget> builder)
    {
        builder.ToTable("chi_tieu_nckh", "nckh");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.FacultyId)
            .HasColumnName("khoa_id")
            .IsRequired();

        builder.Property(x => x.AcademicYearId)
            .HasColumnName("nam_hoc_id")
            .IsRequired();

        builder.Property(x => x.AssignedArticleCount)
            .HasColumnName("so_bai_duoc_giao")
            .IsRequired();

        builder.Property(x => x.Deadline)
            .HasColumnName("han_hoan_thanh");
        builder.Property(x => x.JournalConferenceId).HasColumnName("tap_chi_hoi_nghi_id");
        builder.Property(x => x.ProductTypeId).HasColumnName("loai_san_pham_id").IsRequired();
        builder.HasOne<ProductType>().WithMany().HasForeignKey(x => x.ProductTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Status).HasColumnName("trang_thai").HasMaxLength(30).IsRequired();
        builder.Property(x => x.AssignedByAccountId).HasColumnName("nguoi_giao_id");
        builder.Property(x => x.AcceptedByAccountId).HasColumnName("nguoi_tiep_nhan_id");
        builder.Property(x => x.AcceptedAt).HasColumnName("tiep_nhan_luc");
        builder.Property(x => x.PublishedAt).HasColumnName("cong_bo_luc");
        builder.Property(x => x.LockedAt).HasColumnName("khoa_luc");
        builder.HasOne<JournalConference>().WithMany().HasForeignKey(x => x.JournalConferenceId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.FacultyId,
            x.AcademicYearId,
            x.JournalConferenceId
        })
        .IsUnique();
    }
}
