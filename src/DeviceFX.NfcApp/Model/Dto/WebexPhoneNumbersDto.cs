namespace DeviceFX.NfcApp.Model.Dto;

public class WebexPhoneNumbersDto
{
    public PhoneNumbers[] phoneNumbers { get; set; }
    public class PhoneNumbers
    {
        public string extension { get; set; }
        public string esn { get; set; }
        public bool mainNumber { get; set; }
        public bool tollFreeNumber { get; set; }
        public bool isServiceNumber { get; set; }
        public Location location { get; set; }
        public Owner owner { get; set; }
    }

    public class Location
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class Owner
    {
        public string id { get; set; }
        public string type { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
    }
}



