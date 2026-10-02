namespace backend.Models;

public class Deck
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public int CardCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
