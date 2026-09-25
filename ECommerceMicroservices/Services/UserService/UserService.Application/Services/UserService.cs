using UserService.Application.Authentication;
using UserService.Application.DTOs;
using UserService.Application.Repositories;
using UserService.Domain.Entities;

namespace UserService.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public UserService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<UserDto> RegisterAsync(RegisterUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            throw new ArgumentException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            throw new ArgumentException("Password is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.FirstName))
        {
            throw new ArgumentException("First name is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.LastName))
        {
            throw new ArgumentException("Last name is required.");
        }

        var email = dto.Email.Trim().ToLowerInvariant();

        var existingUser = await _userRepository.GetByEmailAsync(email);

        if (existingUser != null)
        {
            throw new InvalidOperationException(
                "A user with this email already exists.");
        }

        var now = DateTime.UtcNow;

        var user = new User
        {
            Email = email,
            PasswordHash = _passwordHasher.HashPassword(dto.Password),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(user);
    }

    public async Task<string> LoginAsync(LoginUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            throw new ArgumentException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            throw new ArgumentException("Password is required.");
        }

        var email = dto.Email.Trim().ToLowerInvariant();

        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        var passwordValid = _passwordHasher.VerifyPassword(
            dto.Password,
            user.PasswordHash);

        if (!passwordValid)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        return _jwtTokenService.GenerateToken(user);
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);

        if (user == null)
        {
            return null;
        }

        return MapToDto(user);
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}