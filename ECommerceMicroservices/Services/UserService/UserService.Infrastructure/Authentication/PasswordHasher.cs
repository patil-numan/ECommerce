using Microsoft.AspNetCore.Identity;
using UserService.Application.Authentication;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Authentication;

public class PasswordHasher : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<User> _passwordHasher = new();

    public string HashPassword(string password)
    {
        var user = new User();

        return _passwordHasher.HashPassword(user, password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        var user = new User();

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            passwordHash,
            password);

        return result == PasswordVerificationResult.Success ||
               result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}