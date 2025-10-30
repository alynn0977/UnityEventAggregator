using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "LoadingEventAggregator", menuName = "Loading System/Loading Connector")]
public class LoadingConnector : ScriptableObject
{
    [Header("Configuration")]
    [SerializeField] private bool _enableDebugLogging = true;

    /// <summary>
    /// Event broadcasted when a loading state changes
    /// </summary>
    public event Action<ILoadingState> OnStateChanged;

    private void OnEnable()
    {
        if (_enableDebugLogging)
        {
            Debug.Log("[LoadingConnector] Initialized.");
        }
    }

    private void OnDisable()
    {
        if (_enableDebugLogging)
        {
            Debug.Log("[LoadingConnector] Disabled and cleaned up");
        }

        // Clean up all subscribers
        OnStateChanged = null;
    }

    /// <summary>
    /// Update loading state with multiple categories
    /// </summary>
    public void PushStateUpdate(string id, LoadingPhase phase, float progress = 0f, string message = "")
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[LoadingConnector] UpdateState called with null/empty ID");
            return;
        }

        // Create the loading state
        var loadingState = new LoadingState(id)
        {
            Phase = phase,
            Progress = progress,
            Message = message
        };

        if (_enableDebugLogging)
        {
            Debug.Log($"[LoadingConnector] State Update - ID: {id}, Phase: {phase.DisplayName}, Progress: {progress:P0}, Message: {message}");
        }

        // Broadcast to all subscribers
        OnStateChanged?.Invoke(loadingState);
    }
    
    private bool IsSuccess(LoadingPhase phase)
    {
        var phaseId = phase.Id.ToLower();
        return phaseId.Contains("complete") || 
               phaseId.Contains("success") || 
               phaseId.Contains("done") ||
               phaseId.Contains("finished");
    }
}