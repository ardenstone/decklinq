using System.Text;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/decks")]
public class DecksController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly DeckService _deckService;

    public DecksController(AuthService authService, DeckService deckService)
    {
        _authService = authService;
        _deckService = deckService;
    }

    [HttpGet]
    public IActionResult GetDecks()
    {
        var user = _authService.GetCurrentUser(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var decks = _deckService
            .GetDecksForUser(user.Id)
            .Select(deck =>
            {
                deck.Cards = deck.Cards.Where(card => !card.IsArchived).ToList();
                return deck;
            })
            .ToList();

        return Ok(decks);
    }

    [HttpGet("{id:int}")]
    public IActionResult GetDeck(int id)
    {
        var user = _authService.GetCurrentUser(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var deck = _deckService.GetDeckForUser(id, user.Id);
        if (deck is null)
        {
            return NotFound();
        }

        deck.Cards = deck.Cards.Where(card => !card.IsArchived).ToList();
        return Ok(deck);
    }

    [HttpGet("{id:int}/export")]
    public IActionResult ExportDeck(int id, [FromQuery] string format = "json")
    {
        var user = _authService.GetCurrentUser(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var deck = _deckService.GetDeckForUser(id, user.Id);
        if (deck is null)
        {
            return NotFound();
        }

        var normalizedFormat = format.Trim();
        var safeName = string.Concat(deck.Title.Select(character => char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '-')).Trim('-');
        var fileName = string.IsNullOrWhiteSpace(safeName) ? $"deck-{deck.Id}" : safeName;

        if (string.Equals(normalizedFormat, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var csvContent = _deckService.ExportDeckAsCsv(id, user.Id);
            return File(Encoding.UTF8.GetBytes(csvContent), "text/csv", $"{fileName}.csv");
        }

        if (string.Equals(normalizedFormat, "json", StringComparison.OrdinalIgnoreCase))
        {
            var jsonContent = _deckService.ExportDeckAsJson(id, user.Id);
            return File(Encoding.UTF8.GetBytes(jsonContent), "application/json", $"{fileName}.json");
        }

        return BadRequest(new { message = "Unsupported export format. Use json or csv." });
    }

    [HttpPost("{id:int}/import")]
    public async Task<IActionResult> ImportDeck(int id, [FromQuery] string format = "json")
    {
        var user = _authService.GetCurrentUser(User);
        if (user is null)
        {
            return Unauthorized();
        }

        if (_deckService.GetDeckForUser(id, user.Id) is null)
        {
            return NotFound();
        }

        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var payload = await reader.ReadToEndAsync();

        try
        {
            var importedCount = _deckService.ImportDeckCards(id, user.Id, payload, format);
            return Ok(new { importedCardCount = importedCount });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    public IActionResult CreateDeck([FromBody] CreateDeckRequest request)
    {
        var user = _authService.GetCurrentUser(User);
        if (user is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { message = "Deck title is required." });
        }

        var deck = _deckService.CreateDeck(user.Id, request.Title, request.Description ?? string.Empty, request.IsPublic);
        return Created($"/api/decks/{deck.Id}", deck);
    }

    [HttpPut("{id:int}")]
    public IActionResult UpdateDeck(int id, [FromBody] UpdateDeckRequest request)
    {
        var user = _authService.GetCurrentUser(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var deck = _deckService.UpdateDeck(id, user.Id, request.Title, request.Description, request.IsPublic);
        return deck is null ? NotFound() : Ok(deck);
    }

    [HttpDelete("{id:int}")]
    public IActionResult DeleteDeck(int id)
    {
        var user = _authService.GetCurrentUser(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var removed = _deckService.DeleteDeck(id, user.Id);
        return removed ? NoContent() : NotFound();
    }

    public record CreateDeckRequest(string Title, string? Description, bool IsPublic = false);
    public record UpdateDeckRequest(string? Title, string? Description, bool? IsPublic);
}
