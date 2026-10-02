namespace backend.Models;

public class Card
{
    public int Id { get; set; }
    public int DeckId { get; set; }
    public string FrontContent { get; set; } = string.Empty;
    public string BackContent { get; set; } = string.Empty;
    public string? Hints { get; set; }
    public bool IsLaTeX { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
