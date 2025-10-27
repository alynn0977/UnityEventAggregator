using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class LoadingStateListener : MonoBehaviour
{
    [Header("Event Source")]
    [SerializeField] private LoadingEventAggregator _aggregator;
    [Tooltip("Direct connection to the aggregator")]
    
    [Header("Category Filtering - What Types Should I Listen For?")]
    [SerializeField] private LoadingCategory _filterByCategories = LoadingCategory.None;
    [Tooltip("React to loading in these categories. Use bitwise OR for multiple categories.")]
    
    [SerializeField] private string[] _ignoreSpecificIds = new string[0];
    [Tooltip("Ignore these specific IDs even if they match categories")]
    
    [SerializeField] private bool _requireAllCategoriesComplete = false;
    [Tooltip("Wait for ALL active loading in my categories to complete")]
    
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
        if (_aggregator == null)
        {
            Debug.LogError($"[LoadingStateListener] {gameObject.name} has no aggregator assigned!");
            return;
        }
        
        EventManager.Instance.AddListener<LoadingStateChangedEvent>(OnEventReceived);
        
        if (_enableDebugLogging)
        {
            Debug.Log($"[LoadingStateListener] {gameObject.name} listening for categories: {_filterByCategories}");
        }
    }
    
    private void OnDisable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener<LoadingStateChangedEvent>(OnEventReceived);
        }
        
        if (_enableDebugLogging)
        {
            Debug.Log($"[LoadingStateListener] {gameObject.name} stopped listening");
        }
    }
    
    /// <summary>
    /// "Oh! I got an event! Does this category match what I care about?"
    /// </summary>
    private void OnEventReceived(LoadingStateChangedEvent evt)
    {
        if (_enableDebugLogging)
        {
            Debug.Log($"[LoadingStateListener] {gameObject.name} received event: {evt.State.Id} -> {evt.State.Phase.DisplayName} (Categories: {evt.State.Categories})");
        }
        
        // "Do I care about this category?"
        if (!ShouldIRespondToThis(evt.State))
        {
            if (_enableDebugLogging)
            {
                Debug.Log($"[LoadingStateListener] {gameObject.name} ignoring - categories don't match or ID is ignored");
            }
            return;
        }
        
        if (_enableDebugLogging)
        {
            Debug.Log($"[LoadingStateListener] {gameObject.name} responding to categories: {evt.State.Categories}!");
        }
        
        // "Should I wait for other things in my categories to finish?"
        if (_requireAllCategoriesComplete && !AreAllMyCategoriesComplete())
        {
            if (_enableDebugLogging)
            {
                Debug.Log($"[LoadingStateListener] {gameObject.name} waiting for all category loading to complete...");
            }
            return;
        }
        
        DoTheThing(evt.State);
    }
    
    /// <summary>
    /// "Should I care about this loading state based on categories?"
    /// </summary>
    private bool ShouldIRespondToThis(ILoadingState state)
    {
        // "If I have no category filter, I don't care about anything"
        if (_filterByCategories == LoadingCategory.None)
        {
            return false;
        }
        
        // "Is this ID specifically ignored?"
        if (_ignoreSpecificIds.Contains(state.Id))
        {
            return false;
        }
        
        // "Do any of this state's categories match any of my categories?"
        return (state.Categories & _filterByCategories) != 0;
    }
    
    /// <summary>
    /// "Are all loading operations in my categories finished?"
    /// </summary>
    private bool AreAllMyCategoriesComplete()
    {
        var relevantStates = _aggregator.GetStatesByCategories(_filterByCategories);
        
        foreach (var state in relevantStates)
        {
            // Skip ignored IDs
            if (_ignoreSpecificIds.Contains(state.Id))
            {
                continue;
            }
            
            if (!state.Phase.IsTerminal)
            {
                if (_enableDebugLogging)
                {
                    Debug.Log($"[LoadingStateListener] Still waiting for {state.Id} in categories {state.Categories}");
                }
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// "DO THE THING! Execute the appropriate action"
    /// </summary>
    private void DoTheThing(ILoadingState state)
    {
        if (state.Phase.IsTerminal)
        {
            if (IsSuccess(state.Phase))
            {
                if (_enableDebugLogging)
                {
                    Debug.Log($"[LoadingStateListener] {gameObject.name} SUCCESS for categories {state.Categories}!");
                }
                
                _onLoadingCompleted.Invoke();
            }
            else
            {
                if (_enableDebugLogging)
                {
                    Debug.Log($"[LoadingStateListener] {gameObject.name} FAILURE for categories {state.Categories}!");
                }
                
                _onLoadingFailed.Invoke();
            }
        }
        else
        {
            if (state.Progress > 0)
            {
                if (_enableDebugLogging)
                {
                    Debug.Log($"[LoadingStateListener] {gameObject.name} PROGRESS for categories {state.Categories} ({state.Progress:P0})!");
                }
                
                _onLoadingProgress.Invoke();
            }
            else
            {
                if (_enableDebugLogging)
                {
                    Debug.Log($"[LoadingStateListener] {gameObject.name} STARTED for categories {state.Categories}!");
                }
                
                _onLoadingStarted.Invoke();
            }
        }
    }
    
    private bool IsSuccess(LoadingPhase phase)
    {
        var phaseId = phase.Id.ToLower();
        return phaseId.Contains("complete") || 
               phaseId.Contains("success") || 
               phaseId.Contains("done") ||
               phaseId.Contains("finished");
    }
    
    /// <summary>
    /// Check if any loading is currently active in my categories
    /// </summary>
    public bool IsAnyLoadingActive()
    {
        if (_aggregator == null) return false;
        
        var relevantStates = _aggregator.GetStatesByCategories(_filterByCategories);
        return relevantStates.Any(state => !state.Phase.IsTerminal && !_ignoreSpecificIds.Contains(state.Id));
    }
    
    [ContextMenu("Test - Molecule Data Loading")]
    private void TestMoleculeDataLoading()
    {
        if (Application.isPlaying && _aggregator != null)
        {
            var categories = LoadingCategory.Data | LoadingCategory.Molecules;
            _aggregator.UpdateState("test-molecule-data", LoadingPhases.Started, categories, 0f, "Loading molecule data");
        }
    }
    
    [ContextMenu("Test - Analytics Loading")]
    private void TestAnalyticsLoading()
    {
        if (Application.isPlaying && _aggregator != null)
        {
            var categories = LoadingCategory.Analytics | LoadingCategory.Molecules;
            _aggregator.UpdateState("test-analytics", LoadingPhases.Started, categories, 0f, "Running analytics");
        }
    }
}