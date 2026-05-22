namespace Vjezba.Model.Models
{
    public class PackageTrackingPageViewModel
    {
        public string TrackingNumber { get; set; } = string.Empty;
        public bool LookupAttempted { get; set; }
        public string? ErrorMessage { get; set; }
        public Package? Package { get; set; }
        public IReadOnlyList<StatusLog> StatusHistory { get; set; } = Array.Empty<StatusLog>();
    }
}
