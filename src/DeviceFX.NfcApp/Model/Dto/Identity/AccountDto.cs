using System.Text.Json.Serialization;

namespace DeviceFX.NfcApp.Model.Dto.Identity;

public record AccountDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("email")]
    public string Email { get; init; } = "";

    [JsonPropertyName("nickName")]
    public string? NickName { get; init; }

    [JsonPropertyName("firstName")]
    public string FirstName { get; init; } = "";

    [JsonPropertyName("fullName")]
    public string FullName { get; init; } = "";

    [JsonPropertyName("lastName")]
    public string LastName { get; init; } = "";

    [JsonPropertyName("picture")]
    public string? Picture { get; init; }

    [JsonPropertyName("connections")]
    public AccountConnectionDto[] Connections { get; init; } = [];

    [JsonPropertyName("defaultOrganization")]
    public Guid? DefaultOrganization { get; init; }

    [JsonPropertyName("organizations")]
    public Guid[] Organizations { get; init; } = [];
}

public record AccountConnectionDto
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";
}
