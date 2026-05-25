using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vjezba.Model.Models
{
    public class Attachment
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Package))]
        public int PackageId { get; set; }

        public virtual Package Package { get; set; } = null!;

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        public string ContentType { get; set; } = string.Empty;

        public long FileSize { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
