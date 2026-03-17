using FluentAssertions;
using LegoChallenge.Models;
using LegoChallenge.Services;

namespace LegoChallenge.Tests;

public class ColourSubstitutionServiceTests
{
    [Fact]
    public void CanBuild_AlreadyBuildableSet_ReturnsTrueViaIdentityMapping()
    {
        // Set needs: piece-A colour 1 (qty 2)
        // Inventory has exactly that — identity mapping must be valid
        var set = MakeSet([("pieceA", 1, 2)]);
        var inv = Inv([("pieceA", "1", 2)]);

        CanBuild(set, inv).Should().BeTrue();
    }

    [Fact]
    public void CanBuild_SingleColourSwap_ReturnsTrue()
    {
        // Set needs: piece-A in colour 1
        // Inventory only has piece-A in colour 2 → swap 1→2
        var set = MakeSet([("pieceA", 1, 3)]);
        var inv = Inv([("pieceA", "2", 5)]);

        CanBuild(set, inv).Should().BeTrue();
    }

    [Fact]
    public void CanBuild_SingleColourSwap_InsufficientQty_ReturnsFalse()
    {
        var set = MakeSet([("pieceA", 1, 5)]);
        var inv = Inv([("pieceA", "2", 4)]);

        CanBuild(set, inv).Should().BeFalse();
    }

    [Fact]
    public void CanBuild_PieceMissingInAllColours_ReturnsFalse()
    {
        var set = MakeSet([("pieceA", 1, 1)]);
        var inv = Inv([("pieceB", "1", 99)]);

        CanBuild(set, inv).Should().BeFalse();
    }

    [Fact]
    public void CanBuild_TwoColourSwapsNoConflict_ReturnsTrue()
    {
        // Set: piece-A in colour 1, piece-B in colour 2
        // Inventory: piece-A in colour 3, piece-B in colour 4
        // Mapping: 1→3, 2→4
        var set = MakeSet([("pieceA", 1, 2), ("pieceB", 2, 2)]);
        var inv = Inv([("pieceA", "3", 5), ("pieceB", "4", 5)]);

        CanBuild(set, inv).Should().BeTrue();
    }

    [Fact]
    public void CanBuild_RequiresAugmentingPath_NotJustGreedy()
    {
        // This is the critical backtracking test:
        //
        // Set colours: Red (colour 1), Blue (colour 2)
        //   Red  group: piece-X qty 1
        //   Blue group: piece-Y qty 1
        //
        // Inventory colours:
        //   Green  (colour 3): has piece-X AND piece-Y  → compatible with both Red and Blue
        //   Yellow (colour 4): has piece-Y only          → compatible with Blue only
        //
        // Greedy (processes Blue first): Blue→Green, Red→? = FAIL
        // Augmenting path:               Blue→Yellow, Red→Green = PASS

        var set = MakeSet([("pieceX", 1, 1), ("pieceY", 2, 1)]);
        var inv = Inv([
            ("pieceX", "3", 1),  // Green has piece-X
            ("pieceY", "3", 1),  // Green has piece-Y
            ("pieceY", "4", 1)   // Yellow has piece-Y only
        ]);

        CanBuild(set, inv).Should().BeTrue();
    }

    [Fact]
    public void CanBuild_TwoSetColoursOnlyOneInventoryColourAvailable_ReturnsFalse()
    {
        // Set has two colour groups, but only one inventory colour can satisfy either.
        // Injective mapping is impossible.
        var set = MakeSet([("pieceA", 1, 1), ("pieceB", 2, 1)]);
        var inv = Inv([
            ("pieceA", "3", 5),
            ("pieceB", "3", 5)
            // Both groups map to colour 3, but that would violate injectivity
        ]);

        CanBuild(set, inv).Should().BeFalse();
    }

    [Fact]
    public void CanBuild_ColourGroupPartiallyMatchable_ReturnsFalse()
    {
        // Set colour group has two different pieces; inventory colour only covers one of them
        // piece-A in colour 1: needs piece-X and piece-Y
        // Inventory colour 2: has piece-X but NOT piece-Y
        var set = MakeSet([("pieceX", 1, 1), ("pieceY", 1, 1)]); // both same colour
        var inv = Inv([("pieceX", "2", 5)]); // only piece-X in colour 2

        CanBuild(set, inv).Should().BeFalse();
    }

    [Fact]
    public void CanBuild_EmptySet_ReturnsTrue()
    {
        var set = MakeSet([]);
        var inv = new Dictionary<(string, string), int>();

        CanBuild(set, inv).Should().BeTrue();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool CanBuild(SetDetail set, Dictionary<(string, string), int> inv) =>
        ColourSubstitutionService.CanBuildWithColourSubstitution(set, inv);

    private static SetDetail MakeSet(IEnumerable<(string PieceId, int Material, int Qty)> pieces)
    {
        var setPieces = pieces
            .Select(p => new SetPiece(new Part(p.PieceId, p.Material, "rigid"), p.Qty))
            .ToList();
        return new SetDetail("id", "test-set", "000XX", setPieces);
    }

    private static Dictionary<(string, string), int> Inv(
        IEnumerable<(string PieceId, string Color, int Count)> entries) =>
        entries.ToDictionary(e => (e.PieceId, e.Color), e => e.Count);
}
