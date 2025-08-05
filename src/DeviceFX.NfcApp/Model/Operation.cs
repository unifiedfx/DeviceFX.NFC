using CommunityToolkit.Mvvm.ComponentModel;
using UFX.DeviceFX.NFC.Ndef;

namespace DeviceFX.NfcApp.Model;

public partial class Operation : OperationBase
{
    [ObservableProperty] 
    private string? result;
    [ObservableProperty] 
    private PhoneDetails? phone;

    public IDictionary<string,string> Onboarding { get; } = new Dictionary<string, string>();
    public Func<Operation, ValueTask<List<NdefRecord>>>? Callback { get; set; }
    public ValueTask<List<NdefRecord>> InvokeCallbackAsync()
    {
        if (Callback == null) return new ();
        return Callback(this);
    }

    public void Reset()
    {
        Result = null;
        Onboarding.Clear();
        Phone = null;
        Callback = null;
        State = OperationState.Idle;
    }
}