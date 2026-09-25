using UserService.Application.DTOs;

namespace UserService.Application.Services;

public interface IUserService
{
    Task<UserDto> RegisterAsync(RegisterUserDto dto);

    Task<string> LoginAsync(LoginUserDto dto);

    Task<UserDto?> GetByIdAsync(int id);
}