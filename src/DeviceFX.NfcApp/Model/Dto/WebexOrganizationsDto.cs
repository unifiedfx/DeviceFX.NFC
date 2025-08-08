using System.Text.Json.Serialization;

namespace DeviceFX.NfcApp.Model.Dto;

public class WebexOrganizationsDto
{
    [JsonPropertyName("items")]
    public WebexOrganization[] organizations { get; set; }

}

public class WebexOrganization
{
    // ciscospark://us/ORGANIZATION/0f60d632-8604-4203-bb21-21a1ec7649ec
    // userName co "st" or displayName co "st" or emails co "st"
    //ity:2.0:User:meta.organizationId eq "0ae87ade-8c8a-4952-af08-318798958d0c"
    public string id { get; set; }
    public string displayName { get; set; }
    public DateTime created { get; set; }
}

// public class RootObject
// {
//     public string[] schemas { get; set; }
//     public int totalResults { get; set; }
//     public int itemsPerPage { get; set; }
//     public int startIndex { get; set; }
//     public Resources[] Resources { get; set; }
// }
//
// public class Resources
// {
//     public string[] schemas { get; set; }
//     public string id { get; set; }
//     public string userName { get; set; }
//     public bool active { get; set; }
//     public Name name { get; set; }
//     public string displayName { get; set; }
//     public Emails[] emails { get; set; }
//     public string userType { get; set; }
//     public string externalId { get; set; }
//     public Urn_scim_schemas_extension_cisco_webexidentity_2_0_User urn_scim_schemas_extension_cisco_webexidentity_2_0_User { get; set; }
//     public Meta meta { get; set; }
//     public string timezone { get; set; }
//     public Photos[] photos { get; set; }
//     public Addresses[] addresses { get; set; }
//     public PhoneNumbers[] phoneNumbers { get; set; }
//     public string locale { get; set; }
// }
//
// public class Name
// {
//     public string familyName { get; set; }
//     public string givenName { get; set; }
// }
//
// public class Emails
// {
//     public string value { get; set; }
//     public string type { get; set; }
//     public bool primary { get; set; }
// }
//
// public class Urn_scim_schemas_extension_cisco_webexidentity_2_0_User
// {
//     public string[] accountStatus { get; set; }
//     public Meta1 meta { get; set; }
//     public string userNameType { get; set; }
// }
//
// public class Meta1
// {
//     public string organizationId { get; set; }
// }
//
// public class Meta
// {
//     public string resourceType { get; set; }
//     public string location { get; set; }
//     public string version { get; set; }
//     public string created { get; set; }
//     public string lastModified { get; set; }
// }
//
// public class Photos
// {
//     public string value { get; set; }
//     public string type { get; set; }
// }
//
// public class Addresses
// {
//     public string type { get; set; }
//     public string streetAddress { get; set; }
//     public string locality { get; set; }
//     public string region { get; set; }
//     public string postalCode { get; set; }
//     public string country { get; set; }
// }
//
