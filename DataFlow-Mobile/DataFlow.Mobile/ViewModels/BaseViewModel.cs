using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DataFlow.Mobile.ViewModels;

public abstract class BaseViewModel : INotifyPropertyChanged
{
    private bool _isLoading;
    private string _title = string.Empty;

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
            return false;

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected bool SetProperty<T>(ref T storage, T value, Action onChanged, [CallerMemberName] string? propertyName = null)
    {
        if (SetProperty(ref storage, value, propertyName))
        {
            onChanged?.Invoke();
            return true;
        }

        return false;
    }
}