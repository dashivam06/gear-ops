namespace gearOps.Application.DTOs;

public class UpdateProfileDto
{
    public string FullName { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string? Address { get; set; }
    public string? ProfileImageUrl { get; set; }
}

public class ProfileResponseDto
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string? Address { get; set; }
    public string? ProfileImageUrl { get; set; }
    public decimal CreditsRemaining { get; set; }
}
