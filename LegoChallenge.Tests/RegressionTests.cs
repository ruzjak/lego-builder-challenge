using FluentAssertions;
using LegoChallenge.Services;
using RichardSzalay.MockHttp;

namespace LegoChallenge.Tests;

/// <summary>
/// Golden-value regression tests.
/// Fixtures were captured from the live API and lock in the expected outputs.
/// These catch regressions when refactoring logic, without hitting the network.
/// </summary>
public class RegressionTests
{
    private const string BaseUrl = "https://lego.test";

    [Fact]
    public async Task Brickfan35_CanBuildExactlyThreeSets()
    {
        var api = await BuildApiClient();

        var userSummary = await api.GetUserByUsernameAsync("brickfan35");
        var user = await api.GetUserByIdAsync(userSummary!.Id);
        var inventory = InventoryService.BuildInventoryLookup(user!);

        var sets = await api.GetSetsAsync();
        var setDetails = await Task.WhenAll(sets.Select(s => api.GetSetByIdAsync(s.Id)));

        var buildable = setDetails
            .Where(s => s is not null && InventoryService.CanBuildSet(s, inventory))
            .Select(s => s!.Name)
            .ToList();

        buildable.Should().BeEquivalentTo(["car-wash", "castaway", "undersea-monster"]);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a LegoApiClient backed by all fixture files so no network is required.
    /// </summary>
    private static async Task<LegoApiClient> BuildApiClient()
    {
        var mockHttp = new MockHttpMessageHandler();

        // Users list
        await RegisterFixture(mockHttp, "/api/users", "Fixtures/users.json");

        // User summaries by username (parse from users fixture)
        var usersJson = await File.ReadAllTextAsync("Fixtures/users.json");
        await RegisterFixture(mockHttp, "/api/user/by-username/brickfan35", "Fixtures/brickfan35_summary.json");

        // User detail
        await RegisterFixture(mockHttp, "/api/user/by-id/6d6bc9f2-a762-4a30-8d9a-52cf8d8373fc", "Fixtures/brickfan35_detail.json");

        // Sets list + individual set details (all pre-downloaded as fixtures)
        await RegisterFixture(mockHttp, "/api/sets", "Fixtures/sets.json");
        foreach (var file in Directory.GetFiles("Fixtures", "set_*.json"))
        {
            var id = Path.GetFileNameWithoutExtension(file).Replace("set_", "");
            mockHttp.When($"{BaseUrl}/api/set/by-id/{id}")
                    .Respond("application/json", await File.ReadAllTextAsync(file));
        }

        var http = mockHttp.ToHttpClient();
        http.BaseAddress = new Uri(BaseUrl);
        return new LegoApiClient(http);
    }

    private static async Task RegisterFixture(MockHttpMessageHandler mock, string path, string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        mock.When($"{BaseUrl}{path}").Respond("application/json", json);
    }
}
