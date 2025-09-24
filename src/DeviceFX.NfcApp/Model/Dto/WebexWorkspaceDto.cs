namespace DeviceFX.NfcApp.Model.Dto;

public class WebexWorkspaceDto
{
    public string id { get; set; }
    public string orgId { get; set; }
    public string workspaceLocationId { get; set; }
    public string locationId { get; set; }
    public string displayName { get; set; }
    public string sipAddress { get; set; }
    public string created { get; set; }
    public Calling calling { get; set; }
    public Calendar calendar { get; set; }
    public string hotdeskingStatus { get; set; }
    public DeviceHostedMeetings deviceHostedMeetings { get; set; }
    public string supportedDevices { get; set; }
    public string devicePlatform { get; set; }
    public PlannedMaintenance plannedMaintenance { get; set; }
    public Health health { get; set; }
    public Devices[] devices { get; set; }
    public int capacity { get; set; }
    public string type { get; set; }

    public class Calling
    {
        public string type { get; set; }
        public WebexCalling webexCalling { get; set; }
    }

    public class WebexCalling
    {
        public string[] licenses { get; set; }
    }

    public class Calendar
    {
        public string type { get; set; }
    }

    public class DeviceHostedMeetings
    {
        public bool enabled { get; set; }
    }

    public class PlannedMaintenance
    {
        public string mode { get; set; }
    }

    public class Health
    {
        public Issues[] issues { get; set; }
        public string level { get; set; }
    }

    public class Issues
    {
        public string id { get; set; }
        public string createdAt { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string recommendedAction { get; set; }
        public string level { get; set; }
    }

    public class Devices
    {
        public string id { get; set; }
        public string callingDeviceId { get; set; }
        public string webexDeviceId { get; set; }
        public string displayName { get; set; }
        public string placeId { get; set; }
        public string orgId { get; set; }
        public object[] capabilities { get; set; }
        public object[] permissions { get; set; }
        public string product { get; set; }
        public string type { get; set; }
        public object[] tags { get; set; }
        public string ip { get; set; }
        public string mac { get; set; }
        public string serial { get; set; }
        public string activeInterface { get; set; }
        public string software { get; set; }
        public string primarySipUrl { get; set; }
        public string[] sipUrls { get; set; }
        public string created { get; set; }
        public string firstSeen { get; set; }
        public string lastSeen { get; set; }
        public string workspaceLocationId { get; set; }
        public string locationId { get; set; }
        public string managedBy { get; set; }
        public string devicePlatform { get; set; }
        public string lifecycle { get; set; }
        public string plannedMaintenance { get; set; }
        public string workspaceId { get; set; }
        public string upgradeChannel { get; set; }
    }
}
