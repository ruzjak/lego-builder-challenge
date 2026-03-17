using LegoChallenge.Models;
using LegoChallenge.Services;

var baseUrl = "https://d30r5p5favh3z8.cloudfront.net";

using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };
var api = new LegoApiClient(http);

// ── Main Task ─────────────────────────────────────────────────────────────────
Console.WriteLine("=== Which sets can brickfan35 build? ===\n");

var userSummary = await api.GetUserByUsernameAsync("brickfan35");
if (userSummary is null) { Console.WriteLine("User not found."); return; }

var user = await api.GetUserByIdAsync(userSummary.Id);
if (user is null) { Console.WriteLine("User details not found."); return; }

var inventory = InventoryService.BuildInventoryLookup(user);

var sets = await api.GetSetsAsync();
var setDetails = await Task.WhenAll(sets.Select(s => api.GetSetByIdAsync(s.Id)));

var buildable = setDetails
    .Where(s => s is not null && InventoryService.CanBuildSet(s, inventory))
    .Select(s => s!)
    .ToList();

if (buildable.Count == 0)
    Console.WriteLine("brickfan35 cannot build any sets with their current inventory.");
else
    foreach (var s in buildable)
        Console.WriteLine($"  + {s.Name} ({s.SetNumber})");

// ── Stretch Goal 1 ────────────────────────────────────────────────────────────
Console.WriteLine("\n=== Stretch 1: Who can collaborate with landscape-artist to build tropical-island? ===\n");

var laSummary = await api.GetUserByUsernameAsync("landscape-artist");
var targetSet = setDetails.FirstOrDefault(s => s?.Name == "tropical-island");

if (laSummary is null || targetSet is null)
{
    Console.WriteLine("Required data not found.");
}
else
{
    var laUser = await api.GetUserByIdAsync(laSummary.Id);
    var laInventory = InventoryService.BuildInventoryLookup(laUser!);

    var allUsers = await api.GetUsersAsync();
    var collaborators = new List<string>();

    await Parallel.ForEachAsync(allUsers.Where(u => u.Id != laSummary.Id), async (u, _) =>
    {
        var other = await api.GetUserByIdAsync(u.Id);
        if (other is null) return;

        var combined = InventoryService.MergeInventories(laInventory, InventoryService.BuildInventoryLookup(other));
        if (InventoryService.CanBuildSet(targetSet, combined))
            lock (collaborators) { collaborators.Add(u.Username); }
    });

    if (collaborators.Count == 0)
        Console.WriteLine("No single collaborator can fill the gap.");
    else
        foreach (var name in collaborators.Order())
            Console.WriteLine($"  + {name}");
}

// ── Stretch Goal 2 ────────────────────────────────────────────────────────────
Console.WriteLine("\n=== Stretch 2: Largest piece set megabuilder99 can use so >=50% of users can complete it? ===\n");

var mbSummary = await api.GetUserByUsernameAsync("megabuilder99");
if (mbSummary is null)
{
    Console.WriteLine("User megabuilder99 not found.");
}
else
{
    var mbUser = await api.GetUserByIdAsync(mbSummary.Id);
    var mbInventory = InventoryService.BuildInventoryLookup(mbUser!);

    var allUsers = await api.GetUsersAsync();
    var otherInventories = new List<Dictionary<(string, string), int>>();

    await Parallel.ForEachAsync(allUsers.Where(u => u.Id != mbSummary.Id), async (u, _) =>
    {
        var detail = await api.GetUserByIdAsync(u.Id);
        if (detail is null) return;
        lock (otherInventories) { otherInventories.Add(InventoryService.BuildInventoryLookup(detail)); }
    });

    int threshold = (int)Math.Ceiling(otherInventories.Count * 0.5);

    // For each (pieceId, color) in megabuilder99's inventory, find the largest
    // quantity Q such that at least 50% of other users also have >= Q of that piece.
    var result = new Dictionary<(string, string), int>();

    foreach (var (key, mbCount) in mbInventory)
    {
        int lo = 1, hi = mbCount, best = 0;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            int usersWithEnough = otherInventories.Count(inv =>
                inv.TryGetValue(key, out var c) && c >= mid);

            if (usersWithEnough >= threshold) { best = mid; lo = mid + 1; }
            else hi = mid - 1;
        }

        if (best > 0)
            result[key] = best;
    }

    int totalPieces = result.Values.Sum();
    Console.WriteLine($"  Piece types available: {result.Count}");
    Console.WriteLine($"  Total piece count:     {totalPieces}");
    Console.WriteLine($"  (Based on {otherInventories.Count} other users, threshold = {threshold})");
}

// ── Stretch Goal 3 ────────────────────────────────────────────────────────────
Console.WriteLine("\n=== Stretch 3: New sets dr_crocodile can build with colour substitutions? ===\n");

var dcSummary = await api.GetUserByUsernameAsync("dr_crocodile");
if (dcSummary is null)
{
    Console.WriteLine("User dr_crocodile not found.");
}
else
{
    var dcUser = await api.GetUserByIdAsync(dcSummary.Id);
    var dcInventory = InventoryService.BuildInventoryLookup(dcUser!);

    // Separate sets already buildable from sets requiring substitution
    var alreadyBuildable = setDetails
        .Where(s => s is not null && InventoryService.CanBuildSet(s, dcInventory))
        .Select(s => s!.Name)
        .ToHashSet();

    var newlySolvable = setDetails
        .Where(s => s is not null && !alreadyBuildable.Contains(s.Name))
        .Where(s => ColourSubstitutionService.CanBuildWithColourSubstitution(s!, dcInventory))
        .Select(s => s!)
        .ToList();

    Console.WriteLine($"  Sets already buildable by dr_crocodile: {alreadyBuildable.Count}");
    Console.WriteLine($"  New sets unlocked via colour substitution:\n");

    if (newlySolvable.Count == 0)
        Console.WriteLine("  (none)");
    else
        foreach (var s in newlySolvable)
            Console.WriteLine($"  + {s.Name} ({s.SetNumber})");
}
