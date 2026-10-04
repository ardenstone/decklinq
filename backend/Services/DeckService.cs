using System.Text;
using System.Text.Json;
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

    public Card? AddCardToDeck(int deckId, int userId, string frontContent, string backContent, string? hints, bool isLaTeX, string? questionType = null, string? metadata = null)
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
            QuestionType = string.IsNullOrWhiteSpace(questionType) ? null : questionType,
            Metadata = string.IsNullOrWhiteSpace(metadata) ? null : metadata,
            CreatedAt = DateTime.UtcNow
        };

        deck.Cards.Add(card);
        deck.UpdatedAt = DateTime.UtcNow;
        _db.SaveChanges();
        return card;
    }

    public Card? UpdateCardInDeck(int deckId, int cardId, int userId, string? frontContent, string? backContent, string? hints, bool? isLaTeX, string? questionType = null, string? metadata = null)
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

        if (questionType is not null)
        {
            card.QuestionType = string.IsNullOrWhiteSpace(questionType) ? null : questionType;
        }

        if (metadata is not null)
        {
            card.Metadata = string.IsNullOrWhiteSpace(metadata) ? null : metadata;
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

    public string ExportDeckAsJson(int deckId, int userId)
    {
        var deck = GetDeckForUser(deckId, userId) ?? throw new InvalidOperationException("Deck not found.");
        var payload = new DeckExportDocument(
            deck.Id,
            deck.Title,
            deck.Description,
            deck.IsPublic,
            deck.Cards
                .Where(card => !card.IsArchived)
                .Select(card => new DeckExportCard(
                    card.Id,
                    card.FrontContent,
                    card.BackContent,
                    card.Hints,
                    card.IsLaTeX,
                    card.IsArchived,
                    card.ArchivedAt,
                    card.CreatedAt))
                .ToList());

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    public string ExportDeckAsCsv(int deckId, int userId)
    {
        var deck = GetDeckForUser(deckId, userId) ?? throw new InvalidOperationException("Deck not found.");
        var builder = new StringBuilder();

        builder.AppendLine("frontContent,backContent,hints,isLaTeX");

        foreach (var card in deck.Cards.Where(card => !card.IsArchived))
        {
            builder.Append(CsvEscape(card.FrontContent));
            builder.Append(',');
            builder.Append(CsvEscape(card.BackContent));
            builder.Append(',');
            builder.Append(CsvEscape(card.Hints ?? string.Empty));
            builder.Append(',');
            builder.Append(card.IsLaTeX ? "true" : "false");
            builder.AppendLine();
        }

        return builder.ToString();
    }

    public int ImportDeckCards(int deckId, int userId, string payload, string format)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("Import payload is empty.");
        }

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            return ImportDeckCsv(deckId, userId, payload);
        }

        if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
        {
            return ImportDeckJson(deckId, userId, payload);
        }

        throw new ArgumentException("Unsupported import format. Use json or csv.");
    }

    private int ImportDeckJson(int deckId, int userId, string payload)
    {
        var document = JsonSerializer.Deserialize<DeckImportDocument>(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (document is null || document.Cards is null || document.Cards.Count == 0)
        {
            throw new ArgumentException("No cards were found in the JSON payload.");
        }

        var count = 0;

        foreach (var card in document.Cards)
        {
            if (string.IsNullOrWhiteSpace(card.FrontContent) || string.IsNullOrWhiteSpace(card.BackContent))
            {
                throw new ArgumentException("Each card requires frontContent and backContent.");
            }

            AddCardToDeck(deckId, userId, card.FrontContent, card.BackContent, card.Hints, card.IsLaTeX);
            count++;
        }

        return count;
    }

    private int ImportDeckCsv(int deckId, int userId, string payload)
    {
        var rows = ParseCsvRows(payload);
        if (rows.Count < 2)
        {
            throw new ArgumentException("The CSV file does not contain a valid header row.");
        }

        var header = rows[0].Select(value => value.Trim()).ToList();
        var frontIndex = FindColumnIndex(header, "frontcontent", "front", "frontContent");
        var backIndex = FindColumnIndex(header, "backcontent", "back", "backContent");
        var hintsIndex = FindColumnIndex(header, "hints", "hint");
        var latexIndex = FindColumnIndex(header, "islatex", "latex", "isLaTeX");

        if (frontIndex < 0 || backIndex < 0)
        {
            throw new ArgumentException("CSV headers must include frontContent and backContent.");
        }

        var count = 0;

        for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            var values = rows[rowIndex];
            if (values.Count == 0 || values.All(value => string.IsNullOrWhiteSpace(value)))
            {
                continue;
            }

            var frontContent = GetCellValue(values, frontIndex);
            var backContent = GetCellValue(values, backIndex);

            if (string.IsNullOrWhiteSpace(frontContent) || string.IsNullOrWhiteSpace(backContent))
            {
                throw new ArgumentException($"Row {rowIndex + 1} is missing frontContent or backContent.");
            }

            var hints = hintsIndex >= 0 ? GetCellValue(values, hintsIndex) : null;
            var isLaTeX = latexIndex >= 0 && bool.TryParse(GetCellValue(values, latexIndex), out var parsed) ? parsed : false;

            AddCardToDeck(deckId, userId, frontContent, backContent, hints, isLaTeX);
            count++;
        }

        if (count == 0)
        {
            throw new ArgumentException("The CSV file did not include any valid card rows.");
        }

        return count;
    }

    private static int FindColumnIndex(IReadOnlyList<string> headers, params string[] candidates)
    {
        for (var index = 0; index < headers.Count; index++)
        {
            var candidate = headers[index];
            if (candidates.Any(name => string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase)))
            {
                return index;
            }
        }

        return -1;
    }

    private static string GetCellValue(IReadOnlyList<string> row, int index)
    {
        if (index < 0 || index >= row.Count)
        {
            return string.Empty;
        }

        return row[index].Trim();
    }

    private static List<List<string>> ParseCsvRows(string content)
    {
        var rows = new List<List<string>>();
        using var reader = new StringReader(content);
        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            rows.Add(ParseCsvLine(line));
        }

        return rows;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var value = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];

            if (character == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    value.Append('"');
                    index++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (character == ',' && !inQuotes)
            {
                result.Add(value.ToString());
                value.Clear();
                continue;
            }

            value.Append(character);
        }

        result.Add(value.ToString());
        return result;
    }

    private static string CsvEscape(string value)
    {
        var escaped = value.Replace("\"", "\"\"");
        if (escaped.Contains(',') || escaped.Contains('"') || escaped.Contains('\n') || escaped.Contains('\r'))
        {
            return $"\"{escaped}\"";
        }

        return escaped;
    }

    public record DeckExportDocument(int Id, string Title, string Description, bool IsPublic, List<DeckExportCard> Cards);
    public record DeckExportCard(int Id, string FrontContent, string BackContent, string? Hints, bool IsLaTeX, bool IsArchived, DateTime? ArchivedAt, DateTime CreatedAt);

    private sealed class DeckImportDocument
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public bool IsPublic { get; set; }
        public List<DeckImportCard>? Cards { get; set; }
    }

    private sealed class DeckImportCard
    {
        public string? FrontContent { get; set; }
        public string? BackContent { get; set; }
        public string? Hints { get; set; }
        public bool IsLaTeX { get; set; }
    }
}
