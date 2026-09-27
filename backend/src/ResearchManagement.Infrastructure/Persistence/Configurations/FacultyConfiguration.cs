using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class FacultyConfiguration : IEntityTypeConfiguration<Faculty>
{
    public void Configure(EntityTypeBuilder<Faculty> builder)
    {
        builder.ToTable("khoa");
        builder.HasKey(faculty => faculty.Id);
        builder.Property(faculty => faculty.Id).HasColumnName("id");
        builder.Property(faculty => faculty.Code).HasColumnName("ma_khoa").HasMaxLength(30).IsRequired();
        builder.Property(faculty => faculty.Name).HasColumnName("ten_khoa").HasMaxLength(200).IsRequired();
        builder.Property(faculty => faculty.CreatedAt).HasColumnName("tao_luc");
        builder.Property(faculty => faculty.UpdatedAt).HasColumnName("cap_nhat_luc");
        builder.HasIndex(faculty => faculty.Code).IsUnique();
    }
}
