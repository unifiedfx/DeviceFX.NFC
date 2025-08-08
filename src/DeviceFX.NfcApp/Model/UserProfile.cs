using CommunityToolkit.Mvvm.ComponentModel;

namespace DeviceFX.NfcApp.Model;

public partial class UserProfile : ObservableObject
{
    [ObservableProperty]
    private string displayName;

    [ObservableProperty]
    private string? picture;

    [ObservableProperty]
    private bool isLoggedIn;
}