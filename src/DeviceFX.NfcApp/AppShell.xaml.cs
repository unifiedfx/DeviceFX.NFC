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
            if(page.BindingContext is IQueryAttributable qt) qt.ApplyQueryAttributes(ParseQueryString(args.Target.Location.OriginalString));
        });
    }

    private Dictionary<string, object> ParseQueryString(string query)
    {
        var queryParams = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
        if (string.IsNullOrEmpty(query)) return queryParams;
        var index = query.LastIndexOf('?') + 1;
        var pairs = query[index..].Split('&');
        foreach (var pair in pairs)
        {
            var keyValue = pair.Split('=');
            if (keyValue.Length != 2) continue;
            if (int.TryParse(keyValue[1], out var intResult)) queryParams[keyValue[0]] = intResult;
            else if (double.TryParse(keyValue[1], out var doubleResult)) queryParams[keyValue[0]] = doubleResult;
            else if (bool.TryParse(keyValue[1], out var boolResult)) queryParams[keyValue[0]] = boolResult;
            else if (DateTime.TryParse(keyValue[1], out var dateTimeResult)) queryParams[keyValue[0]] = dateTimeResult;
            else queryParams[keyValue[0]] = keyValue[1];
        }
        return queryParams;
    }
}