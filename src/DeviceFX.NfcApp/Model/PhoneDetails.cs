using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Org.BouncyCastle.Cms;
using SQLite;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace DeviceFX.NfcApp.Model;

public partial class PhoneDetails : ObservableObject
{
    public byte[]? Certificate { get; set; }
    private string id;
    [PrimaryKey]
    public string Id
    {
        get { return id; }
        set { SetProperty(ref id, value); }
    }
    [ObservableProperty]
    private string pid;
    private string mac;
    public string Mac
    {
        get => string.IsNullOrWhiteSpace(Id) || Id.Length < 3 ? Id : Id[3..];
        set => SetProperty(ref mac,Id = $"SEP{value}");
    }

    [ObservableProperty]
    private string? wifiMac;
    [ObservableProperty]
    private string serial;
    [ObservableProperty]
    private string? vid;
    [ObservableProperty]
    private string? tagSerial;
    [ObservableProperty]
    private string? url;
    [ObservableProperty]
    private string? label;
    [ObservableProperty]
    private string? nfcVersion;
    [ObservableProperty]
    private string? assetTag;
    [ObservableProperty]
    private string? latitude;
    [ObservableProperty]
    private string? longitude;
    [ObservableProperty]
    private string? postcode;
    [ObservableProperty]
    private string? country;
    [ObservableProperty]
    private DateTime updated = DateTime.UtcNow;

    // [ObservableProperty]
    public string Image =>
        Pid switch
        {
            "DP-9841" => "cisco_9841_original.png",
            "DP-9851" => "cisco_9851_original.png",
            "DP-9861" => "cisco_9861_original.png",
            "DP-9871" => "cisco_9871_original.png",
            _ => "cisco_9841_original.png"
        };

    public PhoneDetails() { }

    public PhoneDetails(string label)
    {
        Label = label;
        var details = label.Split(
            new string[] { "\r\n", "\r", "\n" },
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
        ).ToDictionary(l => l.Split(':')[0].Trim(), l => l.Split(':')[1].Trim());
        if(details.TryGetValue("PID", out var pid)) Pid = pid;
        if(details.TryGetValue("SN", out var sn)) Serial = sn;
        if(details.TryGetValue("VID", out var vid)) Vid = vid;
        if(details.TryGetValue("LAN MAC", out var mac)) Mac = mac;
        if(details.TryGetValue("WIFI MAC", out var wifiMac)) WifiMac = wifiMac;
    }

    public string UpdateTemplate(string template)
    {
        var placeholders = new Dictionary<string, string>()
        {
            {"$(MA)", Mac.ToLowerInvariant()},
            {"$(MAU)", Mac.ToUpperInvariant()},
            {"$(MAC)", string.Join(':', Enumerable.Range(0,Mac.Length/2).Select(r => Mac.Substring(r*2,2))).ToLowerInvariant()},
            {"$(PN)", Pid},
            {"$(PID)", Pid},
            {"$(SN)", Serial},
            {"$(WMA)", WifiMac ?? string.Empty},
            {"$(VID)", Vid ?? string.Empty},
            {"$(TAG)", TagSerial ?? string.Empty},
            {"$(VER)", NfcVersion ?? string.Empty}
        };
        foreach (var key in placeholders.Keys.Where(key => template.Contains(key)))
        {
            template = template.Replace(key, placeholders[key]);
        }
        return Url = template;
    }    
    public byte[]? Encrypt(string inputText)
    {
        if (Certificate == null || Certificate.Length == 0) return null;
        try
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputText);
            X509Certificate cert = new X509Certificate(Certificate);
            // Create CMS enveloped data generator
            CmsEnvelopedDataGenerator envelopedGen = new CmsEnvelopedDataGenerator();
            // Add recipient using the certificate
            envelopedGen.AddKeyTransRecipient(cert);
            // Create the CMS enveloped data
            CmsProcessableByteArray input = new CmsProcessableByteArray(inputBytes);
            // Configure encryption algorithm (AES-256-CBC)
            string encryptionAlgorithm = CmsEnvelopedGenerator.Aes256Cbc;
            // Generate the encrypted CMS data
            CmsEnvelopedData envelopedData = envelopedGen.Generate(
                input,
                encryptionAlgorithm
            );
            // Get the DER-encoded bytes
            byte[] encryptedBytes = envelopedData.GetEncoded();
            return encryptedBytes;
        }
        catch (Exception ex)
        {
            // throw new CryptographicException("CMS Encryption failed: " + ex.Message);
        }
        return null;
    }
    
    public string? CreateConfig(IEnumerable<KeyValuePair<string, string>> config, bool json = false)
    {
        var dict = new Dictionary<string,string>(config, StringComparer.OrdinalIgnoreCase);
        if(dict.Count == 0) return null;
        var onboardingMethod = int.TryParse(dict["onboardingMethod"], out var method)
            ? method
            : 2;
        dict.TryGetValue("onboardingDetail", out var onboardingDetail);
        if(onboardingMethod is 1 or 3 or 5 && onboardingDetail is null) throw new ArgumentOutOfRangeException(nameof(config), $"Onboarding method {onboardingMethod} requires onboardingDetail.");
        return json ? GetJsonConfig() : GetTextConfig();
        
        string GetTextConfig()
        {
            dict["mac"] = Mac.ToUpperInvariant();
            var sb = new StringBuilder();
            foreach (var kvp in dict) sb.AppendLine($"{kvp.Key}:{kvp.Value}");
            return sb.ToString();
        }
        string GetJsonConfig()
        {
            
            var json = new Dictionary<string, object>
            {
                { "onboardingMethod", onboardingMethod },
                { "mac", Mac.ToUpperInvariant() }
            };
            if (onboardingDetail is not null) json.Add("onboardingDetail", onboardingDetail);
            dict.Remove("onboardingMethod");
            dict.Remove("onboardingDetail");
            if(dict.Count > 0) json["onboardingConfig"] = dict;
            return System.Text.Json.JsonSerializer.Serialize(json);
        }
    }

    public override string ToString() => $"{Id} ({Pid})";
}