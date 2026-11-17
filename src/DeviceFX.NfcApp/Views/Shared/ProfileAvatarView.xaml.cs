namespace DeviceFX.NfcApp.Views.Shared;

public partial class ProfileAvatarView : ContentView
{
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(
        nameof(Size), typeof(double), typeof(ProfileAvatarView), 40d,
        propertyChanged: OnSizeChanged);

    public static readonly BindableProperty CircleColorProperty = BindableProperty.Create(
        nameof(CircleColor), typeof(Color), typeof(ProfileAvatarView), Colors.Gray);

    public static readonly BindableProperty InitialsColorProperty = BindableProperty.Create(
        nameof(InitialsColor), typeof(Color), typeof(ProfileAvatarView), Colors.White);

    public ProfileAvatarView() => InitializeComponent();

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public Color CircleColor
    {
        get => (Color)GetValue(CircleColorProperty);
        set => SetValue(CircleColorProperty, value);
    }

    public Color InitialsColor
    {
        get => (Color)GetValue(InitialsColorProperty);
        set => SetValue(InitialsColorProperty, value);
    }

    public double InitialsFontSize => Size * 0.4;

    private static void OnSizeChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ProfileAvatarView)bindable).OnPropertyChanged(nameof(InitialsFontSize));
}
