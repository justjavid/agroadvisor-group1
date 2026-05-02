using System.ComponentModel.DataAnnotations;

namespace Service.DTOs.AuthDTOs;

public class RegisterRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [MinLength(2, ErrorMessage = "Name must be at least 2 characters.")]
    [MaxLength(50, ErrorMessage = "Name cannot exceed 50 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Surname is required.")]
    [MinLength(2, ErrorMessage = "Surname must be at least 2 characters.")]
    [MaxLength(50, ErrorMessage = "Surname cannot exceed 50 characters.")]
    public string Surname { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email is not valid.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Password must contain uppercase, lowercase, and a number.")]
    public string Password { get; set; } = string.Empty;
}
