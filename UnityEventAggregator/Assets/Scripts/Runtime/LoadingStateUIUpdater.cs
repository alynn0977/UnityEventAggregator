using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Updates UI elements based on loading state changes.
/// Attach to a GameObject with UI components.
/// Subscribe to a LoadingConnector and expose parameterless methods for UnityEvents.
/// In LoadingStateListener's UnityEvents, drag this component and call UpdateMessageText(), UpdateProgressText(), etc.
/// </summary>
public class LoadingStateUIUpdater : MonoBehaviour
{
    [Header("Event Source")]
    [SerializeField] private LoadingConnector _connectorObject;
    [Tooltip("The connector to listen to for state updates")]

    [Header("UI Elements")]
    [SerializeField] private Text _messageText;
    [Tooltip("UI Text component to display the message")]

    [SerializeField] private Text _progressText;
    [Tooltip("UI Text component to display progress percentage")]

    [SerializeField] private Text _phaseText;
    [Tooltip("UI Text component to display the phase name")]

    [SerializeField] private Slider _progressSlider;
    [Tooltip("Optional UI Slider to show progress (0-1)")]

    [Header("Display Settings")]
    [SerializeField] private string _messageFormat = "{0}";
    [Tooltip("Format string for message. {0} = message text. Example: 'Status: {0}'")]

    [SerializeField] private string _progressFormat = "{0:P0}";
    [Tooltip("Format string for progress. {0} = progress value. Example: '{0:P0}' shows '50%'")]

    [SerializeField] private string _phaseFormat = "{0}";
    [Tooltip("Format string for phase. {0} = phase display name. Example: 'Phase: {0}'")]

    [Header("Debug")]
    [SerializeField] private bool _enableDebugLogging = false;

    private ILoadingState _currentState;

    private void OnEnable()
    {
        if (_connectorObject == null)
        {
            Debug.LogError($"[LoadingStateUIUpdater] {gameObject.name} has no connector assigned!");
            return;
        }

        // Subscribe to the connector's state change event
        _connectorObject.OnStateChanged += OnStateChanged;

        if (_enableDebugLogging)
        {
            Debug.Log($"[LoadingStateUIUpdater] {gameObject.name} started listening to connector");
        }
    }

    private void OnDisable()
    {
        if (_connectorObject != null)
        {
            // Unsubscribe from the connector
            _connectorObject.OnStateChanged -= OnStateChanged;
        }

        if (_enableDebugLogging)
        {
            Debug.Log($"[LoadingStateUIUpdater] {gameObject.name} stopped listening");
        }
    }

    /// <summary>
    /// Internal handler that stores the current state when connector broadcasts
    /// </summary>
    private void OnStateChanged(ILoadingState state)
    {
        // Store the current state
        _currentState = state;

        if (_enableDebugLogging)
        {
            Debug.Log($"[LoadingStateUIUpdater] State stored - Phase: {state.Phase.DisplayName}, Progress: {state.Progress:P0}, Message: {state.Message}");
        }

        // Automatically update all UI elements immediately when state changes to sync things.
        SyncUIInternal();
    }

    /// <summary>
    /// Internal method to update all UI - called automatically when state changes.
    /// Required in order to prevent race conditions with UnityEvent call orders to UI.
    /// </summary>
    private void SyncUIInternal()
    {
        if (_currentState == null) return;

        // Update message
        if (_messageText != null && !string.IsNullOrEmpty(_currentState.Message))
        {
            _messageText.text = string.Format(_messageFormat, _currentState.Message);
        }

        // Update progress
        if (_progressText != null)
        {
            _progressText.text = string.Format(_progressFormat, _currentState.Progress);
        }

        // Update phase
        if (_phaseText != null)
        {
            _phaseText.text = string.Format(_phaseFormat, _currentState.Phase.DisplayName);
        }

        // Update slider
        if (_progressSlider != null)
        {
            _progressSlider.value = _currentState.Progress;
        }
    }

    // ===== PUBLIC METHODS - Call these from UnityEvents in Inspector =====

    /// <summary>
    /// Updates the message text from current state. Call this from UnityEvents.
    /// </summary>
    public void UpdateMessageText()
    {
        if (_currentState == null) return;

        if (_messageText != null && !string.IsNullOrEmpty(_currentState.Message))
        {
            string formattedMessage = string.Format(_messageFormat, _currentState.Message);
            _messageText.text = formattedMessage;

            if (_enableDebugLogging)
            {
                Debug.Log($"[LoadingStateUIUpdater] UpdateMessageText called - Message: {_currentState.Message}");
            }
        }
    }

    /// <summary>
    /// Updates the progress text from current state. Call this from UnityEvents.
    /// </summary>
    public void UpdateProgressText()
    {
        if (_currentState == null) return;

        if (_progressText != null)
        {
            string formattedProgress = string.Format(_progressFormat, _currentState.Progress);
            _progressText.text = formattedProgress;

            if (_enableDebugLogging)
            {
                Debug.Log($"[LoadingStateUIUpdater] UpdateProgressText called - Progress: {_currentState.Progress:P0}");
            }
        }
    }

    /// <summary>
    /// Updates the phase text from current state. Call this from UnityEvents.
    /// </summary>
    public void UpdatePhaseText()
    {
        if (_currentState == null) return;

        if (_phaseText != null)
        {
            string formattedPhase = string.Format(_phaseFormat, _currentState.Phase.DisplayName);
            _phaseText.text = formattedPhase;

            if (_enableDebugLogging)
            {
                Debug.Log($"[LoadingStateUIUpdater] UpdatePhaseText called - Phase: {_currentState.Phase.DisplayName}");
            }
        }
    }

    /// <summary>
    /// Updates the progress slider from current state. Call this from UnityEvents.
    /// </summary>
    public void UpdateProgressSlider()
    {
        if (_currentState == null) return;

        if (_progressSlider != null)
        {
            _progressSlider.value = _currentState.Progress;

            if (_enableDebugLogging)
            {
                Debug.Log($"[LoadingStateUIUpdater] UpdateProgressSlider called - Progress: {_currentState.Progress:P0}");
            }
        }
    }

    /// <summary>
    /// Updates all UI elements at once. Call this from UnityEvents.
    /// </summary>
    public void UpdateAllUI()
    {
        UpdateMessageText();
        UpdateProgressText();
        UpdatePhaseText();
        UpdateProgressSlider();
    }
}