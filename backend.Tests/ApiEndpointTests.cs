using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace backend.Tests;

public class DecklinqWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
    }
}

public class ApiEndpointTests : IClassFixture<DecklinqWebApplicationFactory>
{
    private readonly DecklinqWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ApiEndpointTests(DecklinqWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsStatusOk()
    {
        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(payload);
        Assert.Equal("ok", payload!.Status);
        Assert.Equal("DeckLinq", payload.App);
    }

    [Fact]
    public async Task AuthEndpoints_RegisterAndLogin_Work()
    {
        var username = $"auth-{Guid.NewGuid():N}";
        var password = "Password123!";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(username, password));
        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        Assert.NotNull(registerPayload);
        Assert.False(string.IsNullOrWhiteSpace(registerPayload!.Token));
        Assert.Equal(username, registerPayload.User.Username);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, password));
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.NotNull(loginPayload);
        Assert.Equal(username, loginPayload!.User.Username);
        Assert.False(string.IsNullOrWhiteSpace(loginPayload.Token));
    }

    [Fact]
    public async Task AuthEndpoints_Login_WithBadCredentials_ReturnsUnauthorized()
    {
        var username = $"bad-{Guid.NewGuid():N}";
        var password = "Password123!";

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(username, password));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UsersEndpoint_Me_ReturnsCurrentUser_WhenAuthorized()
    {
        var username = $"me-{Guid.NewGuid():N}";
        var password = "Password123!";
        var token = await RegisterUserAndGetTokenAsync(username, password);

        using var authClient = _factory.CreateClient();
        authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await authClient.GetAsync("/api/users/me");
        var payload = await response.Content.ReadFromJsonAsync<UserProfileResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(username, payload!.Username);
    }

    [Fact]
    public async Task DeckEndpoints_AllowCreateReadUpdateDeleteThroughHttp()
    {
        var username = $"decks-{Guid.NewGuid():N}";
        var password = "Password123!";
        var token = await RegisterUserAndGetTokenAsync(username, password);

        using var authClient = _factory.CreateClient();
        authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createDeckResponse = await authClient.PostAsJsonAsync("/api/decks", new CreateDeckRequest("HTTP Deck", "Testing deck creation", true));
        var createdDeck = await createDeckResponse.Content.ReadFromJsonAsync<DeckResponse>();

        Assert.Equal(HttpStatusCode.Created, createDeckResponse.StatusCode);
        Assert.NotNull(createdDeck);
        Assert.Equal("HTTP Deck", createdDeck!.Title);

        var listResponse = await authClient.GetAsync("/api/decks");
        var list = await listResponse.Content.ReadFromJsonAsync<List<DeckResponse>>();

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.NotNull(list);
        Assert.Contains(list!, deck => deck.Id == createdDeck.Id);

        var updateResponse = await authClient.PutAsJsonAsync($"/api/decks/{createdDeck.Id}", new UpdateDeckRequest("Updated Deck", "New description", false));
        var updatedDeck = await updateResponse.Content.ReadFromJsonAsync<DeckResponse>();

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.NotNull(updatedDeck);
        Assert.Equal("Updated Deck", updatedDeck!.Title);
        Assert.False(updatedDeck.IsPublic);

        var deleteResponse = await authClient.DeleteAsync($"/api/decks/{createdDeck.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        var verifyResponse = await authClient.GetAsync("/api/decks");
        var remainingDecks = await verifyResponse.Content.ReadFromJsonAsync<List<DeckResponse>>();
        Assert.DoesNotContain(remainingDecks!, deck => deck.Id == createdDeck.Id);
    }

    [Fact]
    public async Task CardEndpoints_SupportCreateUpdateArchiveRestoreAndDeleteFlow()
    {
        var username = $"cards-{Guid.NewGuid():N}";
        var password = "Password123!";
        var token = await RegisterUserAndGetTokenAsync(username, password);

        using var authClient = _factory.CreateClient();
        authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var deckResponse = await authClient.PostAsJsonAsync("/api/decks", new CreateDeckRequest("Card Deck", "Card flow", true));
        var deck = await deckResponse.Content.ReadFromJsonAsync<DeckResponse>();

        Assert.Equal(HttpStatusCode.Created, deckResponse.StatusCode);
        Assert.NotNull(deck);

        var createCardResponse = await authClient.PostAsJsonAsync($"/api/decks/{deck!.Id}/cards", new CreateCardRequest("Hola", "Hello", "Greeting", false));
        var createdCard = await createCardResponse.Content.ReadFromJsonAsync<CardResponse>();

        Assert.Equal(HttpStatusCode.Created, createCardResponse.StatusCode);
        Assert.NotNull(createdCard);
        Assert.Equal("Hola", createdCard!.FrontContent);

        var getCardsResponse = await authClient.GetAsync($"/api/decks/{deck.Id}/cards");
        var cards = await getCardsResponse.Content.ReadFromJsonAsync<List<CardResponse>>();

        Assert.Equal(HttpStatusCode.OK, getCardsResponse.StatusCode);
        Assert.Contains(cards!, card => card.Id == createdCard.Id);

        var updateCardResponse = await authClient.PutAsJsonAsync($"/api/decks/{deck.Id}/cards/{createdCard.Id}", new UpdateCardRequest("Buenas", "Goodbye", "Farewell", true));
        var updatedCard = await updateCardResponse.Content.ReadFromJsonAsync<CardResponse>();

        Assert.Equal(HttpStatusCode.OK, updateCardResponse.StatusCode);
        Assert.NotNull(updatedCard);
        Assert.Equal("Buenas", updatedCard!.FrontContent);
        Assert.True(updatedCard.IsLaTeX);

        var archiveResponse = await authClient.PostAsync($"/api/decks/{deck.Id}/cards/{updatedCard.Id}/archive", null);
        var archivedCard = await archiveResponse.Content.ReadFromJsonAsync<CardResponse>();

        Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);
        Assert.NotNull(archivedCard);
        Assert.True(archivedCard!.IsArchived);

        var archivedListResponse = await authClient.GetAsync("/api/decks/archive");
        var archivedCards = await archivedListResponse.Content.ReadFromJsonAsync<List<CardResponse>>();

        Assert.Equal(HttpStatusCode.OK, archivedListResponse.StatusCode);
        Assert.Contains(archivedCards!, card => card.Id == archivedCard.Id);

        var restoreResponse = await authClient.PostAsJsonAsync($"/api/decks/archive/{archivedCard.Id}/restore", new RestoreCardRequest(deck.Id));
        var restoredCard = await restoreResponse.Content.ReadFromJsonAsync<CardResponse>();

        Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);
        Assert.NotNull(restoredCard);
        Assert.False(restoredCard!.IsArchived);

        var secondCardResponse = await authClient.PostAsJsonAsync($"/api/decks/{deck.Id}/cards", new CreateCardRequest("Second", "Card", "Archive me", false));
        var secondCard = await secondCardResponse.Content.ReadFromJsonAsync<CardResponse>();

        Assert.Equal(HttpStatusCode.Created, secondCardResponse.StatusCode);
        Assert.NotNull(secondCard);

        var secondArchiveResponse = await authClient.PostAsync($"/api/decks/{deck.Id}/cards/{secondCard!.Id}/archive", null);
        var secondArchivedCard = await secondArchiveResponse.Content.ReadFromJsonAsync<CardResponse>();

        Assert.Equal(HttpStatusCode.OK, secondArchiveResponse.StatusCode);
        Assert.NotNull(secondArchivedCard);

        var deleteArchivedResponse = await authClient.DeleteAsync($"/api/decks/archive/{secondArchivedCard!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteArchivedResponse.StatusCode);

        var deleteCardResponse = await authClient.DeleteAsync($"/api/decks/{deck.Id}/cards/{restoredCard.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteCardResponse.StatusCode);
    }

    [Fact]
    public async Task DeckImportExportEndpoints_SupportJsonAndCsvRoundTrips()
    {
        var username = $"import-export-{Guid.NewGuid():N}";
        var password = "Password123!";
        var token = await RegisterUserAndGetTokenAsync(username, password);

        using var authClient = _factory.CreateClient();
        authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var deckResponse = await authClient.PostAsJsonAsync("/api/decks", new CreateDeckRequest("Import Deck", "Round-trip deck", false));
        var deck = await deckResponse.Content.ReadFromJsonAsync<DeckResponse>();

        Assert.Equal(HttpStatusCode.Created, deckResponse.StatusCode);
        Assert.NotNull(deck);

        var jsonImportBody = """
        {
          "title": "Import Deck",
          "description": "Imported from JSON",
          "isPublic": false,
          "cards": [
            { "frontContent": "Capital of France", "backContent": "Paris", "hints": "Eiffel Tower", "isLaTeX": false },
            { "frontContent": "2 + 2", "backContent": "4", "hints": "Math", "isLaTeX": false }
          ]
        }
        """;

        var jsonImportResponse = await authClient.PostAsync($"/api/decks/{deck!.Id}/import?format=json", new StringContent(jsonImportBody, Encoding.UTF8, "application/json"));
        var jsonImportPayload = await jsonImportResponse.Content.ReadFromJsonAsync<ImportDeckResponse>();

        Assert.Equal(HttpStatusCode.OK, jsonImportResponse.StatusCode);
        Assert.NotNull(jsonImportPayload);
        Assert.Equal(2, jsonImportPayload!.ImportedCardCount);

        var exportJsonResponse = await authClient.GetAsync($"/api/decks/{deck.Id}/export?format=json");
        var exportJsonPayload = await exportJsonResponse.Content.ReadFromJsonAsync<DeckExportResponse>();

        Assert.Equal(HttpStatusCode.OK, exportJsonResponse.StatusCode);
        Assert.NotNull(exportJsonPayload);
        Assert.Equal("Import Deck", exportJsonPayload!.Title);
        Assert.Contains(exportJsonPayload.Cards, card => card.FrontContent == "Capital of France");

        var csvImportBody = "frontContent,backContent,hints,isLaTeX\n" +
            "Name of the largest ocean,Pacific,Think of the world, false\n" +
            "Square root of 81,9,Common number, true\n";

        var csvImportResponse = await authClient.PostAsync($"/api/decks/{deck.Id}/import?format=csv", new StringContent(csvImportBody, Encoding.UTF8, "text/csv"));
        var csvImportPayload = await csvImportResponse.Content.ReadFromJsonAsync<ImportDeckResponse>();

        Assert.Equal(HttpStatusCode.OK, csvImportResponse.StatusCode);
        Assert.NotNull(csvImportPayload);
        Assert.Equal(2, csvImportPayload!.ImportedCardCount);

        var exportCsvResponse = await authClient.GetAsync($"/api/decks/{deck.Id}/export?format=csv");
        var exportCsvText = await exportCsvResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, exportCsvResponse.StatusCode);
        Assert.Contains("frontContent", exportCsvText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Capital of France", exportCsvText, StringComparison.OrdinalIgnoreCase);

        var badCsvResponse = await authClient.PostAsync($"/api/decks/{deck.Id}/import?format=csv", new StringContent("frontContent,backContent\nOnlyFrontSide\n", Encoding.UTF8, "text/csv"));
        Assert.Equal(HttpStatusCode.BadRequest, badCsvResponse.StatusCode);
    }

    private async Task<string> RegisterUserAndGetTokenAsync(string username, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(username, password));
        var payload = await response.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        return payload!.Token;
    }

    private record HealthResponse(string Status, string App, DateTimeOffset Timestamp);
    private record RegisterRequest(string Username, string Password);
    private record LoginRequest(string Username, string Password);
    private record AuthResponse(string Token, UserResponse User);
    private record UserResponse(int Id, string Username, DateTime CreatedAt);
    private record UserProfileResponse(int Id, string Username, DateTime CreatedAt);
    private record DeckResponse(int Id, int UserId, string Title, string Description, bool IsPublic, List<CardResponse> Cards, DateTime CreatedAt, DateTime UpdatedAt);
    private record CardResponse(int Id, int DeckId, string FrontContent, string BackContent, string? Hints, bool IsLaTeX, bool IsArchived, DateTime? ArchivedAt, DateTime CreatedAt);
    private record CreateDeckRequest(string Title, string? Description, bool IsPublic = false);
    private record UpdateDeckRequest(string? Title, string? Description, bool? IsPublic);
    private record CreateCardRequest(string FrontContent, string BackContent, string? Hints, bool IsLaTeX = false);
    private record UpdateCardRequest(string? FrontContent, string? BackContent, string? Hints, bool? IsLaTeX);
    private record RestoreCardRequest(int DeckId);
    private record ImportDeckResponse(int ImportedCardCount);
    private record DeckExportResponse(int Id, string Title, string Description, bool IsPublic, List<DeckExportCardResponse> Cards);
    private record DeckExportCardResponse(int Id, string FrontContent, string BackContent, string? Hints, bool IsLaTeX, bool IsArchived, DateTime? ArchivedAt, DateTime CreatedAt);
}
