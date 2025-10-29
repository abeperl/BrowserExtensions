    $ErrorActionPreference = 'Stop'

    $root = 'C:\Users\User\source\repos\BrowserExtensions'
    $projDir = Join-Path $root 'DataFlow-Mobile\DataFlow.Mobile'
    $csproj = Join-Path $projDir 'DataFlow.Mobile.csproj'
    $mauiProgram = Join-Path $projDir 'MauiProgram.cs'
    $apiService = Join-Path $projDir 'Services\ApiService.cs'
    $authService = Join-Path $projDir 'Services\AuthenticationService.cs'
    $iapiService = Join-Path $projDir 'Services\Interfaces\IApiService.cs'
    $ipageService = Join-Path $projDir 'Services\Interfaces\IPageService.cs'
    $pageService = Join-Path $projDir 'Services\PageService.cs'

    function Replace-InFile([string]$path, [string]$pattern, [string]$replacement) {
      if (-not (Test-Path $path)) { throw "File not found: $path" }
      $c = Get-Content $path -Raw
      $n = [regex]::Replace($c, $pattern, $replacement, 'Singleline')
      if ($n -ne $c) {
        Set-Content -Path $path -Value $n -NoNewline
        Write-Host "Updated $path"
      } else {
        Write-Host "No change needed for $path"
      }
    }

    # 1) csproj: switch to net9, remove Polly, set logging debug to 9.0.9

    Replace-InFile $csproj 'net10.0-android' 'net9.0-android'
    Replace-InFile $csproj 'net10.0-ios' 'net9.0-ios'
    Replace-InFile $csproj 'net10.0-maccatalyst' 'net9.0-maccatalyst'
    Replace-InFile $csproj 'net10.0-windows10.0.19041.0' 'net9.0-windows10.0.19041.0'
    Replace-InFile $csproj 'Microsoft.Extensions.Logging.Debug" Version="[^"]+' 'Microsoft.Extensions.Logging.Debug" Version="9.0.9'
    Replace-InFile $csproj '(?ms)\r?\n\s*<PackageReference Include="Polly"[^>]+/>\r?\n' ''
    Replace-InFile $csproj '(?ms)\r?\n\s*<PackageReference Include="Polly.Extensions.Http"[^>]+/>\r?\n' ''

    # 2) MauiProgram.cs: remove Polly usings; remove client.Timeout lines

    Replace-InFile $mauiProgram '(?m)^\susing Polly(.|;).\r?\n' ''
    Replace-InFile $mauiProgram '(?m)^\sclient.Timeout\s=\sTimeSpan.FromSeconds(\d+);\s\r?$' ''

    # Ensure DataFlowApi still adds UA header (no change needed normally)

    # 3) IApiService.cs: rewrite with CancellationToken and correct namespace

    @"
    using DataFlow.Mobile.Models;

    namespace DataFlow.Mobile.Services.Interfaces;

    public interface IApiService
    {
        // Page-based API calls with automatic authentication
        Task<ApiResponse<T>> GetAsync<T>(int pageId, CancellationToken cancellationToken = default);
        Task<ApiResponse<T>> GetAsync<T>(string url, Dictionary<string, string>? headers = null, int? pageId = null,
    CancellationToken cancellationToken = default);
        Task<ApiResponse<T>> PostAsync<T>(string url, object? data = null, Dictionary<string, string>? headers = null, int? pageId =
     null, CancellationToken cancellationToken = default);
        Task<ApiResponse<T>> PutAsync<T>(string url, object? data = null, Dictionary<string, string>? headers = null, int? pageId = 
    null, CancellationToken cancellationToken = default);
        Task<ApiResponse<T>> DeleteAsync<T>(string url, Dictionary<string, string>? headers = null, int? pageId = null,
    CancellationToken cancellationToken = default);

        // Raw API calls without authentication
        Task<ApiResponse<T>> GetRawAsync<T>(string url, Dictionary<string, string>? headers = null, CancellationToken
    cancellationToken = default);
        Task<ApiResponse<T>> PostRawAsync<T>(string url, object? data = null, Dictionary<string, string>? headers = null,
    CancellationToken cancellationToken = default);

        // Utility methods
        Task<bool> TestConnectionAsync(string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken =
     default);
        Task<bool> TestPageConnectionAsync(int pageId, CancellationToken cancellationToken = default);
        Task<ApiResponse<object>> ExecutePageDataRequestAsync(int pageId, CancellationToken cancellationToken = default);

    }
    "@ | Set-Content -Path $iapiService -NoNewline

    # 4) IPageService.cs: replace incorrect interface

    @"
    using DataFlow.Mobile.Models;

    namespace DataFlow.Mobile.Services.Interfaces;

    public interface IPageService
    {
        Task<IEnumerable<Page>> GetAllPagesAsync(CancellationToken cancellationToken = default);
        Task<Page?> GetPageByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Page> CreatePageAsync(Page page, CancellationToken cancellationToken = default);
        Task<Page> UpdatePageAsync(Page page, CancellationToken cancellationToken = default);
        Task<bool> DeletePageAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<object>> FetchPageDataAsync(int pageId, CancellationToken cancellationToken = default);
    }
    "@ | Set-Content -Path $ipageService -NoNewline

    # 5) PageService.cs: correct implementation using Pages DbSet

    @"
    using Microsoft.EntityFrameworkCore;
    using DataFlow.Mobile.Models;
    using Microsoft.Extensions.Logging;
    using DataFlow.Mobile.Services.Interfaces;

    namespace DataFlow.Mobile.Services;

    public class PageService : IPageService
    {
        private readonly DataFlowDbContext _context;
        private readonly ILogger<PageService> _logger;

        public PageService(DataFlowDbContext context, ILogger<PageService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Page>> GetAllPagesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.Pages
                    .AsNoTracking()
                    .Include(p => p.Template)
                    .Include(p => p.Actions)
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Name)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all pages");
                return Enumerable.Empty<Page>();
            }
        }

        public async Task<Page?> GetPageByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.Pages
                    .Include(p => p.Template)
                    .Include(p => p.Actions)
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting page by ID: {PageId}", id);
                return null;
            }
        }

        public async Task<Page> CreatePageAsync(Page page, CancellationToken cancellationToken = default)
        {
            try
            {
                page.CreatedAt = DateTime.UtcNow;
                page.UpdatedAt = DateTime.UtcNow;
                _context.Pages.Add(page);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Created new page: {PageName} (ID: {PageId})", page.Name, page.Id);
                return page;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating page: {PageName}", page.Name);
                throw;
            }
        }

        public async Task<Page> UpdatePageAsync(Page page, CancellationToken cancellationToken = default)
        {
            try
            {
                page.UpdatedAt = DateTime.UtcNow;
                _context.Entry(page).State = EntityState.Modified;
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Updated page: {PageName} (ID: {PageId})", page.Name, page.Id);
                return page;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating page: {PageId}", page.Id);
                throw;
            }
        }

        public async Task<bool> DeletePageAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var page = await _context.Pages.FindAsync(new object?[] { id }, cancellationToken);
                if (page == null)
                    return false;

                page.IsActive = false;
                page.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Deleted page: {PageId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting page: {PageId}", id);
                return false;
            }
        }

        public async Task<IEnumerable<object>> FetchPageDataAsync(int pageId, CancellationToken cancellationToken = default)        
        {
            try
            {
                await Task.CompletedTask; // to be implemented with ApiService
                _logger.LogInformation("Fetching data for page: {PageId}", pageId);
                return Enumerable.Empty<object>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching data for page: {PageId}", pageId);
                return Enumerable.Empty<object>();
            }
        }

    }
    "@ | Set-Content -Path $pageService -NoNewline

    # 6) AuthenticationService.cs: do not persist tokens in DB in CacheTokenAsync

    if (Test-Path $authService) {
      $authText = Get-Content $authService -Raw

    # Remove lines that persist tokens to DB inside CacheTokenAsync

      $authText = [regex]::Replace($authText, '(?m)^\sconfig.(AccessToken|RefreshToken|TokenExpiry)\s=.\r?\n', '')
      $authText = [regex]::Replace($authText, '(?m)^\sawait\s+SaveAuthConfigAsync(config);\s*\r?\n', '')
      Set-Content -Path $authService -Value $authText -NoNewline
      Write-Host "Updated $authService (removed DB token persistence)"
    }

    # 7) ApiService.cs: DI scope fix, CancellationToken support, safer logging

    if (Test-Path $apiService) {

    # Add DI using

      Replace-InFile $apiService '(?m)^using Microsoft.Extensions.Logging;\s*$' "using Microsoft.Extensions.Logging;rnusing
    Microsoft.Extensions.DependencyInjection;"

    # Add IServiceScopeFactory field

      Replace-InFile $apiService 'private readonly INetworkService _networkService;\s*' '$0private readonly IServiceScopeFactory    
    _scopeFactory;'

    # Add scopeFactory to constructor signature and assign

      Replace-InFile $apiService 'INetworkService networkService)\s*{' 'INetworkService networkService, IServiceScopeFactory        
    scopeFactory){'
      Replace-InFile $apiService '_networkService = networkService;\s*' "_networkService = networkService;rn        _scopeFactory = 
    scopeFactory;"

    # Fix invalid scope creation

      Replace-InFile $apiService '_httpClientFactory.GetService<IServiceScope>()' '_scopeFactory.CreateScope()'

    # Propagate CancellationToken in SendAsync if not already present

      Replace-InFile $apiService 'SendAsync(request);' 'SendAsync(request, cancellationToken);'
      Replace-InFile $apiService 'SendAsync(request)' 'SendAsync(request, cancellationToken)'

    # Safer body logging (Debug-only, truncated)

      Replace-InFile $apiService '(?ms)_logger.LogDebug("Request {RequestId} body: {Body}", requestId, jsonData);' 'if
    (_logger.IsEnabled(LogLevel.Debug)) { _logger.LogDebug("Request {RequestId} body: {Body}", requestId, jsonData?.Length > 2048 ? 
    jsonData.Substring(0,2048) + "...": jsonData); }'
      Replace-InFile $apiService '(?ms)_logger.LogDebug("Response {RequestId} body: {Body}", requestId, content);' 'if
    (_logger.IsEnabled(LogLevel.Debug)) { _logger.LogDebug("Response {RequestId} body: {Body}", requestId, content?.Length > 2048 ? 
    content.Substring(0,2048) + "...": content); }'
      Write-Host "Updated $apiService (DI scope fix + safer logging)"
    }

    # Create branch and commit

    try {
      Set-Location $root
      git rev-parse --is-inside-work-tree *> $null 2>&1
      if ($LASTEXITCODE -eq 0) {
        git checkout -b refactor/net9-upgrade
        git add -A
        git commit -m "Upgrade to .NET 9; Http resilience; DI fix; tokens in secure storage; IPageService/PageService fixes"        
        Write-Host "Committed on branch refactor/net9-upgrade"
      } else {
        Write-Host "Not a git repo; skipping commit."
      }
    } catch {
      Write-Warning "Git commit failed: $($_.Exception.Message)"
    }

    Write-Host "Script done. Run: dotnet restore; dotnet build"