namespace Application.Abstractions.Identity;

public interface IAdminSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
