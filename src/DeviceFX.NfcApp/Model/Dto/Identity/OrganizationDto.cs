using System.Text.Json.Serialization;

namespace DeviceFX.NfcApp.Model.Dto.Identity;

public record OrganizationDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("stamp")]
    public string Stamp { get; init; } = "";

    [JsonPropertyName("updated")]
    public DateTime Updated { get; init; }

    [JsonPropertyName("externalService")]
    public IEnumerable<ExternalServiceDto> ExternalService { get; init; } = [];

    [JsonPropertyName("assignableRoles")]
    public IReadOnlyList<AssignableRoleDto> AssignableRoles { get; init; } = [];
}

public record ExternalServiceDto
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("type")]
    public string Type { get; init; } = "";
}

public record AssignableRoleDto
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }
}
