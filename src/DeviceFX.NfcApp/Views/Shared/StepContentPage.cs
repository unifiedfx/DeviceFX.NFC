namespace DeviceFX.NfcApp.Views.Shared;

public abstract class StepContentPage : ContentPage
{
    public string Name => GetType().Name.Replace("Page", string.Empty).ToLowerInvariant();
    public int Priority { get; set; } = 0;
    public string? Group { get; set; }
}