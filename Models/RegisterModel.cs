using System.ComponentModel.DataAnnotations;

namespace Alpha.Models
{
    public class RegisterModel
    {
        #nullable disable
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Display(Name = "UserName")]
        public string UserName { get; set; }

        [Required]
        [Display(Name = "FirstName")]
        [StringLength(100, ErrorMessage ="Çvery long, no more than 100 letters ")]

        public string FirstName { get; set; }
        [Required]
        [Display(Name = "LastName")]
        [StringLength(100, ErrorMessage ="Çvery long, no more than 100 letters ")]
        public string LastName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 8)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; }
    }
}
