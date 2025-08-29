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
        if (!string.IsNullOrWhiteSpace(viewModel.ActivationCode))
        {
            viewModel.OnboardingMode = MainViewModel.OnboardingActivation;
            await viewModel.SaveAsync(nameof(viewModel.OnboardingMode));
        }
    }
}