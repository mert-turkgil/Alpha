using System.ComponentModel.DataAnnotations;

namespace Alpha.Models
{
public class ContactFormViewModel
{
    #nullable disable
    [Required(ErrorMessage = "Name is required.")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Subject is required.")]
    public string Subject { get; set; }

    [Required(ErrorMessage = "Message cannot be empty.")]
    public string Message { get; set; }
}
}