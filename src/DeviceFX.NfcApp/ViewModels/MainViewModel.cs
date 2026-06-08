using System.Collections.ObjectModel;
using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Helpers;
using DeviceFX.NfcApp.Helpers.Preference;
using DeviceFX.NfcApp.Model;
using DeviceFX.NfcApp.Services;
using DeviceFX.NfcApp.Views.Shared;
using UFX.DeviceFX.NFC.Ndef;

namespace DeviceFX.NfcApp.ViewModels;

public partial class MainViewModel : WizardViewModelBase
{
    private readonly AppViewModel appViewModel;
    private readonly SettingsViewModel settingsViewModel;
    private readonly Operation operation;
    private readonly ISearchService searchService;
    private readonly IWebexService webexService;
    private readonly IDeviceService deviceService;
    private readonly IInventoryService inventoryService;
    private readonly IPopupService popupService;
    private readonly CdaService cdaService;
    private readonly IPrintManager printManager;

    public const string OnboardingCloud = "Cloud";
    public const string OnboardingActivation = "Activation";
    public const string OnboardingCUCM = "CUCM";
    public const string SelectedProvision = "Search";
    public const string SelectedOnboarding = "Onboarding";
    public const string SelectedInventory = "Inventory";

    /// <inheritdoc/>
    public MainViewModel(AppViewModel appViewModel,
        SettingsViewModel settingsViewModel,
        Operation operation,
        IEnumerable<StepContentPage> steps,
        ISearchService searchService,
        IWebexService webexService,
        IDeviceService deviceService,
        IInventoryService inventoryService,
        IPopupService popupService,
        CdaService cdaService,
        IMessenger messenger,
        IPrintManager printManager) : base(steps)
    {
        this.appViewModel = appViewModel;
        this.settingsViewModel = settingsViewModel;
        this.operation = operation;
        this.searchService = searchService;
        this.webexService = webexService;
        this.deviceService = deviceService;
        this.inventoryService = inventoryService;
        this.popupService = popupService;
        this.cdaService = cdaService;
        this.printManager = printManager;
        messenger.Register<OrganizationMessage>(this, async (recipient, message) =>
        {
            SearchResults.Clear();
            SearchSelection = null;
            SearchInput = String.Empty;
            await this.RemoveAsync(nameof(SearchInput));
        });
    }

    [Preference<string>("selected-mode", SelectedProvision)]
    private string selectedMode = SelectedProvision;
    
    public string SelectedMode
    {
        get => selectedMode;
        set
        {
            Settings.User.MustLogin = value == SelectedProvision && !Settings.User.IsLoggedIn;
            if (selectedMode == value) return;
            SetProperty(ref selectedMode, value);
        }
    }

    public Operation Operation => operation;
    public Settings Settings => settingsViewModel.Settings;

    #region Onboarding

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OnboardingCommand))]
    [Preference<string>("onboarding-mode", OnboardingCloud)]
    private string onboardingMode = OnboardingCloud;
    
    [ObservableProperty]
    [Preference<string>("asset-tag")]
    private string? assetTag;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OnboardingCommand))]
    [Preference<string>("cloud-profile")]
    private string? cloudProfile;

    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(OnboardingCommand))] 
    [Preference<string>("cloud-ca-rule")]
    private string? cloudCaRule;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OnboardingCommand))]
    [Preference<string>("cucm-server")]
    private string? cucmServer;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OnboardingCommand))]
    [Preference<string>("activation-code")]
    private string? activationCode;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OnboardingCommand))]
    [NotifyCanExecuteChangedFor(nameof(ProvisionCommand))]
    [Preference<bool>("wifi-include", false)]
    private bool wifiInclude;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OnboardingCommand))]
    [Preference<string>("wifi-security-mode", "PSK")]
    private string wifiSecurityMode;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OnboardingCommand))]
    [Preference<string>("wifi-name")]
    private string? wifiName;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OnboardingCommand))]
    [Preference<string>("wifi-user")]
    private string? wifiUser;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OnboardingCommand))]
    [SecurePreference<string>("wifi-password")]
    private string? wifiPassword;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OnboardingCommand))]
    [Preference<string>("wifi-band", "Auto")]
    private string wifiBand;

    [ObservableProperty] private string[] wifiSecurityModes = ["Auto","PSK","EAP-FAST","EAP-PEAP", "None"];

    [ObservableProperty] private bool wifiIncludeUsername;

    [ObservableProperty] private bool wifiIncludePassword = true;
    
    [ObservableProperty] private bool wifiCanInclude;

    [ObservableProperty] private bool cdaCheckBusy;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OnboardingCommand))]
    [NotifyCanExecuteChangedFor(nameof(ProvisionCommand))]
    private string? canSignError;
    
    private bool? canSignData;
    
    private async Task ShowCdaError(string? error)
    {
        if(error == null) return;
        await MainThread.InvokeOnMainThreadAsync(() => CanSignError = error);
        await Task.Delay(TimeSpan.FromSeconds(10));
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            CanSignError = null;
            canSignData = null;
        });
    }

    [RelayCommand(CanExecute = nameof(CanExecuteOnboarding))]
    public async Task OnboardingAsync()
    {
        if (!await CheckCanSignData(OnboardingMode)) return;
        Operation.Reset();
        Operation.Onboarding = GetOnboarding(OnboardingMode, ActivationCode);
        if (OnboardingMode == OnboardingActivation) Operation.ActivationCode = ActivationCode;
        Operation.Organization = Settings.User.Organization?.Name;
        if (WifiInclude) Operation.WifiName = WifiName;
        Operation.AssetTag = AssetTag;
        Operation.Mode = $"Onboarding:{OnboardingMode}";
        await deviceService.ScanPhoneAsync(Operation);
    }
    
    public bool CanExecuteOnboarding()
    {
        if(WifiInclude && !WifiValid()) return false;
        var requiresSigning = RequiresSigning(GetOnboarding(OnboardingMode, ActivationCode));
        var canSign = !requiresSigning || !canSignData.HasValue || canSignData.Value || CanSignError == null;
        return OnboardingMode switch
        {
            OnboardingActivation => canSign && !string.IsNullOrEmpty(ActivationCode),
            _ => canSign
        };
    }
    
    public async Task<bool> CheckCanSignData(string mode, string messageTemplate = "{0}")
    {
        var onboarding = GetOnboarding(mode);
        var requiresSigning = RequiresSigning(onboarding);
        if(!requiresSigning) return true;
        string? error = null;
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            CdaCheckBusy = true;
            canSignData = await cdaService.CanSignData(cts.Token);
            if (!canSignData.Value)
            {
                error = cdaService.GetError();
                CdaCheckBusy = false;
                await Shell.Current.DisplayAlert("CDA Signing Service", string.Format(messageTemplate, error), "Ok");
            }
        }
        catch (Exception)
        {
            canSignData = false;
            error = "Service unreachable";
            CdaCheckBusy = false;
            await Shell.Current.DisplayAlert("CDA Signing Service", string.Format(messageTemplate, error), "Ok");
        }
        CdaCheckBusy = false;
        if(error != null) _ = ShowCdaError(error);
        return canSignData.HasValue && canSignData.Value;
    }

    private IDictionary<string,string> GetOnboarding(string mode, string? code = null)
    { 
        var onboarding = new Dictionary<string, string>();
        switch (mode)
        {
            case OnboardingCUCM:
                if(!string.IsNullOrWhiteSpace(CucmServer)) onboarding.Add(Operation.OnboardingDetail, CucmServer);
                onboarding.Add(Operation.OnboardingMethod, onboarding.ContainsKey(Operation.OnboardingDetail) ? "5" : "4");
                break;
            case OnboardingActivation:
                if(!string.IsNullOrWhiteSpace(code)) onboarding.Add(Operation.OnboardingDetail, code);
                onboarding.Add(Operation.OnboardingMethod,"3");
                break;
            default:
                if(mode == OnboardingCloud && !string.IsNullOrWhiteSpace(CloudProfile)) onboarding.Add(Operation.OnboardingDetail, CloudProfile);
                if(mode == OnboardingCloud && !string.IsNullOrWhiteSpace(CloudCaRule)) onboarding.Add("Custom_CA_Rule", CloudCaRule);
                onboarding.Add(Operation.OnboardingMethod, onboarding.ContainsKey(Operation.OnboardingDetail) ? "1" : "2");
                break;
        }
        if (!WifiInclude || !WifiValid()) return onboarding;
        if(!string.IsNullOrWhiteSpace(WifiName)) onboarding.Add("Network_Name_1_",WifiName);
        if(WifiIncludeUsername && !string.IsNullOrWhiteSpace(WifiUser)) onboarding.Add("Wi-Fi_User_ID_1_",WifiUser);
        if(WifiIncludePassword && !string.IsNullOrWhiteSpace(WifiPassword)) onboarding.Add("Wi-Fi_Password_1_",WifiPassword);
        onboarding.Add("Wi-Security_Mode_1_", string.IsNullOrWhiteSpace(WifiSecurityMode) ? "Auto" : WifiSecurityMode);
        onboarding.Add("Wi-Frequency_Band_1_", string.IsNullOrWhiteSpace(WifiBand) ? "Auto" : WifiBand);
        return onboarding;
    }

    private bool RequiresSigning(IDictionary<string,string> onboarding)
    {
        var onboardingMethod = 2;
        if (onboarding.TryGetValue(Operation.OnboardingMethod, out var onboardingMethodValue))
        {
            onboardingMethod = int.TryParse(onboardingMethodValue, out var method) ? method : 2;            
        }
        if(onboardingMethod is 1 or 5) return true;
        var ignoreCount = onboarding.Count(d => d.Key is Operation.OnboardingMethod or Operation.OnboardingDetail);
        return onboarding.Count > ignoreCount;
    }

    private bool WifiValid()
    {
        WifiIncludeUsername = WifiSecurityMode switch {"PSK" or "None" => false, _ => true};
        WifiIncludePassword = WifiSecurityMode switch {"None" => false, _ => true};
        if(string.IsNullOrWhiteSpace(WifiName)) return false;
        if (WifiIncludeUsername && string.IsNullOrWhiteSpace(WifiUser)) return false;
        if (WifiIncludePassword && string.IsNullOrWhiteSpace(WifiPassword)) return false;
        return true;
    }

    private static string? CleanActivationCode(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var digits = new string(input.Where(char.IsDigit).ToArray());
        if (digits.Length > 16) digits = digits[..16];
        return digits.Length == 0 ? null : digits;
    }

    partial void OnActivationCodeChanged(string? value)
    {
        var cleaned = CleanActivationCode(value);
        if (cleaned != value)
        {
            ActivationCode = cleaned;
        }
    }
    
    #endregion

    #region Search

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectedCommand))]
    [Preference<string>("search-input")]
    private string? searchInput;

    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(ProvisionCommand))]
    private SearchResult? searchSelection;
    
    partial void OnSearchSelectionChanged(SearchResult? value)
    {
        ProvisionActivationCode = null;
    }
    
    [ObservableProperty]
    private ObservableCollection<SearchResult> searchResults = [];

    private CancellationTokenSource searchCts = new();
    [RelayCommand]
    public void Search(string? query)
    {
        searchCts.Cancel();
        searchCts = new();
        _ = Task.Run(Query);
        async Task Query()
        {
            var token = searchCts.Token;
            await Task.Delay(300, token);
            if(token.IsCancellationRequested) return;
            var results = await searchService.SearchAsync(query, Settings.User.Organization.Id, token);
            if(token.IsCancellationRequested) return;
            if (string.IsNullOrEmpty(query) || results.Count == 0) 
                SearchResults.Clear();
            else
                SearchResults = new ObservableCollection<SearchResult>(results.Order());
            SearchSelection = null;
        }
    }

    [RelayCommand]
    public async Task SelectionChangedAsync()
    {
        if(SearchSelection == null) return;
        if (SearchSelection.Checked)
        {
            if (SearchSelection.Issue != null) SearchSelection = null;
            return;
        }
        await webexService.UpdateOrganization(Settings.User, Settings.User.Organization.Id);
        await searchService.CheckResult(SearchSelection, Settings.User.Organization?.Id, Settings.User?.Organization?.LicenseIds);
        if(SearchSelection.Issue == null || !SearchSelection.Checked) return;
        var issue = SearchSelection.Issue;
        SearchSelection = null;
        await Shell.Current.DisplayAlert("Unable to use", $"{issue}", "Ok");
    }

    [RelayCommand(CanExecute = nameof(CanExecuteSelected))]
    public async Task SelectedAsync()
    {
        Operation.Reset();
        await NextAsync();
    }

    public bool CanExecuteSelected() => SearchSelection != null;
    #endregion

    #region Provision

    [Preference<bool>("provision-activation")]
    [ObservableProperty]
    private bool isModelSelected;

    [Preference<string>("provision-model", "DP-9841")]
    [ObservableProperty]
    private string provisionModel = "DP-9841";

    [ObservableProperty]
    private string? provisionActivationCode;
    [ObservableProperty]
    private string? provisionResultActivationCode;

    public bool IsCodeVisible => IsModelSelected && ProvisionActivationCode != null;

    partial void OnIsModelSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsCodeVisible));
    }
    partial void OnProvisionActivationCodeChanged(string value)
    {
        OnPropertyChanged(nameof(IsCodeVisible));
    }
    
    [ObservableProperty]
    private List<string> models = new(["DP-9841", "DP-9851", "DP-9861", "DP-9871"]);
    
    
    public bool CanExecuteProvision()
    {
        WifiCanInclude = WifiValid();
        var requiresSigning = RequiresSigning(GetOnboarding("Provision"));
        return SearchSelection != null && (!requiresSigning || !canSignData.HasValue || canSignData.Value || CanSignError == null);
    }

    [RelayCommand(CanExecute = nameof(CanExecuteProvision))]
    public async Task ProvisionAsync()
    {
        if(SearchSelection == null) return;
        // Start NFC Read then provision to Webex
        if(Operation.State == OperationState.Success)
        {
            Operation.State = OperationState.Idle;
            SearchSelection = null;
            await NextAsync();
            return;
        }

        if (!await CheckCanSignData("Provision", "{0}, cannot save WiFi details at this time"))
        {
            await MainThread.InvokeOnMainThreadAsync(() => WifiInclude = false);
            return;
        }

        Operation.Reset();
        appViewModel.Title = "Provisioning";
        Operation.State = OperationState.InProgress;
        ProvisionResultActivationCode = null;
        if (IsModelSelected)
        {
            WebexService.ActivationResult result = new WebexService.ActivationResult(ProvisionActivationCode);
            Operation.Mode = "Provision:ActivationCode";
            Operation.DisplayName = SearchSelection?.Name;
            Operation.DisplayNumber = SearchSelection?.Number;
            if (string.IsNullOrEmpty(ProvisionActivationCode))
            {
                result = await webexService.AddDeviceByActivationCode(orgId: Settings.User.Organization.Id, model: ProvisionModel, type: SearchSelection.Type, id: SearchSelection.Id);
            }
            if (!string.IsNullOrEmpty(result.Error))
            {
                Operation.Result = result.Error;
                Operation.State = OperationState.Failure;
                Operation.Phone ??= new PhoneDetails();
                appViewModel.Title = "Provision";
            }
            else
            {
                ProvisionActivationCode = result.Code;
                Operation.ActivationCode = ProvisionActivationCode;
                Operation.Organization = Settings.User.Organization?.Name;
                Operation.AssetTag = AssetTag;
                if (WifiInclude) Operation.WifiName = WifiName;
                Operation.Onboarding = GetOnboarding(OnboardingActivation, ProvisionActivationCode);
                var incorrectModel = false;
                Operation.Callback = o =>
                {
                    if (o.Phone?.Pid != null && !string.Equals(o.Phone.Pid, ProvisionModel,
                            StringComparison.InvariantCultureIgnoreCase))
                    {
                        incorrectModel = true;
                        o.State = OperationState.Failure;
                        o.Result = $"Incorrect Model, expecting: {ProvisionModel}";
                    }
                    return new(new List<NdefRecord>());
                };
                await deviceService.ScanPhoneAsync(Operation);
                if (incorrectModel)
                {
                    if (await Application.Current?.MainPage?.DisplayAlert("Incorrect Model", "Update model and try again?", "Retry", "Cancel"))
                    {
                        ProvisionModel = Operation.Phone.Pid;
                        Operation.Reset();
                        Operation.Organization = Settings.User.Organization?.Name;
                        Operation.AssetTag = AssetTag;
                        if (WifiInclude) Operation.WifiName = WifiName;
                        Operation.State = OperationState.InProgress;
                        Operation.Mode = "Provision:ActivationCode";
                        Operation.DisplayName = SearchSelection?.Name;
                        Operation.DisplayNumber = SearchSelection?.Number;
                        result = await webexService.AddDeviceByActivationCode(orgId: Settings.User.Organization.Id, model: ProvisionModel, type: SearchSelection.Type, id: SearchSelection.Id);
                        if (!string.IsNullOrEmpty(result.Code))
                        {
                            ProvisionActivationCode = result.Code;
                            Operation.ActivationCode = ProvisionActivationCode;
                            Operation.Onboarding = GetOnboarding(OnboardingActivation, ProvisionActivationCode);
                            await deviceService.ScanPhoneAsync(Operation);
                        }
                        else
                        {
                            Operation.State = OperationState.Failure;
                            Operation.Phone ??= new PhoneDetails();
                            Operation.Result = result.Error;
                        }
                    }
                }
                if(Operation.State == OperationState.Success)
                {
                    appViewModel.Title = "Provisioned";
                    ProvisionResultActivationCode = ProvisionActivationCode;
                    ProvisionActivationCode = null;
                }
                else
                {
                    appViewModel.Title = "Provision";
                }
            }
        }
        else
        {
            Operation.Mode = "Provision:MacAddress";
            Operation.DisplayName = SearchSelection?.Name;
            Operation.DisplayNumber = SearchSelection?.Number;
            Operation.Organization = Settings.User.Organization?.Name;
            Operation.AssetTag = AssetTag;
            if (WifiInclude) Operation.WifiName = WifiName;
            await deviceService.ScanPhoneAsync(Operation);
            if (Operation.State == OperationState.Success)
            {
                var result = await webexService.AddDeviceByMac(
                    orgId: Settings.User.Organization.Id,
                    mac: Operation.Phone.Mac,
                    model: Operation.Phone.Pid,
                    type: SearchSelection?.Type,
                    id: SearchSelection.Id);
                if (result != null)
                {
                    Operation.Result = result;
                    Operation.State = OperationState.Failure;
                    Operation.Phone ??= new PhoneDetails();
                    appViewModel.Title = "Provision";
                }
                else appViewModel.Title = "Provisioned";
            }
            else
            {
                appViewModel.Title = "Provision";
            }
        }

        if (Operation.State == OperationState.Success
            && !string.IsNullOrWhiteSpace(Operation.Phone?.DisplayName)
            && !string.IsNullOrWhiteSpace(Operation.Phone?.DisplayNumber)) await PrintAsync(Operation.Phone);
    }

    [RelayCommand]
    public async Task ProvisionBackAsync()
    {
        Operation.Reset();
        SearchSelection = null;
        await BackAsync();
    }
    #endregion

    #region Inventory

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShareCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearCommand))]
    private ObservableCollection<PhoneDetails> phoneList = [];

    [ObservableProperty]
    private PhoneDetails? selectedPhone;
    
    public async Task LoadPhonesAsync()
    {
        PhoneList = new ObservableCollection<PhoneDetails>(await inventoryService.GetPhonesAsync());
        CanPrint = Settings.PrinterEnabled && await printManager.CanPrintAsync();
    }
    
    [RelayCommand]
    public async Task ScanAsync()
    {
        try
        {
            Operation.Reset();
            Operation.Mode = "Inventory";
            Operation.Merge = true;
            Operation.AssetTag = AssetTag;
            Operation.Organization = Settings.User.Organization?.Name;
            if (long.TryParse(Settings.AutoNumber, out var autoNumber))
            {
                Operation.DisplayNumber = autoNumber.ToString();
                Operation.Merge = false;
            }
            else autoNumber = -1;
            await deviceService.ScanPhoneAsync(operation);
            if (operation.State == OperationState.Success && autoNumber++ > 0)
            {
                Settings.AutoNumber = autoNumber.ToString();
                await Settings.SaveAsync(nameof(Settings.AutoNumber));
                await PrintAsync(Operation.Phone);
            }
            await LoadPhonesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    [RelayCommand(CanExecute = nameof(CanShare))]
    public async Task ShareAsync()
    {
        var csv = await Application.Current?.MainPage?.DisplayAlert("Export Format", "Choose the export format", "CSV", "Excel")!;
        var filePath = await inventoryService.ExportAsync(csv ? "csv" : "xlsx");
        if(filePath == null) return;
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Share Phone Inventory",
            File = new ShareFile(filePath)
        });
    }

    [RelayCommand(CanExecute = nameof(CanShare))]
    public async Task ClearAsync()
    {
        var result = await Application.Current?.MainPage?.DisplayAlert("Remove Phones", "Do you wish to remove all phones?", "Yes", "No")!;
        if(!result) return;
        await inventoryService.ClearAsync();
        PhoneList.Clear();
        ShareCommand.NotifyCanExecuteChanged();
        ClearCommand.NotifyCanExecuteChanged();
    }
    public bool CanShare() => PhoneList.Count > 0;
    
    [RelayCommand]
    public async Task PhoneClickedAsync(PhoneDetails phone)
    {
        SelectedPhone = phone;
        if (SelectedPhone != null) await popupService.ShowPopupAsync<MainViewModel>(Shell.Current);
    }
    #endregion

    #region Start

    [RelayCommand]
    public async Task LoginAsync(string? page = null)
    {
        try
        {
            SearchInput = String.Empty;
            await this.RemoveAsync("SearchInput");
            await settingsViewModel.LoginCommand.ExecuteAsync(null);
        }
        catch (OperationCanceledException e)
        {
            return;
        }
        CanPrint = Settings.PrinterEnabled && await printManager.CanPrintAsync();
        if(Settings.User.IsLoggedIn) await NextAsync(page);
    }
    [RelayCommand]
    public async Task StartAsync(string? page = null)
    {
        if (!SearchResults.Any() && !string.IsNullOrEmpty(SearchInput))
        {
            SearchInput = String.Empty;
            await this.RemoveAsync("SearchInput");
        }
        CanPrint = Settings.PrinterEnabled && await printManager.CanPrintAsync();
        await NextAsync(page);
    }
    #endregion
    
    #region Print

    [ObservableProperty] private bool canPrint;
    
    partial void OnSelectedPhoneChanged(PhoneDetails? value)
    {
        (PrintCommand as AsyncRelayCommand<PhoneDetails?>)?.NotifyCanExecuteChanged();
    }

    public bool CanExecutePrint()
    {
        if(!CanPrint) return false;
        if (!string.IsNullOrWhiteSpace(SelectedPhone?.AssetTag)) return true;
        if (!string.IsNullOrWhiteSpace(SelectedPhone?.DisplayNumber)) return true;
        if (string.IsNullOrWhiteSpace(SelectedPhone?.DisplayName)) return false;
        return true;
    }

    [RelayCommand(CanExecute = nameof(CanExecutePrint))]
    public async Task PrintAsync(PhoneDetails? phone = null)
    {
        if(phone == null || !CanPrint) return;
        List<string> rows = [];
        if(!string.IsNullOrWhiteSpace(Settings.User.Organization?.Name)) rows.Add("Org: " + Settings.User.Organization.Name);
        if(!string.IsNullOrWhiteSpace(phone.DisplayName)) rows.Add("Name: " + phone.DisplayName);
        if(!string.IsNullOrWhiteSpace(phone.DisplayNumber)) rows.Add("Number: " + phone.DisplayNumber);
        if(!string.IsNullOrWhiteSpace(phone.AssetTag)) rows.Add("Tag: " + phone.AssetTag);
        if(!string.IsNullOrWhiteSpace(phone.Pid)) rows.Add("Product: " + phone.Pid);
        if(!string.IsNullOrWhiteSpace(phone.Serial)) rows.Add("Serial #: " + phone.Serial);
        if(!string.IsNullOrWhiteSpace(phone.Mac)) rows.Add("MAC: " + phone.Mac);
        if (!string.IsNullOrWhiteSpace(phone.WifiMac))
        {
            rows.Add("WIFI MAC: " + phone.WifiMac);
            if(WifiInclude && !string.IsNullOrWhiteSpace(WifiName)) rows.Add("SSID: " + WifiName);
        }
        rows.Add("Issued: " + phone.Updated.ToString("yyyy-MM-dd"));
        if (!string.IsNullOrWhiteSpace(phone.ActivationCode))
        {
            rows.Add("Expires: " + phone.Updated.AddDays(30).ToString("yyyy-MM-dd"));
        }
        await printManager.PrintAsync(rows);
    }
    
    #endregion
}