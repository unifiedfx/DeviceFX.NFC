namespace DeviceFX.NfcApp.Helpers;

public static class VisualElementExtensions
{
    public static async Task RotateAsync(this VisualElement visualElement, CancellationToken cancellationToken)
    {
        cancellationToken.Register(() => 
        {
            visualElement.CancelAnimations();
            visualElement.Rotation = 0;
        });
        try
        {
            var step = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                await visualElement.RotateTo(++step * 90);
                if (step != 4) continue;
                step = 0;
                visualElement.Rotation = 0; // Reset to 0 for continuous loop
            }
        }
        catch (TaskCanceledException)
        {
            // Animation canceled
        }        
    }
}