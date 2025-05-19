using MFADemo.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;

namespace MFADemo.Controllers
{
    // ApplicationDbContext.cs
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<MfaSettings> MfaSettings { get; set; }
        public DbSet<AuthenticationHistory> AuthenticationHistory { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Set primary keys explicitly if needed
            modelBuilder.Entity<AuthenticationHistory>()
                .HasKey(a => a.AuthHistoryId);

            modelBuilder.Entity<User>()
                .HasKey(u => u.UserId);

            modelBuilder.Entity<MfaSettings>()
                .HasKey(m => m.MfaSettingsId);

            // Set up other relationships and constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<MfaSettings>()
                .HasOne(m => m.User)
                .WithOne(u => u.MfaSettings)
                .HasForeignKey<MfaSettings>(m => m.UserId);

            modelBuilder.Entity<AuthenticationHistory>()
                .HasOne(a => a.User)
                .WithMany(u => u.AuthenticationHistory)
                .HasForeignKey(a => a.UserId);
        }
    }
}
