using DataFlow.Mobile.Models;
using DataFlow.Mobile.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace DataFlow.Mobile.Services;

public class LayoutTemplateService : ILayoutTemplateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LayoutTemplateService> _logger;

    public LayoutTemplateService(IUnitOfWork unitOfWork, ILogger<LayoutTemplateService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<LayoutTemplate>> GetAllLayoutTemplatesAsync()
    {
        try
        {
            var repository = _unitOfWork.GetRepository<LayoutTemplate>();
            return await repository.GetAllAsync(orderBy: q => q.OrderBy(lt => lt.IsBuiltIn ? 0 : 1).ThenBy(lt => lt.Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving layout templates");
            return [];
        }
    }

    public async Task<LayoutTemplate?> GetLayoutTemplateByIdAsync(int id)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<LayoutTemplate>();
            return await repository.GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving layout template with ID {Id}", id);
            return null;
        }
    }

    public async Task<List<LayoutTemplate>> GetBuiltInLayoutTemplatesAsync()
    {
        try
        {
            var repository = _unitOfWork.GetRepository<LayoutTemplate>();
            return await repository.GetAsync(lt => lt.IsBuiltIn, orderBy: q => q.OrderBy(lt => lt.Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving built-in layout templates");
            return [];
        }
    }

    public async Task<List<LayoutTemplate>> GetLayoutTemplatesByTypeAsync(string layoutType)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<LayoutTemplate>();
            return await repository.GetAsync(
                lt => lt.LayoutType.ToLower() == layoutType.ToLower(),
                orderBy: q => q.OrderBy(lt => lt.IsBuiltIn ? 0 : 1).ThenBy(lt => lt.Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving layout templates by type {LayoutType}", layoutType);
            return [];
        }
    }

    public async Task<LayoutTemplate> CreateLayoutTemplateAsync(LayoutTemplate layoutTemplate)
    {
        try
        {
            layoutTemplate.IsBuiltIn = false;
            layoutTemplate.CreatedAt = DateTime.UtcNow;
            layoutTemplate.UpdatedAt = DateTime.UtcNow;

            var repository = _unitOfWork.GetRepository<LayoutTemplate>();
            await repository.AddAsync(layoutTemplate);
            await _unitOfWork.SaveChangesAsync();

            return layoutTemplate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating layout template");
            throw;
        }
    }

    public async Task<LayoutTemplate> UpdateLayoutTemplateAsync(LayoutTemplate layoutTemplate)
    {
        try
        {
            if (layoutTemplate.IsBuiltIn)
            {
                throw new InvalidOperationException("Cannot modify built-in layout templates");
            }

            layoutTemplate.UpdatedAt = DateTime.UtcNow;

            var repository = _unitOfWork.GetRepository<LayoutTemplate>();
            repository.Update(layoutTemplate);
            await _unitOfWork.SaveChangesAsync();

            return layoutTemplate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating layout template with ID {Id}", layoutTemplate.Id);
            throw;
        }
    }

    public async Task<bool> DeleteLayoutTemplateAsync(int id)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<LayoutTemplate>();
            var layoutTemplate = await repository.GetByIdAsync(id);

            if (layoutTemplate == null)
                return false;

            if (layoutTemplate.IsBuiltIn)
            {
                throw new InvalidOperationException("Cannot delete built-in layout templates");
            }

            // Check if any templates are using this layout template
            var templateRepository = _unitOfWork.GetRepository<Template>();
            var templatesUsingLayout = await templateRepository.GetAsync(t => t.LayoutTemplateId == id);

            if (templatesUsingLayout.Any())
            {
                throw new InvalidOperationException("Cannot delete layout template that is in use by templates");
            }

            repository.Delete(layoutTemplate);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting layout template with ID {Id}", id);
            throw;
        }
    }

    public async Task<LayoutTemplate> DuplicateLayoutTemplateAsync(int id, string newName)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<LayoutTemplate>();
            var originalTemplate = await repository.GetByIdAsync(id);

            if (originalTemplate == null)
                throw new ArgumentException($"Layout template with ID {id} not found");

            var duplicatedTemplate = new LayoutTemplate
            {
                Name = newName,
                Description = $"Copy of {originalTemplate.Name}",
                LayoutType = originalTemplate.LayoutType,
                ColumnsPerRow = originalTemplate.ColumnsPerRow,
                ItemSpacing = originalTemplate.ItemSpacing,
                ItemPadding = originalTemplate.ItemPadding,
                BorderRadius = originalTemplate.BorderRadius,
                ShowShadows = originalTemplate.ShowShadows,
                ShowBorders = originalTemplate.ShowBorders,
                ShadowColor = originalTemplate.ShadowColor,
                ShadowOffset = originalTemplate.ShadowOffset,
                ShadowBlur = originalTemplate.ShadowBlur,
                BorderColor = originalTemplate.BorderColor,
                BorderWidth = originalTemplate.BorderWidth,
                EnableHover = originalTemplate.EnableHover,
                HoverColor = originalTemplate.HoverColor,
                EnableSelection = originalTemplate.EnableSelection,
                SelectionColor = originalTemplate.SelectionColor,
                IsBuiltIn = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await repository.AddAsync(duplicatedTemplate);
            await _unitOfWork.SaveChangesAsync();

            return duplicatedTemplate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating layout template with ID {Id}", id);
            throw;
        }
    }

    public async Task<LayoutTemplate> GetDefaultLayoutTemplateAsync()
    {
        try
        {
            var repository = _unitOfWork.GetRepository<LayoutTemplate>();
            var defaultTemplate = await repository.GetAsync(lt => lt.IsBuiltIn && lt.Name == "Simple List");

            return defaultTemplate.FirstOrDefault() ?? LayoutTemplate.DefaultList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving default layout template");
            return LayoutTemplate.DefaultList;
        }
    }

    public async Task InitializeBuiltInLayoutTemplatesAsync()
    {
        try
        {
            var repository = _unitOfWork.GetRepository<LayoutTemplate>();
            var existingBuiltIns = await repository.GetAsync(lt => lt.IsBuiltIn);

            // Add default list layout if it doesn't exist
            if (!existingBuiltIns.Any(lt => lt.Name == "Simple List"))
            {
                var simpleList = LayoutTemplate.DefaultList;
                await repository.AddAsync(simpleList);
            }

            // Add compact list layout if it doesn't exist
            if (!existingBuiltIns.Any(lt => lt.Name == "Compact List"))
            {
                var compactList = LayoutTemplate.CompactList;
                await repository.AddAsync(compactList);
            }

            // Add card grid layout if it doesn't exist
            if (!existingBuiltIns.Any(lt => lt.Name == "Card Grid"))
            {
                var cardGrid = LayoutTemplate.CardGrid;
                await repository.AddAsync(cardGrid);
            }

            // Add additional built-in layouts
            if (!existingBuiltIns.Any(lt => lt.Name == "Dense Grid"))
            {
                var denseGrid = new LayoutTemplate
                {
                    Name = "Dense Grid",
                    Description = "Compact grid layout with 3 columns",
                    LayoutType = "Grid",
                    ColumnsPerRow = 3,
                    ItemSpacing = 6,
                    ItemPadding = 12,
                    BorderRadius = 6,
                    ShowShadows = false,
                    ShowBorders = true,
                    BorderWidth = 1,
                    BorderColor = "#E0E0E0",
                    IsBuiltIn = true,
                    IsActive = true
                };
                await repository.AddAsync(denseGrid);
            }

            if (!existingBuiltIns.Any(lt => lt.Name == "Minimal"))
            {
                var minimal = new LayoutTemplate
                {
                    Name = "Minimal",
                    Description = "Clean minimal layout with no shadows or borders",
                    LayoutType = "List",
                    ColumnsPerRow = 1,
                    ItemSpacing = 2,
                    ItemPadding = 16,
                    BorderRadius = 0,
                    ShowShadows = false,
                    ShowBorders = false,
                    EnableHover = false,
                    IsBuiltIn = true,
                    IsActive = true
                };
                await repository.AddAsync(minimal);
            }

            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing built-in layout templates");
        }
    }
}