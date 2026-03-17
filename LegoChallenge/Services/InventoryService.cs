using LegoChallenge.Models;

namespace LegoChallenge.Services;

public class InventoryService
{
    /// <summary>
    /// Converts a user's collection into a flat lookup: (pieceId, colorId) -> total count
    /// </summary>
    public static Dictionary<(string PieceId, string ColorId), int> BuildInventoryLookup(UserDetail user)
    {
        var lookup = new Dictionary<(string, string), int>();

        foreach (var entry in user.Collection)
        foreach (var variant in entry.Variants)
        {
            var key = (entry.PieceId, variant.Color);
            lookup[key] = lookup.GetValueOrDefault(key) + variant.Count;
        }

        return lookup;
    }

    /// <summary>
    /// Returns true if the inventory satisfies all piece requirements of the set.
    /// </summary>
    public static bool CanBuildSet(
        SetDetail set,
        Dictionary<(string PieceId, string ColorId), int> inventory)
    {
        foreach (var piece in set.Pieces)
        {
            var key = (piece.Part.DesignId, piece.Part.Material.ToString());
            if (!inventory.TryGetValue(key, out var available) || available < piece.Quantity)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Merges two inventories into a new combined lookup.
    /// </summary>
    public static Dictionary<(string PieceId, string ColorId), int> MergeInventories(
        Dictionary<(string PieceId, string ColorId), int> a,
        Dictionary<(string PieceId, string ColorId), int> b)
    {
        var merged = new Dictionary<(string, string), int>(a);
        foreach (var (key, count) in b)
            merged[key] = merged.GetValueOrDefault(key) + count;
        return merged;
    }
}
