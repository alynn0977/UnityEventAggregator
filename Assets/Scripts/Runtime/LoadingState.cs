using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class LoadingState : ILoadingState, INotifyPropertyChanged
{
    private string _id;
    private LoadingPhase _phase;
    private float _progress = 0f;
    private string _message = string.Empty;
    private DateTime _timestamp;
    private LoadingCategory _categories;

    public event PropertyChangedEventHandler PropertyChanged;

    public string Id => _id;

    public LoadingPhase Phase
    {
        get => _phase;
        set
        {
            if (!_phase.Equals(value))
            {
                _phase = value;
                _timestamp = DateTime.UtcNow;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Timestamp));
            }
        }
    }

    public float Progress
    {
        get => _progress;
        set
        {
            if (_progress != value)
            {
                _progress = value;
                _timestamp = DateTime.UtcNow;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Timestamp));
            }
        }
    }

    public string Message
    {
        get => _message;
        set
        {
            if (_message != value)
            {
                _message = value;
                _timestamp = DateTime.UtcNow;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Timestamp));
            }
        }
    }

    public LoadingCategory Categories
    {
        get => _categories;
        set
        {
            if (_categories != value)
            {
                _categories = value;
                _timestamp = DateTime.UtcNow;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Timestamp));
            }
        }
    }

    public DateTime Timestamp => _timestamp;

    public LoadingState(string id, LoadingCategory categories = LoadingCategory.None)
    {
        _id = id;
        _categories = categories;
        _timestamp = DateTime.UtcNow;
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}