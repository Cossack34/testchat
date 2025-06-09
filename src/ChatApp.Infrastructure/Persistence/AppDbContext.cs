using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace ChatApp.Infrastructure.Persistence
{
    /// <summary>
    /// Application database context using Entity Framework Core and PostgreSQL.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Chat> Chats => Set<Chat>();
        public DbSet<Message> Messages => Set<Message>();

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(b =>
            {
                b.HasKey(u => u.Id);
                b.HasIndex(u => u.UserName).IsUnique();
                b.Property(u => u.UserName).IsRequired().HasMaxLength(100);
                b.Property(u => u.PasswordHash).IsRequired();
            });

            modelBuilder.Entity<Chat>(b =>
            {
                b.HasKey(c => c.Id);
                b.Property(c => c.Name).IsRequired();
                // Participants as array of GUIDs
                b.Property(c => c.ParticipantIds)
                    .HasColumnType("uuid[]");
            });

            modelBuilder.Entity<Message>(b =>
            {
                b.HasKey(m => m.Id);
                b.HasIndex(m => m.ChatId);
                b.Property(m => m.Content).IsRequired();
                b.Property(m => m.IsDeleted).HasDefaultValue(false);
                b.Property<NpgsqlTsVector>("SearchVector")
                    .HasColumnType("tsvector").IsRequired(false);
            });

            // Create index for full-text search
            modelBuilder.Entity<Message>()
                .HasGeneratedTsVectorColumn(
                    m => m.SearchVector,          
                    "english",                    
                    m => m.Content                
                )
                .HasIndex(m => m.SearchVector)
                .HasMethod("GIN");
        }
    }
}
