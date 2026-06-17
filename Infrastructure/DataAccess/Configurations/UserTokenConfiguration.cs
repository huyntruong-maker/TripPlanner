using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Configurations;

public class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        builder.HasKey(userToken => new { userToken.UserId, userToken.LoginProvider, userToken.Name });
        builder.HasIndex(userToken => userToken.UserId);
        builder.Property(userToken => userToken.LoginProvider).HasMaxLength(256);
        builder.Property(userToken => userToken.Name).HasMaxLength(256);

        builder
            .HasOne(userToken => userToken.User)
            .WithMany(user => user.UserTokens)
            .HasForeignKey(userToken => userToken.UserId);
    }
}