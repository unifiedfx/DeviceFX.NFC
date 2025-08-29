using CommunityToolkit.Mvvm.ComponentModel;

namespace DeviceFX.NfcApp.Model;

public partial class UserProfile : ObservableObject
{
    public WebexAccount? Account { get; private set; }
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
    
    public void Set(WebexAccount? account)
    {
        if(account == null) return;
        Account = account;
        DisplayName = account.DisplayName;
        Picture = account.Picture;
        OrgId = account.CurrentOrgId;
        IsLoggedIn = true;
        MustLogin = false;
    }
    
    public void Reset()
    {
        Account = null;
        DisplayName = null;
        Picture = null;
        OrgId = null;
        IsLoggedIn = false;
        MustLogin = true;
    }
}