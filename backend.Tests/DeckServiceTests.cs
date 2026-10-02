using backend.Data;
using backend.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests;

public class DeckServiceTests
{
    private static DeckService CreateService()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new DeckService(new AppDbContext(options));
    }

    [Fact]
    public void CreateDeck_AndGetDecksForUser_ReturnNewestFirst()
    {
        var service = CreateService();

        var first = service.CreateDeck(42, "First Deck", "Alpha", true);
        var second = service.CreateDeck(42, "Second Deck", "Beta", false);

        var decks = service.GetDecksForUser(42);

        Assert.Equal(2, decks.Count);
        Assert.Equal(second.Id, decks[0].Id);
        Assert.Equal(first.Id, decks[1].Id);
        Assert.All(decks, deck => Assert.Equal(42, deck.UserId));
    }

    [Fact]
    public void UpdateDeck_UpdatesTitleDescriptionAndVisibility()
    {
        var service = CreateService();
        var deck = service.CreateDeck(11, "Old title", "Old description", false);

        var updated = service.UpdateDeck(deck.Id, 11, "New title", "New description", true);

        Assert.NotNull(updated);
        Assert.Equal("New title", updated.Title);
        Assert.Equal("New description", updated.Description);
        Assert.True(updated.IsPublic);
    }

    [Fact]
    public void AddCard_UpdateArchiveRestoreDelete_FollowsExpectedWorkflow()
    {
        var service = CreateService();
        var deck = service.CreateDeck(77, "Study Deck", "Cards", true);

        var created = service.AddCardToDeck(deck.Id, 77, "Front", "Back", "clue", isLaTeX: false);

        Assert.NotNull(created);
        Assert.Equal(1, deck.CardCount);

        var updated = service.UpdateCardInDeck(deck.Id, created!.Id, 77, "Updated Front", "Updated Back", "new clue", true);

        Assert.NotNull(updated);
        Assert.Equal("Updated Front", updated.FrontContent);
        Assert.Equal("Updated Back", updated.BackContent);
        Assert.True(updated.IsLaTeX);

        var archived = service.ArchiveCardFromDeck(deck.Id, updated.Id, 77);

        Assert.NotNull(archived);
        Assert.True(archived.IsArchived);
        Assert.NotNull(archived.ArchivedAt);
        Assert.Contains(service.GetArchivedCardsForUser(77), card => card.Id == archived.Id);

        var restored = service.RestoreCardToDeck(archived.Id, 77, deck.Id);

        Assert.NotNull(restored);
        Assert.False(restored.IsArchived);
        Assert.Null(restored.ArchivedAt);
        Assert.Equal(1, deck.CardCount);

        var deleted = service.DeleteCardFromDeck(deck.Id, restored.Id, 77);

        Assert.True(deleted);
        Assert.Equal(0, service.GetDeckForUser(deck.Id, 77)!.CardCount);
    }

    [Fact]
    public void AddCards_WhenHammered_ProducesUniqueIdsAndMaintainsDeckIntegrity()
    {
        var service = CreateService();
        var deck = service.CreateDeck(999, "Stress Deck", "Hammer time", true);

        for (var i = 0; i < 250; i++)
        {
            var card = service.AddCardToDeck(deck.Id, 999, $"Front {i}", $"Back {i}", $"Hint {i}", i % 2 == 0);
            Assert.NotNull(card);
        }

        var persisted = service.GetDeckForUser(deck.Id, 999)!;
        var ids = persisted.Cards.Select(card => card.Id).ToList();

        Assert.Equal(250, persisted.Cards.Count);
        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.Equal(250, persisted.CardCount);
        Assert.True(ids.Min() > 0);
        Assert.True(ids.Max() >= ids.Min());
    }

    [Fact]
    public void DeleteDeck_OnlyRemovesTheRequestedUsersDeck()
    {
        var service = CreateService();
        var deck = service.CreateDeck(5, "My deck", "Owned", true);

        Assert.True(service.DeleteDeck(deck.Id, 5));
        Assert.False(service.DeleteDeck(deck.Id, 999));
        Assert.Null(service.GetDeckForUser(deck.Id, 5));
    }
}
