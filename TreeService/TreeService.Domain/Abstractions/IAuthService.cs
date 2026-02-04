using TreeService.Contracts.Auth;
using TreeService.Persistence.SQLite.Entities;

namespace TreeService.Domain.Abstractions;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<Guid> RegisterAsync(LoginRequest request, string role = "User");
    string GenerateJwtToken(User user);
}