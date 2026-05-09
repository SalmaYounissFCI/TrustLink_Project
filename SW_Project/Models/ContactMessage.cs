using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SW_Project.Models
{
    public class ContactMessage
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Subject is required.")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Message cannot be empty.")]
        public string Message { get; set; }

        public bool AgreePrivacy { get; set; }

        public DateTime SentAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;
        public string AdminId { get; set; }

        [ForeignKey("AdminId")]
        public ApplicationUser Admin { get; set; }
    }
}