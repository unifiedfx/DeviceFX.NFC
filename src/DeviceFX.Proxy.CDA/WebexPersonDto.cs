namespace DeviceFX.Proxy.CDA;

public record WebexPersonDto(
    string id,
    string[] emails,
    WebexPersonDto.PhoneNumbers[] phoneNumbers,
    WebexPersonDto.SipAddresses[] sipAddresses,
    string displayName,
    string nickName,
    string firstName,
    string lastName,
    string avatar,
    string orgId,
    string[] roles,
    string[] licenses,
    string created,
    string lastModified,
    string lastActivity,
    string status,
    bool invitePending,
    bool loginEnabled,
    string type,
    string[] siteUrls
)
{
    public record PhoneNumbers(
        string type,
        string value,
        bool primary
    );

    public record SipAddresses(
        string type,
        string value,
        bool primary
    );
}