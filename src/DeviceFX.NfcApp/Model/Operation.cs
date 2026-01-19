using CommunityToolkit.Mvvm.ComponentModel;
using DeviceFX.NfcApp.Services;
using UFX.DeviceFX.NFC.Ndef;

namespace DeviceFX.NfcApp.Model;

public partial class Operation(CdaService cdaService) : OperationBase
{
    public const string OnboardingMethod = "onboardingMethod";
    public const string OnboardingDetail = "onboardingDetail";
    public const string OnboardingConfig = "onboardingConfig";

    [ObservableProperty]
    private string? result;
    [ObservableProperty]
    private PhoneDetails? phone;
    public bool Merge { get; set; }
    public string? Mode { get; set; }
    public string? ActivationCode { get; set; }
    public string? DisplayName { get; set; }
    public string? DisplayNumber { get; set; }

    public IDictionary<string,string> Onboarding { get; set; } = new Dictionary<string, string>();
    public Func<Operation, ValueTask<List<NdefRecord>>>? Callback { get; set; }
    public ValueTask<List<NdefRecord>> InvokeCallbackAsync()
    {
        if (Callback == null) return new ();
        return Callback(this);
    }
    public async Task<List<NdefRecord>?> GetConfig()
    {
        await signatureCts.CancelAsync();
        signatureCts = new();
        var config = Phone?.CreateConfig(Onboarding, true);
        if(config == null || Phone == null) return null;
        return Phone.RequiresSigning ? await SignedEncryptedConfig(config) : await EncryptedConfig(config);
    }

    private async Task<List<NdefRecord>?> SignedEncryptedConfig(string config)
    {
        if (Phone?.Mac == null) return null;
        if (signature == null || signatureMac != Phone.Mac || signatureConfig != config)
        {
            signature = await cdaService.SignData(config, Phone.Mac, signatureCts.Token);
            signatureMac = Phone.Mac;
            signatureConfig = config;
        }
        var encrypted = Phone.Encrypt(signature);
        if(encrypted == null) return null;
        return [new MimeNdefRecord("application/x-phoneos-sign-encrypt", encrypted)];
    }

    private async Task<List<NdefRecord>?> EncryptedConfig(string config)
    {
        var encrypted = Phone?.Encrypt(config);
        if(encrypted == null) return null;
        return [new MimeNdefRecord("application/x-phoneos-encrypt", encrypted)];
    }

    private CancellationTokenSource signatureCts = new();
    private byte[]? signature;
    private string? signatureMac;
    private string? signatureConfig;
    public void ResetSignature()
    {
        signature = null;
        signatureMac = null;
        signatureConfig = null;
    }

    public void Reset()
    {
        Merge = false;
        Mode = null;
        ActivationCode = null;
        DisplayName = null;
        DisplayNumber = null;
        Result = null;
        Onboarding.Clear();
        Phone = null;
        Callback = null;
        State = OperationState.Idle;
    }
}