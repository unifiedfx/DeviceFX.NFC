using System.Text.Json.Serialization;

namespace DeviceFX.NfcApp.Model.Dto;

public class WebexIdentityUserDto
{
    public string[] schemas { get; set; }
    public string id { get; set; }
    public string userName { get; set; }
    public bool active { get; set; }
    public Name name { get; set; }
    public string displayName { get; set; }
    public Emails[] emails { get; set; }
    public string userType { get; set; }
    public string externalId { get; set; }
    public PhoneNumbers[] phoneNumbers { get; set; }
    public Photos[]? photos { get; set; }
    [JsonPropertyName("urn:scim:schemas:extension:cisco:webexidentity:2.0:User")]
    public Webex webex { get; set; }
    public Meta meta { get; set; }
    public string? Picture => photos?.OrderByDescending(p => p.type == "photo").Select(p => p.value).FirstOrDefault();
    public class Name
    {
        public string familyName { get; set; }
        public string givenName { get; set; }
    }

    public class Emails
    {
        public string value { get; set; }
        public string type { get; set; }
        public bool primary { get; set; }
    }

    public class PhoneNumbers
    {
        public string value { get; set; }
        public string type { get; set; }
        public bool primary { get; set; }
    }

    public class Photos
    {
        public string value { get; set; }
        public string type { get; set; }
    }

    public class Webex
    {
        public string[] accountStatus { get; set; }
        [JsonPropertyName("meta")]
        public Organization organization { get; set; }
        public string userNameType { get; set; }
    }

    public class Organization
    {
        public string organizationId { get; set; }
        public string name { get; set; }

    }

    public class Meta
    {
        public string resourceType { get; set; }
        public string location { get; set; }
        public string version { get; set; }
        public string created { get; set; }
        public string lastModified { get; set; }
    }
}