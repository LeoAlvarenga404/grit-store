using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Grit.IntegrationTests;

public class ProbeNoteTests(ApiFactory factory)
{
    private const string NotesPath = "/api/v1/probe/notes";

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateNote_ValidText_Returns201AndTheNoteCanBeRead()
    {
        var text = UniqueText();

        var created = await _client.PostAsJsonAsync(NotesPath, new { text }, TestContext.Current.CancellationToken);

        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("id").GetGuid();
        created.Headers.Location!.ToString().ShouldBe($"{NotesPath}/{id}");
        var note = await _client.GetFromJsonAsync<JsonElement>($"{NotesPath}/{id}", TestContext.Current.CancellationToken);
        note.GetProperty("id").GetGuid().ShouldBe(id);
        note.GetProperty("text").GetString().ShouldBe(text);
    }

    [Fact]
    public async Task CreateNote_EmptyText_Returns400WithFieldErrors()
    {
        var response = await _client.PostAsJsonAsync(NotesPath, new { text = "" }, TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        problem.GetProperty("errors").GetProperty("text").GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task CreateNote_DuplicateText_Returns409()
    {
        var text = UniqueText();
        await _client.PostAsJsonAsync(NotesPath, new { text }, TestContext.Current.CancellationToken);

        var response = await _client.PostAsJsonAsync(NotesPath, new { text }, TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.Conflict);
        problem.GetProperty("code").GetString().ShouldBe("probe.duplicate_text");
    }

    [Fact]
    public async Task GetNote_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"{NotesPath}/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.NotFound);
        problem.GetProperty("code").GetString().ShouldBe("probe.note_not_found");
    }

    [Fact]
    public async Task CreateNote_MalformedJson_Returns400Problem()
    {
        using var body = new StringContent("{", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync(NotesPath, body, TestContext.Current.CancellationToken);

        await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
    }

    private static string UniqueText() => $"note-{Guid.NewGuid():N}";
}
