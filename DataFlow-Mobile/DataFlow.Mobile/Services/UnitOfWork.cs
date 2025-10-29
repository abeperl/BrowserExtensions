using Microsoft.EntityFrameworkCore.Storage;
using DataFlow.Mobile.Models;
using DataFlow.Mobile.Services.Interfaces;

namespace DataFlow.Mobile.Services;

public class UnitOfWork : IUnitOfWork
{
    private readonly DataFlowDbContext _context;
    private IDbContextTransaction? _transaction;

    private IGenericRepository<DataPage>? _pages;
    private IGenericRepository<Template>? _templates;
    private IGenericRepository<PageAction>? _actions;
    private IGenericRepository<AuthenticationConfig>? _authenticationConfigs;
    private IGenericRepository<AppSettings>? _settings;
    private IGenericRepository<AudioConfigModel>? _audioConfigs;

    public UnitOfWork(DataFlowDbContext context)
    {
        _context = context;
    }

    public IGenericRepository<DataPage> Pages =>
        _pages ??= new GenericRepository<DataPage>(_context);

    public IGenericRepository<Template> Templates =>
        _templates ??= new GenericRepository<Template>(_context);

    public IGenericRepository<PageAction> Actions =>
        _actions ??= new GenericRepository<PageAction>(_context);

    public IGenericRepository<AuthenticationConfig> AuthenticationConfigs =>
        _authenticationConfigs ??= new GenericRepository<AuthenticationConfig>(_context);

    public IGenericRepository<AppSettings> Settings =>
        _settings ??= new GenericRepository<AppSettings>(_context);

    public IGenericRepository<AudioConfigModel> AudioConfigs =>
        _audioConfigs ??= new GenericRepository<AudioConfigModel>(_context);

    public IGenericRepository<T> GetRepository<T>() where T : class
    {
        return new GenericRepository<T>(_context);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}