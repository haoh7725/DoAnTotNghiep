using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("tai_khoan", "nckh");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.Username).HasColumnName("ten_dang_nhap").HasMaxLength(100).IsRequired();
        builder.Property(a => a.PasswordHash).HasColumnName("mat_khau_hash").HasMaxLength(255).IsRequired();
        builder.Property(a => a.FullName).HasColumnName("ho_ten").HasMaxLength(200).IsRequired();
        builder.Property(a => a.Email).HasColumnName("email").HasMaxLength(254);
        builder.Property(a => a.Status).HasColumnName("trang_thai").HasMaxLength(20).IsRequired();
        builder.Property(a => a.CreatedAt).HasColumnName("tao_luc").HasColumnType("timestamptz").IsRequired();
        builder.Ignore(a => a.IsActive);
        builder.HasIndex(a => a.Username).IsUnique();

        builder.HasMany(a => a.RoleAssignments)
            .WithOne()
            .HasForeignKey(r => r.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(a => a.RoleAssignments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
