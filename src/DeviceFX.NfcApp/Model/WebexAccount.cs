using DeviceFX.NfcApp.Helpers;
using DeviceFX.NfcApp.Model.Dto;

namespace DeviceFX.NfcApp.Model;

public class WebexAccount
{
    public string? Id { get; set; }
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? FamilyName { get; set; }
    public string? GivenName { get; set; }
    public string? Picture { get; set; }
    public string? CurrentOrgId { get; set; }
    public List<WebexOrganization> Organizations { get; set; } = [];
    public List<WebexLicense> Licenses { get; set; } = [];
    public List<WebexLicense> CallingLicenses => Licenses.Where(l => l.Name != null && l.Name.Contains("Webex Calling")).ToList();

    public WebexAccount(WebexIdentityUserDto userDto, WebexOrganizationsDto orgDto, WebexLicensesDto licensesDto)
    {
        Id = userDto.id;
        Username = userDto.userName;
        DisplayName = userDto.displayName;
        Email = userDto.emails?.FirstOrDefault(e => e.primary)?.value ?? userDto.emails?.FirstOrDefault()?.value;
        FamilyName = userDto.name?.familyName;
        GivenName = userDto.name?.givenName;
        Picture = userDto.Picture;
        CurrentOrgId = WebexIDTypes.Organization.ConvertToBase64Id(userDto.webex.organization.organizationId);
        Organizations = orgDto.organizations.Select(o => new WebexOrganization
        {
            Id = WebexIDTypes.Organization.ConvertToBase64Id(o.id),
            Name = o.displayName
        }).ToList();
        Update(WebexIDTypes.Organization.ConvertToBase64Id(userDto.webex.organization.organizationId), licensesDto);
    }

    public void Update(string orgId, WebexLicensesDto licensesDto)
    {
        if(Organizations.All(o => o.Id != orgId)) return;
        CurrentOrgId = orgId;
        Licenses = licensesDto.items.Select(l => new WebexLicense
        {
            Id = l.id,
            Name = l.name,
            SubscriptionId = l.subscriptionId
        }).ToList();
    }
    public bool HasLicense(WebexPersonDto person)
    {
        if (person == null || person.licenses == null || !person.licenses.Any()) return false;
        var licenseIds = Licenses.Select(l => l.Id).ToHashSet();
        return person.licenses.Any(l => licenseIds.Contains(l));
    }
}

public class WebexOrganization
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}

public class WebexLicense
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? SubscriptionId { get; set; }
}