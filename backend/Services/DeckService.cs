using backend.Models;

namespace backend.Services;

public class DeckService
{
    private readonly List<Deck> _decks =
    [
        new Deck
        {
            Id = 1,
            UserId = 1,
            Title = "Spanish Basics",
            Description = "Core vocabulary for everyday conversations.",
            IsPublic = true,
            CreatedAt = DateTime.UtcNow.AddDays(-7),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            Cards =
            [
                new Card { Id = 1, DeckId = 1, FrontContent = "Hola", BackContent = "Hello", Hints = "Greeting", IsLaTeX = false },
                new Card { Id = 2, DeckId = 1, FrontContent = "Gracias", BackContent = "Thank you", Hints = "Polite phrase", IsLaTeX = false }
            ]
        },
        new Deck
        {
            Id = 2,
            UserId = 1,
            Title = "CS Fundamentals",
            Description = "Core concepts for interviews and revision.",
            IsPublic = true,
            CreatedAt = DateTime.UtcNow.AddDays(-9),
            UpdatedAt = DateTime.UtcNow.AddDays(-2),
            Cards =
            [
                new Card { Id = 3, DeckId = 2, FrontContent = "O(n)", BackContent = "Linear time complexity", Hints = "Algorithmic analysis", IsLaTeX = true },
                new Card { Id = 4, DeckId = 2, FrontContent = "JWT", BackContent = "JSON Web Token", Hints = "Authentication", IsLaTeX = false }
            ]
        }
    ];

    public IReadOnlyList<Deck> GetDecksForUser(int userId) =>
        _decks
            .Where(deck => deck.UserId == userId)
            .OrderByDescending(deck => deck.UpdatedAt)
            .ToList();

    public Deck? GetDeckForUser(int deckId, int userId) =>
        _decks.FirstOrDefault(deck => deck.Id == deckId && deck.UserId == userId);

    public Deck CreateDeck(int userId, string title, string description, bool isPublic)
    {
        var deck = new Deck
        {
            Id = _decks.Count == 0 ? 1 : _decks.Max(d => d.Id) + 1,
            UserId = userId,
            Title = title.Trim(),
            Description = description.Trim(),
            IsPublic = isPublic,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Cards = []
        };

        _decks.Add(deck);
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
        return deck;
    }

    public bool DeleteDeck(int deckId, int userId)
    {
        var deck = _decks.FirstOrDefault(d => d.Id == deckId && d.UserId == userId);
        if (deck is null)
        {
            return false;
        }

        _decks.Remove(deck);
        return true;
    }

    public Card? AddCardToDeck(int deckId, int userId, string frontContent, string backContent, string? hints, bool isLaTeX)
    {
        var deck = GetDeckForUser(deckId, userId);
        if (deck is null)
        {
            return null;
        }

        var nextId = deck.Cards.Count == 0 ? 1 : deck.Cards.Max(card => card.Id) + 1;
        var card = new Card
        {
            Id = nextId,
            DeckId = deck.Id,
            FrontContent = frontContent.Trim(),
            BackContent = backContent.Trim(),
            Hints = hints?.Trim(),
            IsLaTeX = isLaTeX,
            CreatedAt = DateTime.UtcNow
        };

        deck.Cards.Add(card);
        deck.UpdatedAt = DateTime.UtcNow;
        return card;
    }

    public Card? UpdateCardInDeck(int deckId, int cardId, int userId, string? frontContent, string? backContent, string? hints, bool? isLaTeX)
    {
        var deck = GetDeckForUser(deckId, userId);
        if (deck is null)
        {
            return null;
        }

        var card = deck.Cards.FirstOrDefault(c => c.Id == cardId);
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
        return card;
    }

    public bool DeleteCardFromDeck(int deckId, int cardId, int userId)
    {
        var deck = GetDeckForUser(deckId, userId);
        if (deck is null)
        {
            return false;
        }

        var card = deck.Cards.FirstOrDefault(c => c.Id == cardId);
        if (card is null)
        {
            return false;
        }

        deck.Cards.Remove(card);
        deck.UpdatedAt = DateTime.UtcNow;
        return true;
    }
}
