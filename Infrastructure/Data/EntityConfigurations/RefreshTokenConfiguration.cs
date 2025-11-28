using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("sys_refresh_token", "public");
        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(256);
        builder.Property(rt => rt.ExpiresAt)
            .IsRequired();

        //添加 Token 索引
        builder.HasIndex(rt => rt.Token).IsUnique();
        // 用户索引
        builder.HasIndex(rt => rt.UserId);

        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}