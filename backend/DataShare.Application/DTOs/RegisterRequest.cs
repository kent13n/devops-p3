using System.ComponentModel.DataAnnotations;

namespace DataShare.Application.DTOs;

public record RegisterRequest
{
    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    [MaxLength(256)]
    public string Email { get; init; } = default!;

    [Required(ErrorMessage = "Le mot de passe est requis")]
    [MinLength(8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères")]
    [MaxLength(128)]
    public string Password { get; init; } = default!;
}
