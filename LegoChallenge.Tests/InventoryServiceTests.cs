using FluentAssertions;
using LegoChallenge.Models;
using LegoChallenge.Services;

namespace LegoChallenge.Tests;

public class InventoryServiceTests
{
    // ── BuildInventoryLookup ──────────────────────────────────────────────────

    [Fact]
    public void BuildInventoryLookup_SinglePieceSingleColour_ReturnsCorrectCount()
    {
        var user = MakeUser([("pieceA", [("1", 5)])]);

        var result = InventoryService.BuildInventoryLookup(user);

        result.Should().ContainKey(("pieceA", "1")).WhoseValue.Should().Be(5);
    }

    [Fact]
    public void BuildInventoryLookup_MultiplePiecesMultipleColours_AllPresent()
    {
        var user = MakeUser([
            ("pieceA", [("1", 3), ("2", 7)]),
            ("pieceB", [("1", 4)])
        ]);

        var result = InventoryService.BuildInventoryLookup(user);

        result.Should().HaveCount(3);
        result[("pieceA", "1")].Should().Be(3);
        result[("pieceA", "2")].Should().Be(7);
        result[("pieceB", "1")].Should().Be(4);
    }

    // ── CanBuildSet ───────────────────────────────────────────────────────────

    [Fact]
    public void CanBuildSet_EmptySet_ReturnsTrue()
    {
        var set = MakeSet([]);
        var inventory = new Dictionary<(string, string), int>();

        InventoryService.CanBuildSet(set, inventory).Should().BeTrue();
    }

    [Fact]
    public void CanBuildSet_ExactQuantityMatch_ReturnsTrue()
    {
        var set = MakeSet([("pieceA", 1, 5)]);
        var inventory = Inv([("pieceA", "1", 5)]);

        InventoryService.CanBuildSet(set, inventory).Should().BeTrue();
    }

    [Fact]
    public void CanBuildSet_MoreThanEnough_ReturnsTrue()
    {
        var set = MakeSet([("pieceA", 1, 3)]);
        var inventory = Inv([("pieceA", "1", 10)]);

        InventoryService.CanBuildSet(set, inventory).Should().BeTrue();
    }

    [Fact]
    public void CanBuildSet_OneShortOfRequired_ReturnsFalse()
    {
        var set = MakeSet([("pieceA", 1, 5)]);
        var inventory = Inv([("pieceA", "1", 4)]);

        InventoryService.CanBuildSet(set, inventory).Should().BeFalse();
    }

    [Fact]
    public void CanBuildSet_PieceMissingEntirely_ReturnsFalse()
    {
        var set = MakeSet([("pieceA", 1, 1)]);
        var inventory = Inv([("pieceB", "1", 99)]);

        InventoryService.CanBuildSet(set, inventory).Should().BeFalse();
    }

    [Fact]
    public void CanBuildSet_CorrectPieceWrongColour_ReturnsFalse()
    {
        var set = MakeSet([("pieceA", 1, 1)]);   // needs colour "1"
        var inventory = Inv([("pieceA", "2", 99)]); // only has colour "2"

        InventoryService.CanBuildSet(set, inventory).Should().BeFalse();
    }

    [Fact]
    public void CanBuildSet_SamePieceMultipleColoursAllPresent_ReturnsTrue()
    {
        var set = MakeSet([("pieceA", 1, 2), ("pieceA", 2, 3)]);
        var inventory = Inv([("pieceA", "1", 2), ("pieceA", "2", 3)]);

        InventoryService.CanBuildSet(set, inventory).Should().BeTrue();
    }

    [Fact]
    public void CanBuildSet_SamePieceMultipleColoursOneInsufficient_ReturnsFalse()
    {
        var set = MakeSet([("pieceA", 1, 2), ("pieceA", 2, 3)]);
        var inventory = Inv([("pieceA", "1", 2), ("pieceA", "2", 2)]); // colour 2: need 3, have 2

        InventoryService.CanBuildSet(set, inventory).Should().BeFalse();
    }

    // ── MergeInventories ─────────────────────────────────────────────────────

    [Fact]
    public void MergeInventories_NoOverlap_ContainsAllKeys()
    {
        var a = Inv([("p1", "1", 3)]);
        var b = Inv([("p2", "2", 5)]);

        var result = InventoryService.MergeInventories(a, b);

        result.Should().HaveCount(2);
        result[("p1", "1")].Should().Be(3);
        result[("p2", "2")].Should().Be(5);
    }

    [Fact]
    public void MergeInventories_FullOverlap_SumsQuantities()
    {
        var a = Inv([("p1", "1", 3)]);
        var b = Inv([("p1", "1", 4)]);

        var result = InventoryService.MergeInventories(a, b);

        result.Should().HaveCount(1);
        result[("p1", "1")].Should().Be(7);
    }

    [Fact]
    public void MergeInventories_PartialOverlap_MergesCorrectly()
    {
        var a = Inv([("p1", "1", 3), ("p2", "1", 5)]);
        var b = Inv([("p1", "1", 2), ("p3", "1", 1)]);

        var result = InventoryService.MergeInventories(a, b);

        result[("p1", "1")].Should().Be(5);
        result[("p2", "1")].Should().Be(5);
        result[("p3", "1")].Should().Be(1);
    }

    [Fact]
    public void MergeInventories_DoesNotMutateOriginals()
    {
        var a = Inv([("p1", "1", 3)]);
        var b = Inv([("p1", "1", 4)]);

        InventoryService.MergeInventories(a, b);

        a[("p1", "1")].Should().Be(3);
        b[("p1", "1")].Should().Be(4);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static UserDetail MakeUser(
        IEnumerable<(string PieceId, IEnumerable<(string Color, int Count)> Variants)> collection)
    {
        var entries = collection
            .Select(e => new CollectionEntry(
                e.PieceId,
                e.Variants.Select(v => new PieceVariant(v.Color, v.Count)).ToList()))
            .ToList();

        return new UserDetail("id", "test-user", "LOC", 0, entries);
    }

    // set pieces: (pieceId, colourInt, quantity)
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
