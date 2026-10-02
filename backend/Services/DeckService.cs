using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class DeckService
{
    private readonly AppDbContext _db;

    public DeckService(AppDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<Deck> GetDecksForUser(int userId) =>
        _db.Decks
            .Include(deck => deck.Cards)
            .Where(deck => deck.UserId == userId)
            .OrderByDescending(deck => deck.UpdatedAt)
            .ToList();

    public Deck? GetDeckForUser(int deckId, int userId) =>
        _db.Decks
            .Include(deck => deck.Cards)
            .FirstOrDefault(deck => deck.Id == deckId && deck.UserId == userId);

    public IReadOnlyList<Card> GetArchivedCardsForUser(int userId) =>
        _db.Cards
            .Where(card => _db.Decks.Any(deck => deck.Id == card.DeckId && deck.UserId == userId) && card.IsArchived)
            .OrderByDescending(card => card.ArchivedAt ?? card.CreatedAt)
            .ToList();

    public Deck CreateDeck(int userId, string title, string description, bool isPublic)
    {
        var deck = new Deck
        {
            UserId = userId,
            Title = title.Trim(),
            Description = description.Trim(),
            IsPublic = isPublic,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Cards = []
        };

        _db.Decks.Add(deck);
        _db.SaveChanges();
        return deck;
    }

    public Deck? UpdateDeck(int deckId, int userId, string? title, string? description, bool? isPublic)
    {
        var deck = GetDeckForUser(deckId, userId);
        if (deck is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            deck.Title = title.Trim();
        }

        if (description is not null)
        {
            deck.Description = description.Trim();
        }

        if (isPublic.HasValue)
        {
            deck.IsPublic = isPublic.Value;
        }

        deck.UpdatedAt = DateTime.UtcNow;
        _db.SaveChanges();
        return deck;
    }

    public bool DeleteDeck(int deckId, int userId)
    {
        var deck = _db.Decks.FirstOrDefault(d => d.Id == deckId && d.UserId == userId);
        if (deck is null)
        {
            return false;
        }

        _db.Decks.Remove(deck);
        _db.SaveChanges();
        return true;
    }

    public Card? AddCardToDeck(int deckId, int userId, string frontContent, string backContent, string? hints, bool isLaTeX)
    {
        var deck = GetDeckForUser(deckId, userId);
        if (deck is null)
        {
            return null;
        }

        var card = new Card
        {
            DeckId = deck.Id,
            FrontContent = frontContent.Trim(),
            BackContent = backContent.Trim(),
            Hints = hints?.Trim(),
            IsLaTeX = isLaTeX,
            CreatedAt = DateTime.UtcNow
        };

        deck.Cards.Add(card);
        deck.UpdatedAt = DateTime.UtcNow;
        _db.SaveChanges();
        return card;
    }

    public Card? UpdateCardInDeck(int deckId, int cardId, int userId, string? frontContent, string? backContent, string? hints, bool? isLaTeX)
    {
        var deck = GetDeckForUser(deckId, userId);
        if (deck is null)
        {
            return null;
        }

        var card = deck.Cards.FirstOrDefault(c => c.Id == cardId && !c.IsArchived);
        if (card is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(frontContent))
        {
            card.FrontContent = frontContent.Trim();
        }

        if (!string.IsNullOrWhiteSpace(backContent))
        {
            card.BackContent = backContent.Trim();
        }

        if (hints is not null)
        {
            card.Hints = hints.Trim();
        }

        if (isLaTeX.HasValue)
        {
            card.IsLaTeX = isLaTeX.Value;
        }

        deck.UpdatedAt = DateTime.UtcNow;
        _db.SaveChanges();
        return card;
    }

    public bool DeleteCardFromDeck(int deckId, int cardId, int userId)
    {
        var deck = GetDeckForUser(deckId, userId);
        if (deck is null)
        {
            return false;
        }

        var card = deck.Cards.FirstOrDefault(c => c.Id == cardId && !c.IsArchived);
        if (card is null)
        {
            return false;
        }

        deck.Cards.Remove(card);
        _db.Cards.Remove(card);
        deck.UpdatedAt = DateTime.UtcNow;
        _db.SaveChanges();
        return true;
    }

    public Card? ArchiveCardFromDeck(int deckId, int cardId, int userId)
    {
        var deck = GetDeckForUser(deckId, userId);
        if (deck is null)
        {
            return null;
        }

        var card = deck.Cards.FirstOrDefault(c => c.Id == cardId && !c.IsArchived);
        if (card is null)
        {
            return null;
        }

        card.IsArchived = true;
        card.ArchivedAt = DateTime.UtcNow;
        deck.UpdatedAt = DateTime.UtcNow;
        _db.SaveChanges();
        return card;
    }

    public Card? RestoreCardToDeck(int cardId, int userId, int targetDeckId)
    {
        var targetDeck = GetDeckForUser(targetDeckId, userId);
        if (targetDeck is null)
        {
            return null;
        }

        var card = _db.Cards
            .FirstOrDefault(c => c.Id == cardId && c.IsArchived && _db.Decks.Any(deck => deck.Id == c.DeckId && deck.UserId == userId));

        if (card is null)
        {
            return null;
        }

        var sourceDeck = _db.Decks
            .Include(deck => deck.Cards)
            .FirstOrDefault(deck => deck.Id == card.DeckId && deck.UserId == userId);

        if (sourceDeck is null)
        {
            return null;
        }

        var sourceCard = sourceDeck.Cards.FirstOrDefault(c => c.Id == cardId);
        if (sourceCard is null)
        {
            return null;
        }

        sourceDeck.Cards.Remove(sourceCard);
        sourceCard.DeckId = targetDeckId;
        sourceCard.IsArchived = false;
        sourceCard.ArchivedAt = null;
        targetDeck.Cards.Add(sourceCard);

        targetDeck.UpdatedAt = DateTime.UtcNow;
        sourceDeck.UpdatedAt = DateTime.UtcNow;
        _db.SaveChanges();
        return sourceCard;
    }

    public bool DeleteArchivedCard(int cardId, int userId)
    {
        var card = _db.Cards
            .FirstOrDefault(c => c.Id == cardId && c.IsArchived && _db.Decks.Any(deck => deck.Id == c.DeckId && deck.UserId == userId));

        if (card is null)
        {
            return false;
        }

        _db.Cards.Remove(card);
        _db.SaveChanges();
        return true;
    }
}
