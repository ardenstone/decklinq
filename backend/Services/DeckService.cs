using backend.Models;

namespace backend.Services;

public class DeckService
{
    private readonly List<Deck> _decks =
    [
        new Deck { Id = 1, Title = "Spanish Basics", Description = "Core vocabulary for everyday conversations.", IsPublic = true, CardCount = 24 },
        new Deck { Id = 2, Title = "CS Fundamentals", Description = "Core concepts for interviews and revision.", IsPublic = true, CardCount = 18 },
        new Deck { Id = 3, Title = "World Capitals", Description = "Fast geography recall for travel prep.", IsPublic = false, CardCount = 12 }
    ];

    public IReadOnlyList<Deck> GetDecks() => _decks;

    public Deck? GetDeck(int id) => _decks.FirstOrDefault(deck => deck.Id == id);

    public Deck CreateDeck(string title, string description, bool isPublic)
    {
        var deck = new Deck
        {
            Id = _decks.Count == 0 ? 1 : _decks.Max(d => d.Id) + 1,
            Title = title,
            Description = description,
            IsPublic = isPublic,
            CardCount = 0
        };

        _decks.Add(deck);
        return deck;
    }
}
