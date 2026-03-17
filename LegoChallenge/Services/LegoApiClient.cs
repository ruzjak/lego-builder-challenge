using System.Net.Http.Json;
using LegoChallenge.Models;

namespace LegoChallenge.Services;

public class LegoApiClient(HttpClient http)
{
    public async Task<List<UserSummary>> GetUsersAsync()
    {
        var response = await http.GetFromJsonAsync<UsersResponse>("/api/users");
        return response?.Users ?? [];
    }

    public async Task<UserSummary?> GetUserByUsernameAsync(string username)
    {
        return await http.GetFromJsonAsync<UserSummary>($"/api/user/by-username/{username}");
    }

    public async Task<UserDetail?> GetUserByIdAsync(string id)
    {
        return await http.GetFromJsonAsync<UserDetail>($"/api/user/by-id/{id}");
    }

    public async Task<List<SetSummary>> GetSetsAsync()
    {
        var response = await http.GetFromJsonAsync<SetsResponse>("/api/sets");
        return response?.Sets ?? [];
    }

    public async Task<SetDetail?> GetSetByIdAsync(string id)
    {
        return await http.GetFromJsonAsync<SetDetail>($"/api/set/by-id/{id}");
    }
}
