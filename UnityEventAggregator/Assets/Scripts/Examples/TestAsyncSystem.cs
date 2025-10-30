using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class TestAsyncSystem : MonoBehaviour
{
    [Header("Event Connector")]
    [SerializeField] private LoadingConnector _connector;
    [Tooltip("The connector that will broadcast loading state updates")]

    [Header("Configuration")]
    [SerializeField] private int _minProcessingTimeMs = 1000;
    [SerializeField] private int _maxProcessingTimeMs = 3000;
    [SerializeField] private bool _enableDebugLogging = true;

    private string _processId = "TestAsyncProcess";

    private void Start()
    {
        if (_connector == null)
        {
            Debug.LogError("[TestAsyncSystem] No LoadingConnector assigned!");
            return;
        }
    }

    /// <summary>
    /// Initiates the async test process
    /// </summary>
    public void StartAsyncProcess()
    {
        if (_connector == null)
        {
            Debug.LogError("[TestAsyncSystem] Cannot start - no connector assigned!");
            return;
        }

        StartCoroutine(RunAsyncProcessCoroutine());
    }

    /// <summary>
    /// Coroutine wrapper to handle async work and main thread marshaling
    /// </summary>
    private IEnumerator RunAsyncProcessCoroutine()
    {
        // STEP 1: Push "Started" state
        _connector.PushStateUpdate(_processId, LoadingPhases.Started, 0f, "Starting async process...");

        if (_enableDebugLogging)
        {
            Debug.Log("[TestAsyncSystem] Process started on main thread");
        }

        // STEP 2: Start background work (using System.Random instead of Unity's)
        int result = 0;
        bool taskCompleted = false;
        Exception taskException = null;

        Task backgroundTask = Task.Run(() =>
        {
            try
            {
                result = PerformBackgroundWork();
                taskCompleted = true;
            }
            catch (Exception ex)
            {
                taskException = ex;
                taskCompleted = true;
            }
        });

        // STEP 3: Wait for background task and show progress
        float elapsedTime = 0f;
        float estimatedDuration = (_minProcessingTimeMs + _maxProcessingTimeMs) / 2000f; // Convert to seconds

        while (!taskCompleted)
        {
            yield return null; // Wait one frame
            elapsedTime += Time.deltaTime;

            // Calculate progress
            float progress = Mathf.Clamp01(elapsedTime / estimatedDuration);
            _connector.PushStateUpdate(_processId, LoadingPhases.InProgress, progress, $"Processing... {progress:P0}");
        }

        // STEP 4: Back on main thread - process the result
        if (taskException != null)
        {
            // Exception occurred
            _connector.PushStateUpdate(_processId, LoadingPhases.Failed, 1f, $"Exception: {taskException.Message}");
            Debug.LogError($"[TestAsyncSystem] Background task failed: {taskException}");
            yield break;
        }

        if (_enableDebugLogging)
        {
            Debug.Log($"[TestAsyncSystem] Background work completed. Result: {result}");
        }

        // STEP 5: Check if result is even or odd
        bool isEven = result % 2 == 0;

        if (isEven)
        {
            // Success case - even number
            _connector.PushStateUpdate(_processId, LoadingPhases.Complete, 1f, $"Success! Result was even: {result}");

            if (_enableDebugLogging)
            {
                Debug.Log($"[TestAsyncSystem] Process completed successfully - Result: {result} (EVEN)");
            }
        }
        else
        {
            // Failure case - odd number
            _connector.PushStateUpdate(_processId, LoadingPhases.Failed, 1f, $"Failed! Result was odd: {result}");

            if (_enableDebugLogging)
            {
                Debug.LogWarning($"[TestAsyncSystem] Process failed - Result: {result} (ODD)");
            }
        }
    }

    /// <summary>
    /// Simulates background work on a separate thread
    /// WARNING: Cannot call Unity APIs from this method!
    /// </summary>
    private int PerformBackgroundWork()
    {
        // Use System.Random instead of UnityEngine.Random (which is main-thread only)
        System.Random random = new System.Random();

        // Simulate work time
        int processingTime = random.Next(_minProcessingTimeMs, _maxProcessingTimeMs);
        Thread.Sleep(processingTime);

        // Generate random numbers and add them together
        int num1 = random.Next(1, 1000);
        int num2 = random.Next(1, 1000);
        int num3 = random.Next(1, 1000);

        int result = num1 + num2 + num3;

        // Note: Cannot use Debug.Log or any Unity APIs here - we're on a background thread!
        // The result will be sent back to the main thread via the coroutine

        return result;
    }

    /// <summary>
    /// Public method to trigger the process manually (can be called from buttons, etc.)
    /// </summary>
    [ContextMenu("Run Test Process")]
    public void RunTestProcess()
    {
        StartAsyncProcess();
    }
}
