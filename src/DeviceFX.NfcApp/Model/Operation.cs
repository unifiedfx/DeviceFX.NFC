using CommunityToolkit.Mvvm.ComponentModel;

namespace DeviceFX.NfcApp.Model;

public partial class Operation : OperationBase
{
    [ObservableProperty] 
    private string? result;
    [ObservableProperty] 
    private PhoneDetails phone;
    
    public IDictionary<string,string> Onboarding { get; } = new Dictionary<string, string>();
}