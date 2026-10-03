using System.ComponentModel.DataAnnotations;

namespace SkyTech.Services
{
    public class PublicHoliday
    {
        [Key]
        public int PublicHolidayId { get; set; }

        [Required]
        [MaxLength(100)]
        public string HolidayName { get; set; } = string.Empty;

        [Required]
        public DateTime HolidayDate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}