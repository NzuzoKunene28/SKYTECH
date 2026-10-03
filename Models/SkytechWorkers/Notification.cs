using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkyTech.Models.SkytechWorkers
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [ForeignKey(nameof(Staff))]
        public int StaffId { get; set; }

        [ValidateNever]
        public virtual Staff Staff { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; }

        [Required]
        [MaxLength(500)]
        public string Message { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}