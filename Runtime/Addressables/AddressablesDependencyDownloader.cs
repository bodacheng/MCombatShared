using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class AddressablesDependencyDownloader
{
    private static readonly IDictionary<string, long> RequiredDownloadSizes = new Dictionary<string, long>();
    private static readonly IDictionary<string, long> DownloadedByteCounts = new Dictionary<string, long>();

    public static long DownloadedBytes
    {
        get
        {
            long whole = 0;
            foreach (var kv in DownloadedByteCounts)
            {
                whole += kv.Value;
            }

            return whole;
        }
    }

    public static void Reset()
    {
        RequiredDownloadSizes.Clear();
        DownloadedByteCounts.Clear();
    }

    public static async UniTask<long> GetDownloadSize(string label, Action<string> exceptionProcess = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(label))
        {
            return 0;
        }

        AsyncOperationHandle<long> handle = default;
        try
        {
            handle = Addressables.GetDownloadSizeAsync(label);
            await WaitForCompletion(handle, cancellationToken);
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var result = handle.Result;
                if (result > 0)
                {
                    RequiredDownloadSizes[label] = result;
                }

                return result;
            }

            Debug.LogError(AddressablesResourcePolicy.DownloadSizeFailureMessage(label));
            exceptionProcess?.Invoke(label);
            return 0;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Debug.LogError(AddressablesResourcePolicy.DownloadSizeExceptionMessage(label, ex));
            exceptionProcess?.Invoke(label);
            return 0;
        }
        finally
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
    }

    public static async UniTask<long> GetWholeDownloadSize(IEnumerable<string> labels, Action<string> exceptionProcess = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Reset();

        long wholeSize = 0;
        foreach (var label in NormalizeLabels(labels))
        {
            wholeSize += await GetDownloadSize(label, exceptionProcess, cancellationToken);
        }

        return wholeSize;
    }

    public static async UniTask<bool> DownloadDependencies(
        string label,
        Action<string> progressUIRefresh = null,
        string progressText = null,
        CancellationToken cancellationToken = default,
        TimeSpan? idleTimeout = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(label))
        {
            return true;
        }

        AsyncOperationHandle downloadHandle = default;
        try
        {
            // Keep the handle valid until Status and DownloadStatus have been read.
            downloadHandle = Addressables.DownloadDependenciesAsync(label, false);
            long lastBytes = -1;
            double lastProgressAt = Time.realtimeSinceStartupAsDouble;
            while (!downloadHandle.IsDone)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var bytes = downloadHandle.GetDownloadStatus().DownloadedBytes;
                if (bytes != lastBytes)
                {
                    lastBytes = bytes;
                    lastProgressAt = Time.realtimeSinceStartupAsDouble;
                }
                else if (idleTimeout.HasValue && idleTimeout.Value > TimeSpan.Zero &&
                         Time.realtimeSinceStartupAsDouble - lastProgressAt >= idleTimeout.Value.TotalSeconds)
                {
                    throw new TimeoutException($"Resource download made no progress for {idleTimeout.Value.TotalSeconds} seconds: {label}");
                }
                UpdateDownloadedBytes(label, downloadHandle);
                progressUIRefresh?.Invoke(progressText ?? label);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
            {
                UpdateDownloadedBytes(label, downloadHandle);
                return true;
            }

            Debug.LogError(AddressablesResourcePolicy.DownloadMissionFailureMessage(
                label,
                downloadHandle.OperationException));
            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Debug.LogError(AddressablesResourcePolicy.DownloadMissionExceptionMessage(ex));
            return false;
        }
        finally
        {
            if (downloadHandle.IsValid())
            {
                Addressables.Release(downloadHandle);
            }
        }
    }

    public static async UniTask<bool> DownloadRequiredDependencies(
        IEnumerable<string> labels,
        Action<string> progressUIRefresh = null,
        string progressText = null,
        CancellationToken cancellationToken = default,
        TimeSpan? idleTimeout = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using (var groupCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            async UniTask<bool> DownloadLabel(string label)
            {
                try
                {
                    var success = await DownloadDependencies(label, progressUIRefresh, progressText, groupCancellation.Token, idleTimeout);
                    if (!success)
                        groupCancellation.Cancel();
                    return success;
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && groupCancellation.IsCancellationRequested)
                {
                    return false;
                }
            }

            var downloadTasks = new List<UniTask<bool>>();
            foreach (var label in NormalizeLabels(labels))
            {
                AddressablesResourcePolicy.EnsureDownloadedBytesLabel(DownloadedByteCounts, label);
                if (RequiredDownloadSizes.ContainsKey(label))
                    downloadTasks.Add(DownloadLabel(label));
            }

            if (downloadTasks.Count == 0)
                return true;

            var results = await UniTask.WhenAll(downloadTasks);
            cancellationToken.ThrowIfCancellationRequested();
            return results.All(result => result);
        }
    }

    // Poll instead of registering a completion callback: a canceled attempt never
    // resumes when the underlying Addressables operation eventually finishes.
    // The caller still owns and releases its handle on Unity's main thread.
    public static async UniTask WaitForCompletion(AsyncOperationHandle handle, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        while (!handle.IsDone)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static IEnumerable<string> NormalizeLabels(IEnumerable<string> labels)
    {
        return labels == null
            ? Enumerable.Empty<string>()
            : labels.Where(label => !string.IsNullOrEmpty(label)).Distinct();
    }

    private static void UpdateDownloadedBytes(string label, AsyncOperationHandle downloadHandle)
    {
        if (!DownloadedByteCounts.ContainsKey(label))
        {
            return;
        }

        DownloadedByteCounts[label] = downloadHandle.GetDownloadStatus().DownloadedBytes;
    }
}
