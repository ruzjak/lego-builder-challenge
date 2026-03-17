using System.Text.Json.Serialization;

namespace LegoChallenge.Models;

public record UserSummary(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("location")] string Location,
    [property: JsonPropertyName("brickCount")] int BrickCount
);

public record UsersResponse(
    [property: JsonPropertyName("Users")] List<UserSummary> Users
);

public record PieceVariant(
    [property: JsonPropertyName("color")] string Color,
    [property: JsonPropertyName("count")] int Count
);

public record CollectionEntry(
    [property: JsonPropertyName("pieceId")] string PieceId,
    [property: JsonPropertyName("variants")] List<PieceVariant> Variants
);

public record UserDetail(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("location")] string Location,
    [property: JsonPropertyName("brickCount")] int BrickCount,
    [property: JsonPropertyName("collection")] List<CollectionEntry> Collection
);

public record SetSummary(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("setNumber")] string SetNumber,
    [property: JsonPropertyName("totalPieces")] int TotalPieces
);

public record SetsResponse(
    [property: JsonPropertyName("Sets")] List<SetSummary> Sets
);

public record Part(
    [property: JsonPropertyName("designID")] string DesignId,
    [property: JsonPropertyName("material")] int Material,
    [property: JsonPropertyName("partType")] string PartType
);

public record SetPiece(
    [property: JsonPropertyName("part")] Part Part,
    [property: JsonPropertyName("quantity")] int Quantity
);

public record SetDetail(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("setNumber")] string SetNumber,
    [property: JsonPropertyName("pieces")] List<SetPiece> Pieces
);
