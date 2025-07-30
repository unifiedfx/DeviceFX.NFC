using System.Windows.Input;

namespace DeviceFX.NfcApp.Views.Shared;

public partial class LabelCheckboxView : ContentView
{
    public static readonly BindableProperty IsToggledProperty = BindableProperty.Create(nameof(IsToggled), typeof(bool), typeof(LabelCheckboxView), false, BindingMode.TwoWay);
    public static readonly BindableProperty LabelTextProperty = BindableProperty.Create(nameof(LabelText), typeof(string), typeof(LabelCheckboxView), default(string), BindingMode.TwoWay);
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(LabelCheckboxView));
    

    public bool IsToggled
    {
        get { return (bool) GetValue(IsToggledProperty); }
        set
        {
            SetValue(IsToggledProperty, value);
            Command?.Execute(value);
        }
    }
    public string LabelText
    {
        get { return (string) GetValue(LabelTextProperty); }
        set { SetValue(LabelTextProperty, value); }
    }
    public ICommand? Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }    
    public LabelCheckboxView()
    {
        InitializeComponent();
    }
}