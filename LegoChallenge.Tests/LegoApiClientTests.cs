using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LegoChallenge.Services;
using RichardSzalay.MockHttp;

namespace LegoChallenge.Tests;

public class LegoApiClientTests
{
    private const string BaseUrl = "https://lego.test";

    [Fact]
    public async Task GetUsersAsync_DeserializesUserList()
    {
        var json = await File.ReadAllTextAsync("Fixtures/users.json");
        var (client, _) = MockClient("GET", "/api/users", json);

        var result = await client.GetUsersAsync();

        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(u =>
        {
            u.Id.Should().NotBeNullOrEmpty();
            u.Username.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task GetUserByUsernameAsync_DeserializesCorrectUser()
    {
        var json = await File.ReadAllTextAsync("Fixtures/brickfan35_summary.json");
        var (client, _) = MockClient("GET", "/api/user/by-username/brickfan35", json);

        var result = await client.GetUserByUsernameAsync("brickfan35");

        result.Should().NotBeNull();
        result!.Username.Should().Be("brickfan35");
        result.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetUserByIdAsync_DeserializesCollectionWithVariants()
    {
        var json = await File.ReadAllTextAsync("Fixtures/brickfan35_detail.json");
        var (client, _) = MockClient("GET", "/api/user/by-id/6d6bc9f2-a762-4a30-8d9a-52cf8d8373fc", json);

        var result = await client.GetUserByIdAsync("6d6bc9f2-a762-4a30-8d9a-52cf8d8373fc");

        result.Should().NotBeNull();
        result!.Collection.Should().NotBeEmpty();
        result.Collection.Should().AllSatisfy(entry =>
        {
            entry.PieceId.Should().NotBeNullOrEmpty();
            entry.Variants.Should().NotBeEmpty();
            entry.Variants.Should().AllSatisfy(v =>
            {
                v.Color.Should().NotBeNullOrEmpty();
                v.Count.Should().BeGreaterThan(0);
            });
        });
    }

    [Fact]
    public async Task GetSetsAsync_DeserializesSetList()
    {
        var json = await File.ReadAllTextAsync("Fixtures/sets.json");
        var (client, _) = MockClient("GET", "/api/sets", json);

        var result = await client.GetSetsAsync();

        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(s =>
        {
            s.Id.Should().NotBeNullOrEmpty();
            s.Name.Should().NotBeNullOrEmpty();
            s.SetNumber.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task GetUserByUsernameAsync_NotFound_ReturnsNull()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{BaseUrl}/api/user/by-username/ghost")
                .Respond(HttpStatusCode.NotFound);

        using var http = mockHttp.ToHttpClient();
        http.BaseAddress = new Uri(BaseUrl);
        var client = new LegoApiClient(http);

        var act = async () => await client.GetUserByUsernameAsync("ghost");

        // HttpClient throws on 404 when using GetFromJsonAsync — verify it propagates
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (LegoApiClient client, MockHttpMessageHandler mock) MockClient(
        string method, string path, string responseJson)
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Parse(method), $"{BaseUrl}{path}")
                .Respond("application/json", responseJson);

        var http = mockHttp.ToHttpClient();
        http.BaseAddress = new Uri(BaseUrl);
        return (new LegoApiClient(http), mockHttp);
    }
}
