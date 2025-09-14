using DataFlow.Mobile.Models;

namespace DataFlow.Mobile.Services.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<Page> Pages { get; }
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