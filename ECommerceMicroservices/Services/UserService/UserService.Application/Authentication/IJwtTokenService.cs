using UserService.Domain.Entities;

namespace UserService.Application.Authentication;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}