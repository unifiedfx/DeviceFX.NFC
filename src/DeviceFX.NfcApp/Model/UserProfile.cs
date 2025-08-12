using CommunityToolkit.Mvvm.ComponentModel;
using DeviceFX.NfcApp.Model.Dto;

namespace DeviceFX.NfcApp.Model;

public partial class UserProfile : ObservableObject
{
    [ObservableProperty]
    private string? displayName;

    [ObservableProperty]
    private string? picture;

    [ObservableProperty]
    private string? orgId;

    [ObservableProperty]
    private bool isLoggedIn;

    [ObservableProperty]
    private bool mustLogin;
    
    public void Set(WebexIdentityUserDto? user)
    {
        if(user == null) return;
        DisplayName = user.displayName;
        Picture = user.Picture;
        OrgId = user.webex.organization.organizationId;
        IsLoggedIn = true;
        MustLogin = false;
    }
    
    public void Reset()
    {
        DisplayName = null;
        Picture = null;
        OrgId = null;
        IsLoggedIn = false;
        MustLogin = true;
    }
}