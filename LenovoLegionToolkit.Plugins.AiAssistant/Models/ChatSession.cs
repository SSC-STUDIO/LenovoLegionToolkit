using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LenovoLegionToolkit.Plugins.AiAssistant.Models;

/// <summary>
/// Represents a chat session with its messages
/// </summary>
public class ChatSession : INotifyPropertyChanged
{
    private string _title = string.Empty;
    private DateTime _createdAt;
    private DateTime _lastUpdatedAt;

    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnPropertyChanged();
            }
        }
    }
    
    public DateTime CreatedAt
    {
        get => _createdAt;
        set
        {
            if (_createdAt != value)
            {
                _createdAt = value;
                OnPropertyChanged();
            }
        }
    }
    
    public DateTime LastUpdatedAt
    {
        get => _lastUpdatedAt;
        set
        {
            if (_lastUpdatedAt != value)
            {
                _lastUpdatedAt = value;
                OnPropertyChanged();
            }
        }
    }
    
    public ObservableCollection<ChatMessage> Messages { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

