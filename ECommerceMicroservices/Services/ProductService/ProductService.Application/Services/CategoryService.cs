using ProductService.Application.DTOs;
using ProductService.Application.Repositories;
using ProductService.Domain.Entities;

namespace ProductService.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();

        return categories.Select(MapToDto).ToList();
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);

        return category == null ? null : MapToDto(category);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Category name is required.");

        var category = new Category
        {
            Name = dto.Name.Trim()
        };

        await _categoryRepository.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(category);
    }

    public async Task<bool> UpdateAsync(
        int id,
        UpdateCategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Category name is required.");

        var category = await _categoryRepository.GetByIdAsync(id);

        if (category == null)
            return false;

        category.Name = dto.Name.Trim();

        await _categoryRepository.UpdateAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);

        if (category == null)
            return false;

        await _categoryRepository.DeleteAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    private static CategoryDto MapToDto(Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name
        };
    }
}