using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "LoadingEventAggregator", menuName = "Loading System/Event Aggregator")]
public class LoadingEventAggregator : ScriptableObject
{
    [Header("Configuration")]
    [SerializeField] private bool _enableDebugLogging = true;
    [SerializeField] private float _terminalStateCleanupDelay = 5f;
    
    private Dictionary<string, LoadingState> _states;
    
    private void OnEnable()
    {
        _states = new Dictionary<string, LoadingState>();
        
        EventManager.Instance.AddListener<LoadingStateChangedEvent>(OnLoadingStateChangedEvent);
        
        if (_enableDebugLogging)
        {
            Debug.Log("[LoadingEventAggregator] Initialized and listening for LoadingStateChangedEvent");
        }
    }
    
    private void OnDisable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener<LoadingStateChangedEvent>(OnLoadingStateChangedEvent);
        }
        
        _states?.Clear();
        
        if (_enableDebugLogging)
        {
            Debug.Log("[LoadingEventAggregator] Disabled and cleaned up");
        }
    }
    
    /// <summary>
    /// Update loading state with multiple categories
    /// </summary>
    public void UpdateState(string id, LoadingPhase phase, LoadingCategory categories, float progress = 0f, string message = "")
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[LoadingEventAggregator] UpdateState called with null/empty ID");
            return;
        }
        
        LoadingChangeType changeType;
        
        // Get or create state
        if (!_states.TryGetValue(id, out LoadingState state))
        {
            state = new LoadingState(id, categories);
            _states[id] = state;
            changeType = LoadingChangeType.Added;
        }
        else
        {
            changeType = LoadingChangeType.Updated;
        }
        
        // Update state
        state.Phase = phase;
        state.Progress = progress;
        state.Message = message;
        state.Categories = categories;
        
        // Fire the event through EventManager
        var loadingEvent = new LoadingStateChangedEvent(state, changeType);
        EventManager.Instance.TriggerEvent(loadingEvent);
        
        if (_enableDebugLogging)
        {
            Debug.Log($"[LoadingEventAggregator] Fired event: {id} -> {phase.DisplayName} (Categories: {categories})");
        }
    }
    
    /// <summary>
    /// Convenience method - single category
    /// </summary>
    public void UpdateState(string id, LoadingPhase phase, float progress = 0f, string message = "")
    {
        UpdateState(id, phase, LoadingCategory.None, progress, message);
    }
    
    public LoadingState GetState(string id)
    {
        _states.TryGetValue(id, out LoadingState state);
        return state;
    }
    
    public IEnumerable<LoadingState> GetAllStates()
    {
        return _states.Values.ToList();
    }
    
    /// <summary>
    /// Get all states that match any of the specified categories
    /// </summary>
    public IEnumerable<LoadingState> GetStatesByCategories(LoadingCategory categories)
    {
        return _states.Values.Where(state => (state.Categories & categories) != 0);
    }
    
    public void ClearState(string id)
    {
        if (_states.TryGetValue(id, out LoadingState state))
        {
            _states.Remove(id);
            
            var removeEvent = new LoadingStateChangedEvent(state, LoadingChangeType.Removed);
            EventManager.Instance.TriggerEvent(removeEvent);
            
            if (_enableDebugLogging)
            {
                Debug.Log($"[LoadingEventAggregator] Cleared state: {id}");
            }
        }
    }
    
    private void OnLoadingStateChangedEvent(LoadingStateChangedEvent evt)
    {
        if (evt.State == null)
        {
            Debug.LogWarning("[LoadingEventAggregator] Received event with null state");
            return;
        }
        
        _states[evt.State.Id] = evt.State as LoadingState;
        
        if (_enableDebugLogging)
        {
            Debug.Log($"[LoadingEventAggregator] Processed event: {evt.State.Id} -> {evt.State.Phase.DisplayName} (Categories: {evt.State.Categories})");
        }
        
        if (evt.State.Phase.IsTerminal)
        {
            StartCoroutine(CleanupStateAfterDelay(evt.State.Id, _terminalStateCleanupDelay));
        }
    }
    
    private IEnumerator CleanupStateAfterDelay(string loadingId, float delay)
    {
        yield return new WaitForSeconds(delay);
        ClearState(loadingId);
    }
    
    [ContextMenu("Test Multi-Category Event")]
    private void TestMultiCategoryEvent()
    {
        if (Application.isPlaying)
        {
            var categories = LoadingCategory.Data | LoadingCategory.Molecules | LoadingCategory.Analytics;
            UpdateState("test-multi", LoadingPhases.Started, categories, 0.3f, "Loading molecule analytics data");
        }
    }
}