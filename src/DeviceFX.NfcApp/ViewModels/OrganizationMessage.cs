using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DeviceFX.NfcApp.ViewModels;

public class OrganizationMessage : ValueChangedMessage<string>
{
    public OrganizationMessage(string value) : base(value)
    {
    }
}