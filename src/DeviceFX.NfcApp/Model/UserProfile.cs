using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DeviceFX.NfcApp.Helpers;
using DeviceFX.NfcApp.Model.Dto;

namespace DeviceFX.NfcApp.Model;

public partial class UserProfile : ObservableObject
{

    [ObservableProperty]
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

    public void Set(WebexIdentityUserDto userDto, WebexOrganizationsDto orgsDto, string? orgId = null)
    {
        DisplayName = userDto.displayName;
        Picture = userDto.Picture;
        Email = userDto.emails?.FirstOrDefault(e => e.primary)?.value ?? userDto.emails?.FirstOrDefault()?.value; 
        IsLoggedIn = true;
        MustLogin = false;
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
    
    public void Reset()
    {
        DisplayName = null;
        Picture = null;
        Email = null;
        Organizations = [];
        IsLoggedIn = false;
        MustLogin = true;
        Organization = null;
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