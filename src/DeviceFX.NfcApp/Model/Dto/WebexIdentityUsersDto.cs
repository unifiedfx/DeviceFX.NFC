using System.Text.Json.Serialization;

namespace DeviceFX.NfcApp.Model.Dto;

public class WebexIdentityUsersDto
{
    public string[] schemas { get; set; }
    public int totalResults { get; set; }
    public int itemsPerPage { get; set; }
    public int startIndex { get; set; }
    public ResourcesItem[] Resources { get; set; }

    public class ResourcesItem
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

        [JsonPropertyName("urn:scim:schemas:extension:cisco:webexidentity:2.0:User")]
        public WebexIdentity webex { get; set; }

        public Meta meta { get; set; }
        public Photos[] photos { get; set; }
        public Addresses[] addresses { get; set; }
        public PhoneNumbers[] phoneNumbers { get; set; }
        public string locale { get; set; }
    }

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

    public class WebexIdentity
    {
        public string[] accountStatus { get; set; }
        public Meta1 meta { get; set; }
        public string userNameType { get; set; }
    }

    public class Meta1
    {
        public string organizationId { get; set; }
    }

    public class Meta
    {
        public string resourceType { get; set; }
        public string location { get; set; }
        public string version { get; set; }
        public string created { get; set; }
        public string lastModified { get; set; }
    }

    public class Photos
    {
        public string value { get; set; }
        public string type { get; set; }
    }
    public class Addresses
    {
        public string type { get; set; }
        public string streetAddress { get; set; }
        public string locality { get; set; }
        public string region { get; set; }
        public string postalCode { get; set; }
        public string country { get; set; }
    }
    
    public class PhoneNumbers
    {
        public string value { get; set; }
        public string type { get; set; }
        public bool primary { get; set; }
    }
}