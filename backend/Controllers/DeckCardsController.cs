using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/decks")]
public class DeckCardsController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly DeckService _deckService;

    public DeckCardsController(AuthService authService, DeckService deckService)
    {
        _authService = authService;
        _deckService = deckService;
    }

    [HttpGet("{deckId:int}/cards")]
    public IActionResult GetCards(int deckId)
    {
        var user = _authService.GetCurrentUser(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var deck = _deckService.GetDeckForUser(deckId, user.Id);
        if (deck is null)
        {
            return NotFound();
        }

        var cards = deck.Cards.Where(card => !card.IsArchived).ToList();
        return Ok(cards);
    }

    [HttpGet("archive")]
    public IActionResult GetArchivedCards()
    {
        var user = _authService.GetCurrentUser(User);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(_deckService.GetArchivedCardsForUser(user.Id));
    }

    [HttpPost("{deckId:int}/cards")]
    public IActionResult CreateCard(int deckId, [FromBody] CreateCardRequest request)
    {
        var user = _authService.GetCurrentUser(User);
        if (user is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.FrontContent) || string.IsNullOrWhiteSpace(request.BackContent))
        {
            return BadRequest(new { message = "Front and back content are required." });
        }

        var card = _deckService.AddCardToDeck(deckId, user.Id, request.FrontContent, request.BackContent, request.Hints, request.IsLaTeX);
        return card is null ? NotFound() : Created($"/api/decks/{deckId}/cards/{card.Id}", card);
    }

    [HttpPut("{deckId:int}/cards/{cardId:int}")]
    public IActionResult UpdateCard(int deckId, int cardId, [FromBody] UpdateCardRequest request)
    {
        var user = _authService.GetCurrentUser(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var card = _deckService.UpdateCardInDeck(deckId, cardId, user.Id, request.FrontContent, request.BackContent, request.Hints, request.IsLaTeX);
        return card is null ? NotFound() : Ok(card);
    }

    [HttpPost("{deckId:int}/cards/{cardId:int}/archive")]
    public IActionResult ArchiveCard(int deckId, int cardId)
    {
        var user = _authService.GetCurrentUser(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var card = _deckService.ArchiveCardFromDeck(deckId, cardId, user.Id);
        return card is null ? NotFound() : Ok(card);
    }

    [HttpPost("archive/{cardId:int}/restore")]
    public IActionResult RestoreCard(int cardId, [FromBody] RestoreCardRequest request)
    {
        var user = _authService.GetCurrentUser(User);
        if (user is null)
        {
            return Unauthorized();
        }

        if (request.DeckId <= 0)
        {
            return BadRequest(new { message = "Destination deck is required." });
        }

        var card = _deckService.RestoreCardToDeck(cardId, user.Id, request.DeckId);
        return card is null ? NotFound() : Ok(card);
    }

    [HttpDelete("archive/{cardId:int}")]
    public IActionResult DeleteArchivedCard(int cardId)
    {
        var user = _authService.GetCurrentUser(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var removed = _deckService.DeleteArchivedCard(cardId, user.Id);
        return removed ? NoContent() : NotFound();
    }

    [HttpDelete("{deckId:int}/cards/{cardId:int}")]
    public IActionResult DeleteCard(int deckId, int cardId)
    {
        var user = _authService.GetCurrentUser(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var removed = _deckService.DeleteCardFromDeck(deckId, cardId, user.Id);
        return removed ? NoContent() : NotFound();
    }

    public record CreateCardRequest(string FrontContent, string BackContent, string? Hints, bool IsLaTeX = false);
    public record UpdateCardRequest(string? FrontContent, string? BackContent, string? Hints, bool? IsLaTeX);
    public record RestoreCardRequest(int DeckId);
}
