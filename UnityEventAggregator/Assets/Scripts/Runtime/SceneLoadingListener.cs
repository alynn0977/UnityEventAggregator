using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if MIRROR
using Mirror;
#endif

/// <summary>
/// Listens to scene loading events and persists across scene changes.
/// Integrates with Unity's SceneManager and optionally Mirror NetworkManager to track loading states.
/// Publishes loading progress through the LoadingConnector event system.
/// </summary>
/// <remarks>
/// Mirror Integration: Define 'MIRROR' in your Scripting Define Symbols to enable Mirror NetworkManager support.
/// </remarks>
public class SceneLoadingListener : MonoBehaviour
{
    [Header("Event Publishing")]
    [SerializeField] private LoadingConnector _loadingConnector;
    [Tooltip("The connector to publish scene loading events to")]

    [Header("Persistence")]
    [SerializeField] private bool _persistAcrossScenes = true;
    [Tooltip("Should this manager persist across scene changes?")]

    [Header("Listening Behavior")]
    [SerializeField] private bool _autoStartListening = false;
    [Tooltip("Auto-subscribe to scene events on OnEnable? If false, call StartListening() manually (e.g., from a button)")]

    [SerializeField] private bool _autoStopAfterSceneLoad = false;
    [Tooltip("Automatically stop listening after a scene successfully loads?")]

#if MIRROR
    [Header("Mirror Integration")]
    [SerializeField] private bool _trackNetworkSceneChanges = true;
    [Tooltip("Monitor Mirror NetworkManager for scene changes")]

    [SerializeField] private NetworkManager _networkManagerReference;
    [Tooltip("Optional: Assign your NetworkManager directly. If null, will auto-detect via FindObjectOfType")]
#endif

    [Header("Loading State IDs")]
    [SerializeField] private string _loadingStateId = "SceneLoading";
    [Tooltip("ID used when publishing loading state updates")]

    [Header("Debug")]
    [SerializeField] private bool _enableDebugLogging = true;

    private static SceneLoadingListener _instance;
#if MIRROR
    private NetworkManager _networkManager;
#endif
    private bool _isLoadingScene = false;
    private bool _isListening = false;
    private string _currentLoadingSceneName;
    private float _loadingProgress = 0f;

    #region Lifecycle

    private void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (_persistAcrossScenes)
        {
            if (_instance != null && _instance != this)
            {
                if (_enableDebugLogging)
                {
                    Debug.Log($"[SceneLoadingListener] Duplicate instance found on {gameObject.name}, destroying...");
                }
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (_enableDebugLogging)
            {
                Debug.Log($"[SceneLoadingListener] Instance set to persist across scenes on {gameObject.name}");
            }
        }

        ValidateConnector();
    }

    private void OnEnable()
    {
        // Only auto-subscribe if configured to do so
        if (_autoStartListening)
        {
            StartListening();
        }
    }

    private void OnDisable()
    {
        // Always unsubscribe on disable to prevent memory leaks
        StopListening();
    }

    #endregion

    #region Unity SceneManager Event Handlers

    /// <summary>
    /// Called when a scene has been loaded
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_enableDebugLogging)
        {
            Debug.Log($"[SceneLoadingListener] Scene loaded: {scene.name} (Mode: {mode})");
        }

        // Update loading state
        _isLoadingScene = false;
        _currentLoadingSceneName = scene.name;
        _loadingProgress = 1f;

        // Publish completion event
        PublishLoadingState(
            LoadingPhases.Complete,
            1f,
            $"Scene '{scene.name}' loaded successfully"
        );

        // Re-find NetworkManager in the new scene if needed
#if MIRROR
        if (_trackNetworkSceneChanges && _networkManager == null)
        {
            StartCoroutine(FindNetworkManagerCoroutine());
        }
#endif

        // Auto-stop listening if configured
        if (_autoStopAfterSceneLoad)
        {
            StopListening();
        }
    }

    /// <summary>
    /// Called when a scene is about to be unloaded
    /// </summary>
    private void OnSceneUnloaded(Scene scene)
    {
        if (_enableDebugLogging)
        {
            Debug.Log($"[SceneLoadingListener] Scene unloaded: {scene.name}");
        }

        // Mark as loading
        _isLoadingScene = true;
        _loadingProgress = 0f;

        PublishLoadingState(
            LoadingPhases.Started,
            0f,
            $"Unloading scene '{scene.name}'"
        );
    }

    /// <summary>
    /// Called when the active scene changes
    /// </summary>
    private void OnActiveSceneChanged(Scene previousScene, Scene newScene)
    {
        if (_enableDebugLogging)
        {
            Debug.Log($"[SceneLoadingListener] Active scene changed from '{previousScene.name}' to '{newScene.name}'");
        }

        PublishLoadingState(
            LoadingPhases.InProgress,
            0.5f,
            $"Switching to scene '{newScene.name}'"
        );
    }

    #endregion

#if MIRROR
    #region Mirror NetworkManager Integration

    /// <summary>
    /// Coroutine to find the NetworkManager (may not exist immediately on startup)
    /// </summary>
    private IEnumerator FindNetworkManagerCoroutine()
    {
        // Wait a frame for scene to settle
        yield return null;

        // Check if a NetworkManager was assigned in the inspector first
        if (_networkManagerReference != null)
        {
            _networkManager = _networkManagerReference;

            if (_enableDebugLogging)
            {
                Debug.Log($"[SceneLoadingListener] Using assigned NetworkManager reference: {_networkManager.GetType().Name}");
            }
        }
        else
        {
            // Fall back to auto-detection
            _networkManager = FindObjectOfType<NetworkManager>();

            if (_networkManager != null)
            {
                if (_enableDebugLogging)
                {
                    Debug.Log($"[SceneLoadingListener] Auto-detected NetworkManager: {_networkManager.GetType().Name}");
                }
            }
            else
            {
                if (_enableDebugLogging)
                {
                    Debug.Log("[SceneLoadingListener] No NetworkManager found in scene");
                }
            }
        }

        // Subscribe to network scene changes if we found a NetworkManager
        if (_networkManager != null)
        {
            SubscribeToNetworkManagerEvents();
        }
    }

    /// <summary>
    /// Subscribe to NetworkManager scene change events
    /// </summary>
    private void SubscribeToNetworkManagerEvents()
    {
        if (_networkManager == null) return;

        // Mirror doesn't have built-in events for scene changes, but we can monitor
        // the onlineScene and offlineScene properties and track when SceneManager loads them
        // The OnSceneLoaded handler will catch these automatically

        if (_enableDebugLogging)
        {
            Debug.Log($"[SceneLoadingListener] Monitoring NetworkManager scene changes (Online: {_networkManager.onlineScene}, Offline: {_networkManager.offlineScene})");
        }
    }

    /// <summary>
    /// Check if the current scene change is network-related
    /// </summary>
    private bool IsNetworkSceneChange(string sceneName)
    {
        if (_networkManager == null) return false;

        return sceneName == _networkManager.onlineScene ||
               sceneName == _networkManager.offlineScene;
    }

    #endregion
#endif

    #region Public API

    /// <summary>
    /// Start listening to scene loading events. Call this from your "START SESSION" button or similar.
    /// </summary>
    public void StartListening()
    {
        if (_isListening)
        {
            if (_enableDebugLogging)
            {
                Debug.Log("[SceneLoadingListener] Already listening to scene events");
            }
            return;
        }

        // Subscribe to Unity SceneManager events
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;

        _isListening = true;

        if (_enableDebugLogging)
        {
            Debug.Log("[SceneLoadingListener] Started listening to SceneManager events");
        }

        // Find and track NetworkManager if enabled
#if MIRROR
        if (_trackNetworkSceneChanges)
        {
            StartCoroutine(FindNetworkManagerCoroutine());
        }
#endif
    }

    /// <summary>
    /// Stop listening to scene loading events. Called automatically on disable or when auto-stop is enabled.
    /// </summary>
    public void StopListening()
    {
        if (!_isListening)
        {
            return;
        }

        // Unsubscribe from SceneManager events
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;

        _isListening = false;

        if (_enableDebugLogging)
        {
            Debug.Log("[SceneLoadingListener] Stopped listening to SceneManager events");
        }
    }

    /// <summary>
    /// Check if currently listening to scene events
    /// </summary>
    public bool IsListening => _isListening;

    /// <summary>
    /// Manually trigger a scene load with progress tracking
    /// </summary>
    public void LoadSceneAsync(string sceneName)
    {
        StartCoroutine(LoadSceneAsyncCoroutine(sceneName));
    }

    /// <summary>
    /// Manually trigger a scene load with progress tracking
    /// </summary>
    public void LoadSceneAsync(int sceneBuildIndex)
    {
        StartCoroutine(LoadSceneAsyncCoroutine(sceneBuildIndex));
    }

    /// <summary>
    /// Get current loading status
    /// </summary>
    public bool IsLoading => _isLoadingScene;

    /// <summary>
    /// Get current loading progress (0-1)
    /// </summary>
    public float Progress => _loadingProgress;

    /// <summary>
    /// Get the name of the scene currently being loaded
    /// </summary>
    public string CurrentLoadingScene => _currentLoadingSceneName;

    #endregion

    #region Async Scene Loading

    /// <summary>
    /// Load a scene asynchronously with progress tracking
    /// </summary>
    private IEnumerator LoadSceneAsyncCoroutine(string sceneName)
    {
        _isLoadingScene = true;
        _currentLoadingSceneName = sceneName;
        _loadingProgress = 0f;

        PublishLoadingState(
            LoadingPhases.Started,
            0f,
            $"Starting to load scene '{sceneName}'"
        );

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        if (asyncLoad == null)
        {
            PublishLoadingState(
                LoadingPhases.Failed,
                0f,
                $"Failed to start loading scene '{sceneName}'"
            );
            _isLoadingScene = false;
            yield break;
        }

        while (!asyncLoad.isDone)
        {
            // AsyncOperation.progress goes from 0 to 0.9, then jumps to 1 when done
            // We'll normalize it to 0-1 range
            _loadingProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            PublishLoadingState(
                LoadingPhases.InProgress,
                _loadingProgress,
                $"Loading scene '{sceneName}': {_loadingProgress:P0}"
            );

            yield return null;
        }

        // OnSceneLoaded will handle the completion event
    }

    /// <summary>
    /// Load a scene asynchronously by build index with progress tracking
    /// </summary>
    private IEnumerator LoadSceneAsyncCoroutine(int sceneBuildIndex)
    {
        _isLoadingScene = true;
        _currentLoadingSceneName = $"Scene Index {sceneBuildIndex}";
        _loadingProgress = 0f;

        PublishLoadingState(
            LoadingPhases.Started,
            0f,
            $"Starting to load scene at index {sceneBuildIndex}"
        );

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneBuildIndex);

        if (asyncLoad == null)
        {
            PublishLoadingState(
                LoadingPhases.Failed,
                0f,
                $"Failed to start loading scene at index {sceneBuildIndex}"
            );
            _isLoadingScene = false;
            yield break;
        }

        while (!asyncLoad.isDone)
        {
            _loadingProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            PublishLoadingState(
                LoadingPhases.InProgress,
                _loadingProgress,
                $"Loading scene {sceneBuildIndex}: {_loadingProgress:P0}"
            );

            yield return null;
        }

        // OnSceneLoaded will handle the completion event
    }

    #endregion

    #region Loading State Publishing

    /// <summary>
    /// Publish a loading state update to the connector
    /// </summary>
    private void PublishLoadingState(LoadingPhase phase, float progress, string message)
    {
        if (_loadingConnector == null)
        {
            if (_enableDebugLogging)
            {
                Debug.LogWarning("[SceneLoadingListener] Cannot publish state - no LoadingConnector assigned");
            }
            return;
        }

        _loadingConnector.PushStateUpdate(_loadingStateId, phase, progress, message);
    }

    /// <summary>
    /// Validate that the connector is assigned
    /// </summary>
    private void ValidateConnector()
    {
        if (_loadingConnector == null)
        {
            Debug.LogWarning("[SceneLoadingListener] No LoadingConnector assigned! Scene loading events will not be published.");
        }
    }

    #endregion

    #region Static Accessors

    /// <summary>
    /// Get the singleton instance of the SceneLoadingListener
    /// </summary>
    public static SceneLoadingListener Instance => _instance;

    #endregion
}
