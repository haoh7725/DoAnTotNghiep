using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;
namespace ResearchManagement.Infrastructure.Persistence.Configurations;
public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> b)
    {
        b.ToTable("bo_mon"); b.HasKey(x=>x.Id);
        b.Property(x=>x.Id).HasColumnName("id");
        b.Property(x=>x.FacultyId).HasColumnName("khoa_id");
        b.Property(x=>x.Code).HasColumnName("ma_bo_mon").HasMaxLength(30).IsRequired();
        b.Property(x=>x.Name).HasColumnName("ten_bo_mon").HasMaxLength(200).IsRequired();
        b.HasIndex(x=>x.Code).IsUnique();
        b.HasOne<Faculty>().WithMany().HasForeignKey(x=>x.FacultyId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class AcademicYearConfiguration : IEntityTypeConfiguration<AcademicYear>
{
    public void Configure(EntityTypeBuilder<AcademicYear> b)
    {
        b.ToTable("nam_hoc"); b.HasKey(x=>x.Id);
        b.Property(x=>x.Id).HasColumnName("id");
        b.Property(x=>x.Code).HasColumnName("ma_nam_hoc").HasMaxLength(20).IsRequired();
        b.HasIndex(x=>x.Code).IsUnique();
        b.Property(x=>x.StartDate).HasColumnName("ngay_bat_dau");
        b.Property(x=>x.EndDate).HasColumnName("ngay_ket_thuc");
    }
}
