using Domain.Common;

namespace Domain.Features.Users;

public class User : Entity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Idp { get; set; }
    public string ExternalId { get; set; } = string.Empty;

    public static User Create(
        string firstName,
        string lastName,
        string email,
        Guid createdBy)

    {
        return new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            CreatedBy = createdBy
        };
    }

    public void Update(
        string firstName,
        string lastName,
        Guid updatedBy
    )
    {
        FirstName = firstName;
        LastName = lastName;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }
}
