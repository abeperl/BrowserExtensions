using DataFlow.Mobile.Models;
using DataFlow.Mobile.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataFlow.Mobile.Services;

public class ColorSchemeService : IColorSchemeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ColorSchemeService> _logger;

    public ColorSchemeService(IUnitOfWork unitOfWork, ILogger<ColorSchemeService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<ColorScheme>> GetAllColorSchemesAsync()
    {
        try
        {
            var repository = _unitOfWork.GetRepository<ColorScheme>();
            return await repository.GetAllAsync(orderBy: q => q.OrderBy(cs => cs.IsBuiltIn ? 0 : 1).ThenBy(cs => cs.Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving color schemes");
            return [];
        }
    }

    public async Task<ColorScheme?> GetColorSchemeByIdAsync(int id)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<ColorScheme>();
            return await repository.GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving color scheme with ID {Id}", id);
            return null;
        }
    }

    public async Task<List<ColorScheme>> GetBuiltInColorSchemesAsync()
    {
        try
        {
            var repository = _unitOfWork.GetRepository<ColorScheme>();
            return await repository.GetAsync(cs => cs.IsBuiltIn, orderBy: q => q.OrderBy(cs => cs.Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving built-in color schemes");
            return [];
        }
    }

    public async Task<ColorScheme> CreateColorSchemeAsync(ColorScheme colorScheme)
    {
        try
        {
            colorScheme.IsBuiltIn = false;
            colorScheme.CreatedAt = DateTime.UtcNow;
            colorScheme.UpdatedAt = DateTime.UtcNow;

            var repository = _unitOfWork.GetRepository<ColorScheme>();
            await repository.AddAsync(colorScheme);
            await _unitOfWork.SaveChangesAsync();

            return colorScheme;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating color scheme");
            throw;
        }
    }

    public async Task<ColorScheme> UpdateColorSchemeAsync(ColorScheme colorScheme)
    {
        try
        {
            if (colorScheme.IsBuiltIn)
            {
                throw new InvalidOperationException("Cannot modify built-in color schemes");
            }

            colorScheme.UpdatedAt = DateTime.UtcNow;

            var repository = _unitOfWork.GetRepository<ColorScheme>();
            repository.Update(colorScheme);
            await _unitOfWork.SaveChangesAsync();

            return colorScheme;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating color scheme with ID {Id}", colorScheme.Id);
            throw;
        }
    }

    public async Task<bool> DeleteColorSchemeAsync(int id)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<ColorScheme>();
            var colorScheme = await repository.GetByIdAsync(id);

            if (colorScheme == null)
                return false;

            if (colorScheme.IsBuiltIn)
            {
                throw new InvalidOperationException("Cannot delete built-in color schemes");
            }

            // Check if any templates are using this color scheme
            var templateRepository = _unitOfWork.GetRepository<Template>();
            var templatesUsingScheme = await templateRepository.GetAsync(t => t.ColorSchemeId == id);

            if (templatesUsingScheme.Any())
            {
                throw new InvalidOperationException("Cannot delete color scheme that is in use by templates");
            }

            repository.Delete(colorScheme);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting color scheme with ID {Id}", id);
            throw;
        }
    }

    public async Task<ColorScheme> DuplicateColorSchemeAsync(int id, string newName)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<ColorScheme>();
            var originalScheme = await repository.GetByIdAsync(id);

            if (originalScheme == null)
                throw new ArgumentException($"Color scheme with ID {id} not found");

            var duplicatedScheme = new ColorScheme
            {
                Name = newName,
                Description = $"Copy of {originalScheme.Name}",
                PrimaryColor = originalScheme.PrimaryColor,
                SecondaryColor = originalScheme.SecondaryColor,
                BackgroundColor = originalScheme.BackgroundColor,
                SurfaceColor = originalScheme.SurfaceColor,
                TextColor = originalScheme.TextColor,
                TextSecondaryColor = originalScheme.TextSecondaryColor,
                BorderColor = originalScheme.BorderColor,
                SuccessColor = originalScheme.SuccessColor,
                WarningColor = originalScheme.WarningColor,
                ErrorColor = originalScheme.ErrorColor,
                InfoColor = originalScheme.InfoColor,
                IsBuiltIn = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await repository.AddAsync(duplicatedScheme);
            await _unitOfWork.SaveChangesAsync();

            return duplicatedScheme;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating color scheme with ID {Id}", id);
            throw;
        }
    }

    public async Task<ColorScheme> GetDefaultColorSchemeAsync()
    {
        try
        {
            var repository = _unitOfWork.GetRepository<ColorScheme>();
            var defaultScheme = await repository.GetAsync(cs => cs.IsBuiltIn && cs.Name == "Light Theme");

            return defaultScheme.FirstOrDefault() ?? ColorScheme.DefaultLight;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving default color scheme");
            return ColorScheme.DefaultLight;
        }
    }

    public async Task InitializeBuiltInColorSchemesAsync()
    {
        try
        {
            var repository = _unitOfWork.GetRepository<ColorScheme>();
            var existingBuiltIns = await repository.GetAsync(cs => cs.IsBuiltIn);

            // Add default light theme if it doesn't exist
            if (!existingBuiltIns.Any(cs => cs.Name == "Light Theme"))
            {
                var lightTheme = ColorScheme.DefaultLight;
                await repository.AddAsync(lightTheme);
            }

            // Add default dark theme if it doesn't exist
            if (!existingBuiltIns.Any(cs => cs.Name == "Dark Theme"))
            {
                var darkTheme = ColorScheme.DefaultDark;
                await repository.AddAsync(darkTheme);
            }

            // Add additional built-in schemes
            if (!existingBuiltIns.Any(cs => cs.Name == "Ocean Blue"))
            {
                var oceanBlue = new ColorScheme
                {
                    Name = "Ocean Blue",
                    Description = "Cool ocean-inspired blue theme",
                    PrimaryColor = "#0077BE",
                    SecondaryColor = "#4A90A4",
                    BackgroundColor = "#F0F8FF",
                    SurfaceColor = "#E6F3FF",
                    TextColor = "#1A1A1A",
                    TextSecondaryColor = "#5A5A5A",
                    BorderColor = "#B8D4E3",
                    SuccessColor = "#20B2AA",
                    WarningColor = "#FFB347",
                    ErrorColor = "#CD5C5C",
                    InfoColor = "#4682B4",
                    IsBuiltIn = true,
                    IsActive = true
                };
                await repository.AddAsync(oceanBlue);
            }

            if (!existingBuiltIns.Any(cs => cs.Name == "Forest Green"))
            {
                var forestGreen = new ColorScheme
                {
                    Name = "Forest Green",
                    Description = "Natural forest-inspired green theme",
                    PrimaryColor = "#228B22",
                    SecondaryColor = "#6B8E23",
                    BackgroundColor = "#F5FFFA",
                    SurfaceColor = "#E8F5E8",
                    TextColor = "#2F4F2F",
                    TextSecondaryColor = "#696969",
                    BorderColor = "#90EE90",
                    SuccessColor = "#32CD32",
                    WarningColor = "#DAA520",
                    ErrorColor = "#DC143C",
                    InfoColor = "#4682B4",
                    IsBuiltIn = true,
                    IsActive = true
                };
                await repository.AddAsync(forestGreen);
            }

            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing built-in color schemes");
        }
    }
}