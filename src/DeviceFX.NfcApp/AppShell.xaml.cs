using System.ComponentModel;
using DeviceFX.NfcApp.Helpers;
using DeviceFX.NfcApp.ViewModels;
using DeviceFX.NfcApp.Views;
using DeviceFX.NfcApp.Views.Shared;

namespace DeviceFX.NfcApp;

public partial class AppShell : Shell
{
    private readonly IServiceProvider serviceProvider;
    private readonly Dictionary<string,Type> modalRoutes = new(StringComparer.InvariantCultureIgnoreCase)
    {
        { "Settings", typeof(SettingsPage) }
    };

    public AppShell(WizardViewModelBase wizardViewModel, IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        BindingContext = wizardViewModel;
        foreach (var page in wizardViewModel.Steps )
        {
            Items.Add(new ShellContent
            {
                Title = page.Title,
                Route = page.Name,
                ContentTemplate = new DataTemplate(page.GetType())
            });
        }
        foreach (var route in modalRoutes)
        {
            Items.Add(new ShellContent
            {
                Title = route.Key,
                Route = route.Key.ToLowerInvariant(),
                ContentTemplate = new DataTemplate(route.Value)
            });
        }
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var shellTitleView = serviceProvider.GetService<ShellTitleView>();
        SetTitleView(this, shellTitleView);
        SetNavBarHasShadow(this, false);
    }

    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);
        var appViewModel = serviceProvider.GetService<AppViewModel>();
        appViewModel.Title =Current?.CurrentItem.CurrentItem.CurrentItem.Title ?? Current?.CurrentPage.Title;
        if (Current?.CurrentPage is Page {BindingContext: INotifyPropertyChanged viewModel})
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await viewModel.LoadAsync();
            });
        }
    }

    protected override void OnNavigating(ShellNavigatingEventArgs args)
    {
        base.OnNavigating(args);
        if(args.Source == ShellNavigationSource.Push) return;
        var route = modalRoutes.FirstOrDefault(r => args.Target.Location.OriginalString.Contains(r.Key, StringComparison.InvariantCultureIgnoreCase));
        if(route.Key == null) return;
        if (serviceProvider.GetService(route.Value) is not Page page) return;
        args.Cancel(); // Cancel the navigation
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if(Navigation.ModalStack.LastOrDefault() != page) await Navigation.PushModalAsync(page, true);
        });
    }
}