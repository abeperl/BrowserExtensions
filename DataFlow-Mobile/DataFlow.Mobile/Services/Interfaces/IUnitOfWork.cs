using DataFlow.Mobile.Models;
using PageModel = DataFlow.Mobile.Models.Page;

namespace DataFlow.Mobile.Services.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<PageModel> Pages { get; }
    IGenericRepository<Template> Templates { get; }
    IGenericRepository<PageAction> Actions { get; }
    IGenericRepository<AuthenticationConfig> AuthenticationConfigs { get; }
    IGenericRepository<AppSettings> Settings { get; }
    IGenericRepository<AudioConfigModel> AudioConfigs { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}