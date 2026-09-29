using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;
namespace ResearchManagement.Infrastructure.Persistence.Configurations;
public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> b)
    {
        b.ToTable("tai_khoan"); b.HasKey(x=>x.Id);
        b.Property(x=>x.Id).HasColumnName("id");
        b.Property(x=>x.Username).HasColumnName("ten_dang_nhap").HasMaxLength(100).IsRequired();
        b.HasIndex(x=>x.Username).IsUnique();
        b.Property(x=>x.PasswordHash).HasColumnName("mat_khau_hash").HasMaxLength(255).IsRequired();
        b.Property(x=>x.FullName).HasColumnName("ho_ten").HasMaxLength(200).IsRequired();
        b.Property(x=>x.Email).HasColumnName("email").HasMaxLength(254);
        b.Property(x=>x.Status).HasColumnName("trang_thai").HasMaxLength(20).IsRequired();
        b.Property(x=>x.CreatedAt).HasColumnName("tao_luc");
    }
}
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
public sealed class RoleAssignmentConfiguration : IEntityTypeConfiguration<RoleAssignment>
{
    public void Configure(EntityTypeBuilder<RoleAssignment> b)
    {
        b.ToTable("phan_quyen"); b.HasKey(x=>x.Id);
        b.Property(x=>x.Id).HasColumnName("id");
        b.Property(x=>x.AccountId).HasColumnName("tai_khoan_id");
        b.Property(x=>x.Role).HasColumnName("vai_tro").HasMaxLength(30).IsRequired();
        b.Property(x=>x.Scope).HasColumnName("pham_vi").HasMaxLength(20).IsRequired();
        b.Property(x=>x.FacultyId).HasColumnName("khoa_id");
        b.Property(x=>x.DepartmentId).HasColumnName("bo_mon_id");
        b.HasOne<Account>().WithMany().HasForeignKey(x=>x.AccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
