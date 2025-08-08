namespace DeviceFX.NfcApp.Model.Dto;

public class WebexIdentityOrganizationDto
{
    public string[] schemas { get; set; }
    public string displayName { get; set; }
    public string id { get; set; }
    public Meta meta { get; set; }
    public class Meta
    {
        public string created { get; set; }
        public string lastModified { get; set; }
        public string version { get; set; }
    }
}