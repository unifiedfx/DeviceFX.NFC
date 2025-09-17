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

    public const string OnboardingCucm = "CUCM";
    public const string OnboardingCloud = "Cloud";
    public const string OnboardingActivation = "Activation";
    public const string SelectedProvision = "Search";
    public const string SelectedOnboarding = "Onboarding";
    public const string SelectedInventory = "Inventory";

    /// <inheritdoc/>
    public MainViewModel(AppViewModel appViewModel, SettingsViewModel settingsViewModel, Operation operation, IEnumerable<StepContentPage> steps, ISearchService searchService, IWebexService webexService, IDeviceService deviceService, IInventoryService inventoryService, IPopupService popupService, IMessenger messenger) : base(steps)
    {
        this.appViewModel = appViewModel;
        this.settingsViewModel = settingsViewModel;
        this.operation = operation;
        this.searchService = searchService;
        this.webexService = webexService;
        this.deviceService = deviceService;
        this.inventoryService = inventoryService;
        this.popupService = popupService;
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
    [Preference<string>("onboarding-mode", OnboardingActivation)]
    private string onboardingMode = OnboardingActivation;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OnboardingCommand))]
    [Preference<string>("activation-code")]
    private string? activationCode;
    
    [RelayCommand(CanExecute = nameof(CanExecuteOnboarding))]
    public async Task OnboardingAsync()
    {
        Operation.Reset();
        switch (OnboardingMode)
        {
            case OnboardingCucm:
                Operation.Onboarding.Add("onboardingMethod","4");
                Operation.Merge = true;
                break;
            case OnboardingCloud:
                Operation.Onboarding.Add("onboardingMethod","2");
                Operation.Merge = true;
                break;
            case OnboardingActivation:
                Operation.Onboarding.Add("onboardingMethod","3");
                Operation.Onboarding.Add("onboardingDetail",ActivationCode);
                Operation.ActivationCode = ActivationCode;
                break;
        }
        Operation.Mode = $"Onboarding:{OnboardingMode}";
        await deviceService.ScanPhoneAsync(Operation);
    }
    public bool CanExecuteOnboarding() => OnboardingMode != OnboardingActivation || !string.IsNullOrEmpty(ActivationCode);

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
    
    [ObservableProperty]
    private ObservableCollection<SearchResult> searchResults = [];

    private CancellationTokenSource searchCts = new();
    [RelayCommand]
    public async Task SearchAsync(string? query)
    {
        await searchCts.CancelAsync();
        searchCts = new();
        _ = Search();
        async Task Search()
        {
            var results = await searchService.SearchAsync(query, Settings.User.Organization.Id, searchCts.Token);
            if (string.IsNullOrEmpty(query) || !results.Any()) 
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
    
    
    [RelayCommand(CanExecute = nameof(CanExecuteSelected))]
    public async Task ProvisionAsync()
    {
        // Start NFC Read then provision to Webex
        if(Operation.State == OperationState.Success)
        {
            Operation.State = OperationState.Idle;
            SearchSelection = null;
            await NextAsync();
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
                result = await webexService.AddDeviceByActivationCode(orgId: Settings.User.Organization.Id, model: ProvisionModel, personId: SearchSelection.Id);
            }
            if (!string.IsNullOrEmpty(result.Error))
            {
                Operation.Result = result.Error;
                Operation.State = OperationState.Failure;
                appViewModel.Title = "Provision";
            }
            else
            {
                ProvisionActivationCode = result.Code;
                Operation.ActivationCode = ProvisionActivationCode;
                Operation.Onboarding.Clear();
                Operation.Onboarding.Add("onboardingMethod","3");
                Operation.Onboarding.Add("onboardingDetail", ProvisionActivationCode);
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
                        Operation.State = OperationState.InProgress;
                        Operation.Mode = "Provision:ActivationCode";
                        Operation.DisplayName = SearchSelection?.Name;
                        Operation.DisplayNumber = SearchSelection?.Number;
                        result = await webexService.AddDeviceByActivationCode(orgId: Settings.User.Organization.Id, model: ProvisionModel, personId: SearchSelection.Id);
                        if (!string.IsNullOrEmpty(result.Code))
                        {
                            ProvisionActivationCode = result.Code;
                            Operation.ActivationCode = ProvisionActivationCode;
                            Operation.Onboarding.Add("onboardingMethod","3");
                            Operation.Onboarding.Add("onboardingDetail", ProvisionActivationCode);
                            await deviceService.ScanPhoneAsync(Operation);
                        }
                        else
                        {
                            Operation.State = OperationState.Failure;
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
            await deviceService.ScanPhoneAsync(Operation);
            if (Operation.State == OperationState.Success)
            {
                var result = await webexService.AddDeviceByMac(orgId: Settings.User.Organization.Id, mac: Operation.Phone.Mac, model:Operation.Phone.Pid, personId: SearchSelection.Id);
                if (result != null)
                {
                    Operation.Result = result;
                    Operation.State = OperationState.Failure;
                    appViewModel.Title = "Provision";
                }
                else appViewModel.Title = "Provisioned";
            }
            else
            {
                appViewModel.Title = "Provision";
            }
        }
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
    }
    
    [RelayCommand]
    public async Task ScanAsync()
    {
        try
        {
            Operation.Reset();
            Operation.Mode = "Inventory";
            Operation.Merge = true;
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
        await NextAsync(page);
    }
    #endregion
}