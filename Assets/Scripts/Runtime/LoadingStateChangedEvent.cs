using System;

public enum LoadingChangeType
{
    Added,
    Updated,
    Removed,
    ProgressChanged
}

public class LoadingStateChangedEvent : GameEvent
{
    public ILoadingState State { get; }
    public LoadingChangeType ChangeType { get; }
    
    public LoadingStateChangedEvent(ILoadingState state, LoadingChangeType changeType)
    {
        State = state;
        ChangeType = changeType;
    }
}