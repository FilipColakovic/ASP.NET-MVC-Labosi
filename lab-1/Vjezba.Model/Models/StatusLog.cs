using Vjezba.Model.Enums;

namespace Vjezba.Model.Models
{
    public class StatusLog
    {
        public int Id { get; set; }
        public DateTime TimeChanged { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public PackageStatus PreviousStatus { get; set; }
        public PackageStatus NewStatus { get; set; }
        public Package Package { get; set; }
    }
}
