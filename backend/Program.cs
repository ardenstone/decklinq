using backend.Models;
using backend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000", "http://localhost:8080")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<DeckService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("FrontendPolicy");

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    app = "DeckLinq",
    timestamp = DateTimeOffset.UtcNow
}));

app.MapGet("/api/decks", (DeckService deckService) => Results.Ok(deckService.GetDecks()));

app.MapGet("/api/decks/{id:int}", (int id, DeckService deckService) =>
{
    var deck = deckService.GetDeck(id);
    return deck is null ? Results.NotFound() : Results.Ok(deck);
});

app.MapPost("/api/decks", (CreateDeckRequest request, DeckService deckService) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new { message = "Deck title is required." });
    }

    var deck = deckService.CreateDeck(request.Title, request.Description ?? string.Empty, request.IsPublic);
    return Results.Created($"/api/decks/{deck.Id}", deck);
});

app.Run();

public record CreateDeckRequest(string Title, string? Description, bool IsPublic = false);
