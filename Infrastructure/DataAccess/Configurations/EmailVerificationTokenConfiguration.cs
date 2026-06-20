using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Configurations;

public class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(EntityTypeBuilder<EmailVerificationToken> builder)
    {
        builder.HasKey(t => t.Id).HasName("EmailVerificationTokenId");

        builder.Property(t => t.Token)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(t => t.ExpiresAt).IsRequired();
        builder.Property(t => t.ConsumedAt).IsRequired(false);
        builder.Property(t => t.CreatedAt).IsRequired();

        builder.HasOne(t => t.User)
            .WithMany(u => u.EmailVerificationTokens)
            .HasForeignKey(t => t.UserId)
            .HasConstraintName("FK_EmailVerificationTokens_Users")
            .OnDelete(DeleteBehavior.Cascade);

        // Fast look-up by token value (e.g. when a user clicks the verification link)
        builder.HasIndex(t => t.Token)
            .HasDatabaseName("IX_EmailVerificationTokens_Token")
            .IsUnique();

        // Fast look-up of all tokens for a given user
        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("IX_EmailVerificationTokens_UserId");
    }
}
