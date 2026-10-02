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
        return deck is null ? NotFound() : Ok(deck.Cards);
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
}
