using DeviceFX.NfcApp.Helpers;
using DeviceFX.NfcApp.ViewModels;
using DeviceFX.NfcApp.Views.Shared;

namespace DeviceFX.NfcApp.Views;

public partial class OnboardingPage : StepContentPage
{
    public OnboardingPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is not MainViewModel viewModel) return;
        viewModel.WifiPasswordVisible = false;
        if (!string.IsNullOrWhiteSpace(viewModel.ActivationCode))
        {
            viewModel.OnboardingMode = MainViewModel.OnboardingActivation;
            await viewModel.SaveAsync(nameof(viewModel.OnboardingMode));
        }
    }

    // Reject typing or pasting past 16 digits (17 characters with a leading #) instead of
    // letting the view model trim the code, which would drop a digit the user didn't touch.
    private void OnActivationCodeTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is Entry entry && !ActivationCodeValidator.IsEditAllowed(e.OldTextValue, e.NewTextValue))
            entry.Text = e.OldTextValue;
    }
}