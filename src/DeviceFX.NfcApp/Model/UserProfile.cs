using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using DeviceFX.NfcApp.Helpers;
using DeviceFX.NfcApp.Model.Dto;
using DeviceFX.NfcApp.Model.Dto.Identity;

namespace DeviceFX.NfcApp.Model;

public partial class UserProfile : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Initials))]
    private string? displayName;

    [ObservableProperty]
    private string? picture;

    [ObservableProperty]
    private string? email;

    [ObservableProperty]
    private OrganizationProfile? organization;

    [ObservableProperty]
    private bool isLoggedIn;

    [ObservableProperty]
    private bool mustLogin;

    [ObservableProperty]
    private ObservableCollection<OrganizationProfile> organizations = [];

    public string Initials => ToInitials(DisplayName);

    public string[] Roles { get; private set; } = [];

    public IReadOnlyDictionary<string, JsonElement>? Features { get; private set; }

    public void Set(WebexIdentityUserDto userDto, WebexOrganizationsDto orgsDto, string? orgId = null)
    {
        DisplayName = userDto.displayName;
        Picture = userDto.Picture;
        Email = userDto.emails?.FirstOrDefault(e => e.primary)?.value ?? userDto.emails?.FirstOrDefault()?.value;
        IsLoggedIn = true;
        MustLogin = false;
        Roles = [];
        Features = null;
        Organizations = new ObservableCollection<OrganizationProfile>(orgsDto.organizations.Select(o => new OrganizationProfile
        {
            Id = WebexIDTypes.Organization.ConvertToBase64Id(o.id),
            Name = o.displayName
        }));
        if (orgId != null)
        {
            Organization = Organizations.FirstOrDefault(o => o.Id == orgId);
            if(Organization != null) return;
        }
        var currentOrgId = WebexIDTypes.Organization.ConvertToBase64Id(userDto.webex.organization.organizationId);
        Organization = Organizations.FirstOrDefault(o => o.Id == currentOrgId);
    }

    public void Set(
        AccountDto account,
        IReadOnlyList<OrganizationDto> organizationDtos,
        string? orgId = null,
        string[]? roles = null,
        IReadOnlyDictionary<string, JsonElement>? features = null)
    {
        DisplayName = DisplayNameFrom(account);
        Picture = account.Picture;
        Email = string.IsNullOrWhiteSpace(account.Email) ? null : account.Email;
        IsLoggedIn = true;
        MustLogin = false;
        Roles = roles ?? [];
        Features = features;
        Organizations = new ObservableCollection<OrganizationProfile>(organizationDtos.Select(o => new OrganizationProfile
        {
            Id = o.Id.ToString(),
            Name = o.Name
        }));

        if (!string.IsNullOrWhiteSpace(orgId))
        {
            Organization = Organizations.FirstOrDefault(o => IdsEqual(o.Id, orgId));
            if (Organization != null) return;
        }

        if (account.DefaultOrganization is { } defaultOrg)
        {
            Organization = Organizations.FirstOrDefault(o => IdsEqual(o.Id, defaultOrg.ToString()));
            if (Organization != null) return;
        }

        Organization = Organizations.FirstOrDefault();
    }

    public void Reset()
    {
        DisplayName = null;
        Picture = null;
        Email = null;
        Organizations = [];
        IsLoggedIn = false;
        MustLogin = true;
        Organization = null;
        Roles = [];
        Features = null;
    }

    private static string? DisplayNameFrom(AccountDto account)
    {
        if (!string.IsNullOrWhiteSpace(account.FullName)) return account.FullName;
        var fullName = $"{account.FirstName} {account.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName)) return fullName;
        if (!string.IsNullOrWhiteSpace(account.NickName)) return account.NickName;
        return string.IsNullOrWhiteSpace(account.Email) ? null : account.Email;
    }

    private static bool IdsEqual(string? left, string? right) =>
        Guid.TryParse(left, out var a) && Guid.TryParse(right, out var b)
            ? a == b
            : string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    internal static string ToInitials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return string.Empty;

        static char? Letter(string value) => value.FirstOrDefault(char.IsLetter) is var c && c != '\0' ? c : null;

        var first = Letter(parts[0]);
        if (first is null) return string.Empty;
        if (parts.Length == 1)
        {
            var second = parts[0].Skip(1).FirstOrDefault(char.IsLetter);
            return second == default
                ? char.ToUpperInvariant(first.Value).ToString()
                : $"{char.ToUpperInvariant(first.Value)}{char.ToUpperInvariant(second)}";
        }

        var last = Letter(parts[^1]);
        return last is null
            ? char.ToUpperInvariant(first.Value).ToString()
            : $"{char.ToUpperInvariant(first.Value)}{char.ToUpperInvariant(last.Value)}";
    }
}

public partial class OrganizationProfile : ObservableObject
{
    [ObservableProperty]
    private string? id;
    [ObservableProperty]
    private string? name;

    public List<string>? LicenseIds { get; set; }

    public override string ToString() => Name;
}
