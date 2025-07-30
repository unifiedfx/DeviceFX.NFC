using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Helpers.Preference;
using DeviceFX.NfcApp.Model;
using DeviceFX.NfcApp.Views.Shared;

namespace DeviceFX.NfcApp.ViewModels;

public partial class MainViewModel(Operation operation, IEnumerable<StepContentPage> steps, ISearchService searchService, IDeviceService deviceService, IInventoryService inventoryService) : WizardViewModelBase(steps)
{
    public const string OnboardingCucm = "CUCM";
    public const string OnboardingCloud = "Cloud";
    public const string OnboardingActivation = "Activation";
    public const string SelectedProvision = "Search";
    public const string SelectedOnboarding = "Onboarding";
    public const string SelectedInventory = "Inventory";

    [ObservableProperty]
    [Preference<string>("selected-mode", SelectedProvision)]
    private string selectedMode = SelectedProvision;
    
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
        operation.Onboarding.Clear();
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
    public async Task SelectedAsync() => await NextAsync("provision");

    public bool CanExecuteSelected() => SearchSelection != null;
    #endregion

    #region Provision

    [RelayCommand]
    public async Task ProvisionAsync()
    {
        // Start NFC Read then provision to Webex
        operation.Onboarding.Clear();
        await deviceService.ScanPhoneAsync(operation);
    }
    #endregion

    #region Inventory

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShareCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearCommand))]
    private ObservableCollection<PhoneDetails> phoneList = [];
    
    public async Task LoadPhonesAsync()
    {
        PhoneList = new ObservableCollection<PhoneDetails>(await inventoryService.GetPhonesAsync());
    }
    
    [RelayCommand]
    public async Task ScanAsync()
    {
        try
        {
            operation.Onboarding.Clear();
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

    #endregion
}