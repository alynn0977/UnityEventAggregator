using System;

public struct LoadingPhase
{
    public string Id { get; }
    public string DisplayName { get; }
    public bool IsTerminal { get; }
    
    public LoadingPhase(string id, string displayName, bool isTerminal)
    {
        Id = id;
        DisplayName = displayName;
        IsTerminal = isTerminal;
    }
    
    public override string ToString() => DisplayName;
    public override bool Equals(object obj) => obj is LoadingPhase other && Id == other.Id;
    public override int GetHashCode() => Id?.GetHashCode() ?? 0;
}

public static class LoadingPhases
{
    public static LoadingPhase Started = new LoadingPhase("started", "Loading Started", false);
    public static LoadingPhase InProgress = new LoadingPhase("progress", "In Progress", false);
    public static LoadingPhase Complete = new LoadingPhase("complete", "Loading Complete", true);
    public static LoadingPhase Failed = new LoadingPhase("failed", "Loading Failed", true);
    public static LoadingPhase Cancelled = new LoadingPhase("cancelled", "Loading Cancelled", true);
}