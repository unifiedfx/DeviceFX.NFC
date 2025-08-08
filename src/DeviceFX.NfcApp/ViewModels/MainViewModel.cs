using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Helpers.Preference;
using DeviceFX.NfcApp.Model;
using DeviceFX.NfcApp.Views.Shared;
using UFX.DeviceFX.NFC.Ndef;

namespace DeviceFX.NfcApp.ViewModels;

public partial class MainViewModel(AppViewModel appViewModel, SettingsViewModel settingsViewModel, Operation operation, IEnumerable<StepContentPage> steps, ISearchService searchService, IDeviceService deviceService, IInventoryService inventoryService, IPopupService popupService) : WizardViewModelBase(steps)
{
    public const string OnboardingCucm = "CUCM";
    public const string OnboardingCloud = "Cloud";
    public const string OnboardingActivation = "Activation";
    public const string SelectedProvision = "Search";
    public const string SelectedOnboarding = "Onboarding";
    public const string SelectedInventory = "Inventory";

    [Preference<string>("selected-mode", SelectedProvision)]
    private string selectedMode = SelectedProvision;
    
    public string SelectedMode
    {
        get => selectedMode;
        set
        {
            if (selectedMode == value) return;
            SetProperty(ref selectedMode, value);
            OnPropertyChanged(nameof(StartText));
            // settingsViewModel.Settings.User.OnPropertyChanged(nameof(settingsViewModel.Settings.User.IsLoggedIn));
        }
    }

    public Operation Operation => operation;

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
                operation.Onboarding.Add("onboardingMethod","4");
                break;
            case OnboardingCloud:
                operation.Onboarding.Add("onboardingMethod","2");
                break;
            case OnboardingActivation:
                operation.Onboarding.Add("onboardingMethod","3");
                operation.Onboarding.Add("onboardingDetail",ActivationCode);
                break;
        }
        await deviceService.ScanPhoneAsync(operation);
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
    [Preference<string>("search-selected")]
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
            var results = await searchService.SearchAsync(query, searchCts.Token);
            if (string.IsNullOrEmpty(query) || !results.Any()) 
                SearchResults.Clear();
            else
                SearchResults = new ObservableCollection<SearchResult>(results.Order());
            SearchSelection = null;
        }
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

    [RelayCommand]
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
        operation.State = OperationState.InProgress;
        operation.Callback = AsyncCallback;
        await deviceService.ScanPhoneAsync(operation);
        if (operation.State == OperationState.Success)
        {
            appViewModel.Title = "Provisioned";
        }
        else
        {
            appViewModel.Title = "Provision";
        }

        async ValueTask<List<NdefRecord>> AsyncCallback(Operation op)
        {
            await Task.Delay(1000);
            return [];
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
            await deviceService.ScanPhoneAsync(operation);
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
        if(SelectedPhone != null) await popupService.ShowPopupAsync<MainViewModel>();
    }
    #endregion

    #region Start

    public string StartText => !settingsViewModel.Settings.User.IsLoggedIn && SelectedMode =="Search" ? "Login" : "Start";

    [RelayCommand]
    public async Task StartAsync()
    {
        await NextAsync(SelectedMode);
    }
    #endregion

}