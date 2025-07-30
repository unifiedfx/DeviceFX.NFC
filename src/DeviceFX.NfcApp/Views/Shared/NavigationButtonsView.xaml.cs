using System.Windows.Input;

namespace DeviceFX.NfcApp.Views.Shared;

public partial class NavigationButtonsView : ContentView
{
    public static readonly BindableProperty BackButtonTextProperty = BindableProperty.Create(nameof(BackButtonText), typeof(string), typeof(NavigationButtonsView), "Back", BindingMode.TwoWay);
    public static readonly BindableProperty NextButtonTextProperty = BindableProperty.Create(nameof(NextButtonText), typeof(string), typeof(NavigationButtonsView), "Next", BindingMode.TwoWay);
    public static readonly BindableProperty NextButtonImageSourceProperty = BindableProperty.Create(nameof(NextButtonImageSource), typeof(string), typeof(NavigationButtonsView), "", BindingMode.TwoWay);

    public static readonly BindableProperty NextCommandProperty = BindableProperty.Create(nameof(NextCommand), typeof(ICommand), typeof(NavigationButtonsView), null, BindingMode.TwoWay);
    public static readonly BindableProperty BackCommandProperty = BindableProperty.Create(nameof(BackCommand), typeof(ICommand), typeof(NavigationButtonsView), null, BindingMode.TwoWay);

    public static readonly BindableProperty NextParameterProperty = BindableProperty.Create(nameof(NextParameter), typeof(object), typeof(NavigationButtonsView), null, BindingMode.TwoWay);
    public static readonly BindableProperty BackParameterProperty = BindableProperty.Create(nameof(BackParameter), typeof(object), typeof(NavigationButtonsView), null, BindingMode.TwoWay);

    
    public ICommand NextCommand
    {
        get => (ICommand)GetValue(NextCommandProperty);
        set => SetValue(NextCommandProperty, value);
    }
    public ICommand BackCommand
    {
        get => (ICommand)GetValue(BackCommandProperty);
        set => SetValue(BackCommandProperty, value);
    }

    public object NextParameter
    {
        get => GetValue(NextParameterProperty);
        set => SetValue(NextParameterProperty, value);
    }

    public object BackParameter
    {
        get => GetValue(BackParameterProperty);
        set => SetValue(BackParameterProperty, value);
    }

    
    
    public string BackButtonText
    {
        get { return (string) GetValue(BackButtonTextProperty); }
        set { SetValue(BackButtonTextProperty, value); }
    }
    public string NextButtonText
    {
        get { return (string) GetValue(NextButtonTextProperty); }
        set { SetValue(NextButtonTextProperty, value); }
    }
    public string NextButtonImageSource
    {
        get { return (string) GetValue(NextButtonImageSourceProperty); }
        set { SetValue(NextButtonImageSourceProperty, value); }
    }
    public NavigationButtonsView()
    {
        InitializeComponent();
    }
}