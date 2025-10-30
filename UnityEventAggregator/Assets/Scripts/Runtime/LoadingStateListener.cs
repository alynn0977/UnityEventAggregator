using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class LoadingStateListener : MonoBehaviour
{
    [Header("Event Source")]
    [SerializeField] private LoadingConnector _connectorObject;
    [Tooltip("Direct connection to the aggregator")]

    [Header("Actions - What Should I Do?")]
    [SerializeField] private UnityEvent _onLoadingStarted = new UnityEvent();
    [Tooltip("DO THIS when loading starts")]
    
    [SerializeField] private UnityEvent _onLoadingProgress = new UnityEvent();
    [Tooltip("DO THIS when progress updates")]
    
    [SerializeField] private UnityEvent _onLoadingCompleted = new UnityEvent();
    [Tooltip("DO THIS when loading completes successfully")]
    
    [SerializeField] private UnityEvent _onLoadingFailed = new UnityEvent();
    [Tooltip("DO THIS when loading fails")]
    
    [Header("Debug")]
    [SerializeField] private bool _enableDebugLogging = false;

    private void OnEnable()
    {
        if (_connectorObject == null)
        {
            Debug.LogError($"[LoadingStateListener] {gameObject.name} has no aggregator assigned!");
            return;
        }

        // Subscribe to the connector's state change event
        _connectorObject.OnStateChanged += HandleStateChanged;

        if (_enableDebugLogging)
        {
            Debug.Log($"[LoadingStateListener] {gameObject.name} started listening to connector");
        }
    }

    private void OnDisable()
    {
        if (_connectorObject != null)
        {
            // Unsubscribe from the connector
            _connectorObject.OnStateChanged -= HandleStateChanged;
        }

        if (_enableDebugLogging)
        {
            Debug.Log($"[LoadingStateListener] {gameObject.name} stopped listening");
        }
    }

    /// <summary>
    /// Handles state changes from the connector and routes to appropriate UnityEvents
    /// </summary>
    private void HandleStateChanged(ILoadingState state)
    {
        if (_enableDebugLogging)
        {
            Debug.Log($"[LoadingStateListener] {gameObject.name} received state update - Phase: {state.Phase.DisplayName}, Progress: {state.Progress:P0}");
        }

        // Route based on phase ID
        var phaseId = state.Phase.Id.ToLower();

        if (phaseId == "started")
        {
            _onLoadingStarted?.Invoke();
        }
        else if (phaseId == "progress")
        {
            _onLoadingProgress?.Invoke();
        }
        else if (phaseId == "complete")
        {
            _onLoadingCompleted?.Invoke();
        }
        else if (phaseId == "failed")
        {
            _onLoadingFailed?.Invoke();
        }
        else
        {
            // Handle terminal phases generically
            if (state.Phase.IsTerminal)
            {
                if (IsSuccessPhase(state.Phase))
                {
                    _onLoadingCompleted?.Invoke();
                }
                else
                {
                    _onLoadingFailed?.Invoke();
                }
            }
        }
    }

    /// <summary>
    /// Determines if a phase represents a successful completion
    /// </summary>
    private bool IsSuccessPhase(LoadingPhase phase)
    {
        var phaseId = phase.Id.ToLower();
        return phaseId.Contains("complete") ||
               phaseId.Contains("success") ||
               phaseId.Contains("done") ||
               phaseId.Contains("finished");
    }
}