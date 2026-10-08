using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class LecturerConfiguration : IEntityTypeConfiguration<Lecturer>
{
    public void Configure(EntityTypeBuilder<Lecturer> builder)
    {
        builder.ToTable("giang_vien", "nckh");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DepartmentId).HasColumnName("bo_mon_id").IsRequired();
        builder.Property(x => x.AccountId).HasColumnName("tai_khoan_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("ma_giang_vien").HasMaxLength(30).IsRequired();
        builder.Property(x => x.FullName).HasColumnName("ho_ten").HasMaxLength(200).IsRequired();
        builder.Property(x => x.BirthDate).HasColumnName("ngay_sinh").HasColumnType("date");
        builder.Property(x => x.Gender).HasColumnName("gioi_tinh").HasMaxLength(20);
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(254);
        builder.Property(x => x.Phone).HasColumnName("so_dien_thoai").HasMaxLength(30);
        builder.Property(x => x.AcademicRank).HasColumnName("hoc_ham").HasMaxLength(100);
        builder.Property(x => x.Degree).HasColumnName("hoc_vi").HasMaxLength(100);
        builder.Property(x => x.Position).HasColumnName("chuc_vu").HasMaxLength(100);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.AccountId).IsUnique();
    }
}

public sealed class ScientificProfileConfiguration : IEntityTypeConfiguration<ScientificProfile>
{
    public void Configure(EntityTypeBuilder<ScientificProfile> builder)
    {
        builder.ToTable("ly_lich_khoa_hoc", "nckh");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.LecturerId).HasColumnName("giang_vien_id").IsRequired();
        builder.Property(x => x.Expertise).HasColumnName("chuyen_mon");
        builder.Property(x => x.ResearchFields).HasColumnName("linh_vuc_nghien_cuu");
        builder.Property(x => x.ResearchDirections).HasColumnName("huong_nghien_cuu");
        builder.Property(x => x.ActivitySummary).HasColumnName("tom_tat_hoat_dong");
        builder.Property(x => x.UpdatedAt).HasColumnName("cap_nhat_luc").IsRequired();
        builder.HasIndex(x => x.LecturerId).IsUnique();

        // Khai báo quan hệ để EF xóa lý lịch trước giảng viên khi cả hai cùng bị xóa trong một lần lưu.
        builder.HasOne<Lecturer>().WithOne().HasForeignKey<ScientificProfile>(x => x.LecturerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductTypeConfiguration : IEntityTypeConfiguration<ProductType>
{
    public void Configure(EntityTypeBuilder<ProductType> builder)
    {
        builder.ToTable("loai_san_pham", "nckh");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Code).HasColumnName("ma_loai").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Name).HasColumnName("ten_loai").HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}
