using LegoChallenge.Models;

namespace LegoChallenge.Services;

public static class ColourSubstitutionService
{
    /// <summary>
    /// Returns true if the set can be built using colour substitutions.
    ///
    /// Rules:
    ///   - All pieces of a substituted colour must be replaced (no partial swaps).
    ///   - The replacement colour must not already be used elsewhere in the set.
    ///
    /// This reduces to finding an injective mapping (set colour -> inventory colour)
    /// where for each pair (setColour -> invColour), the user has enough of every
    /// (pieceId, invColour) required by that colour group. Solved via bipartite matching.
    /// </summary>
    public static bool CanBuildWithColourSubstitution(
        SetDetail set,
        Dictionary<(string PieceId, string ColorId), int> inventory)
    {
        // Group set requirements by colour: setColour -> [(pieceId, requiredQty)]
        var byColour = set.Pieces
            .GroupBy(p => p.Part.Material.ToString())
            .ToDictionary(
                g => g.Key,
                g => g.Select(p => (p.Part.DesignId, p.Quantity)).ToList()
            );

        var setColours = byColour.Keys.ToList();
        var invColours = inventory.Keys.Select(k => k.ColorId).Distinct().ToList();

        // For each set colour, which inventory colours can substitute it?
        var options = setColours.ToDictionary(
            sc => sc,
            sc => invColours
                .Where(ic => byColour[sc].All(p =>
                    inventory.TryGetValue((p.DesignId, ic), out var count) && count >= p.Quantity))
                .ToList()
        );

        // Bipartite matching: find injective assignment from setColours -> invColours
        // matchR[invColour] = setColour currently assigned to it
        var matchR = new Dictionary<string, string>();

        bool TryAugment(string sc, HashSet<string> visited)
        {
            foreach (var ic in options[sc])
            {
                if (!visited.Add(ic)) continue;

                // ic is free, or we can re-route whoever currently holds it
                if (!matchR.TryGetValue(ic, out var holder) || TryAugment(holder, visited))
                {
                    matchR[ic] = sc;
                    return true;
                }
            }
            return false;
        }

        foreach (var sc in setColours)
        {
            TryAugment(sc, new HashSet<string>());
        }

        // Full matching found only if every set colour got assigned
        return matchR.Values.Distinct().Count() == setColours.Count;
    }
}
