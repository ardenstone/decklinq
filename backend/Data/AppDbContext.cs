using System.Text.Json;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Deck> Decks => Set<Deck>();
    public DbSet<Card> Cards => Set<Card>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(user => user.Username)
                .IsUnique();

            entity.Property(user => user.Username)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(user => user.PasswordHash)
                .IsRequired();
        });

        modelBuilder.Entity<Deck>(entity =>
        {
            entity.Property(deck => deck.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(deck => deck.Description)
                .HasMaxLength(2000);

            entity.HasIndex(deck => new { deck.UserId, deck.UpdatedAt });

            entity.HasMany(deck => deck.Cards)
                .WithOne()
                .HasForeignKey(card => card.DeckId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Card>(entity =>
        {
            entity.Property(card => card.FrontContent)
                .HasMaxLength(4000)
                .IsRequired();

            entity.Property(card => card.BackContent)
                .HasMaxLength(4000)
                .IsRequired();

            entity.Property(card => card.Hints)
                .HasMaxLength(2000);

            entity.Property(card => card.QuestionType)
                .HasColumnType("questiontype");

            entity.Property(card => card.Metadata)
                .HasColumnType("jsonb")
                .HasConversion(
                    value => value.HasValue ? value.Value.GetRawText() : null,
                    dbValue => string.IsNullOrWhiteSpace(dbValue) ? null : JsonDocument.Parse(dbValue).RootElement);

            entity.HasIndex(card => new { card.DeckId, card.IsArchived, card.CreatedAt });
        });
    }
}
