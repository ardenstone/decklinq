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

        return Ok(_deckService.GetDecksForUser(user.Id));
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
        return deck is null ? NotFound() : Ok(deck);
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
