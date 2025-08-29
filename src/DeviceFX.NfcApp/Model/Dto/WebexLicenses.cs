namespace DeviceFX.NfcApp.Model.Dto;

public class WebexLicensesDto
{
    public WebexLicenseDto[] items { get; set; }
}

public class WebexLicenseDto
{
    public string id { get; set; }
    public string name { get; set; }
    public int totalUnits { get; set; }
    public int consumedUnits { get; set; }
    public int consumedByUsers { get; set; }
    public int consumedByWorkspaces { get; set; }
    public string subscriptionId { get; set; }
    public string siteUrl { get; set; }
    public string siteType { get; set; }
}


public class WebexPeopleDto
{
    public object notFoundIds { get; set; }
    public WebexPersonDto[] items { get; set; }
}

public class WebexPersonDto
{
    public string id { get; set; }
    public string[] emails { get; set; }
    public SipAddresses[] sipAddresses { get; set; }
    public string displayName { get; set; }
    public string nickName { get; set; }
    public string firstName { get; set; }
    public string lastName { get; set; }
    public string orgId { get; set; }
    public string[] roles { get; set; }
    public string[] licenses { get; set; }
    public string created { get; set; }
    public string lastModified { get; set; }
    public string status { get; set; }
    public bool invitePending { get; set; }
    public bool loginEnabled { get; set; }
    public string type { get; set; }
    public string[] siteUrls { get; set; }
    public string avatar { get; set; }
    public string timeZone { get; set; }
    public string lastActivity { get; set; }
    public Addresses[] addresses { get; set; }
    public PhoneNumbers[] phoneNumbers { get; set; }
    public string extension { get; set; }
    public string locationId { get; set; }
    public class SipAddresses
    {
        public string type { get; set; }
        public string value { get; set; }
        public bool primary { get; set; }
    }
    public class Addresses
    {
        public string country { get; set; }
        public string locality { get; set; }
        public string region { get; set; }
        public string streetAddress { get; set; }
        public string type { get; set; }
        public string postalCode { get; set; }
    }

    public class PhoneNumbers
    {
        public string type { get; set; }
        public string value { get; set; }
        public bool primary { get; set; }
    }   
}

