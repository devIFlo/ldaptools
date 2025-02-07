using LdapTools.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LdapTools.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<LdapSettings> LdapSettings { get; set; }
        public DbSet<EmailSettings> EmailSettings { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<LogEntry> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<LogEntry>(entity =>
            {
                entity.Property(e => e.Message).HasColumnType("TEXT").IsRequired(false);
                entity.Property(e => e.Level).HasColumnType("INTEGER").IsRequired(false);
                entity.Property(e => e.Timestamp).HasColumnType("TIMESTAMP").IsRequired(false);
                entity.Property(e => e.Exception).HasColumnType("TEXT").IsRequired(false);
                entity.Property(e => e.Properties).HasColumnType("JSONB").IsRequired(false);
            });
        }
    }
}
