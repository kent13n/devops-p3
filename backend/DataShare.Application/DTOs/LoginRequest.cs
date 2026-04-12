using System.ComponentModel.DataAnnotations;

namespace DataShare.Application.DTOs;

public record LoginRequest
{
    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string Email { get; init; } = default!;

    [Required(ErrorMessage = "Le mot de passe est requis")]
    public string Password { get; init; } = default!;
}
