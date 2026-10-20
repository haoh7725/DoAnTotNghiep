
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class JournalConferenceConfiguration
    : IEntityTypeConfiguration<JournalConference>
{
    public void Configure(EntityTypeBuilder<JournalConference> builder)
    {
        builder.ToTable("danh_muc_tap_chi_hoi_nghi", "nckh");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Name)
            .HasColumnName("ten")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("loai")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Issn)
            .HasColumnName("issn")
            .HasMaxLength(30);

        builder.Property(x => x.Index)
            .HasColumnName("chi_so")
            .HasMaxLength(100);

        builder.Property(x => x.Classification)
            .HasColumnName("phan_loai")
            .HasMaxLength(100);

        builder.Property(x => x.Notes)
            .HasColumnName("ghi_chu");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("tao_luc")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(x => new { x.Name, x.Type });
    }
}
