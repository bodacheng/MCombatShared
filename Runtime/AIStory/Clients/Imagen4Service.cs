using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

public class Imagen4Service
{
    private readonly GeminiConfig config;
    private readonly Func<string, object, int, CancellationToken, Task<CloudScriptImageResponse>> executeFunction;
    
    public Imagen4Service(GeminiConfig config, Func<string, object, Task<CloudScriptImageResponse>> executeFunction)
    {
        this.config = config;
        if (executeFunction != null)
            this.executeFunction = (name, parameters, timeout, cancellation) => executeFunction(name, parameters);
    }

    public Imagen4Service(GeminiConfig config, Func<string, object, int, CancellationToken, Task<CloudScriptImageResponse>> executeFunction)
    {
        this.config = config;
        this.executeFunction = executeFunction;
    }
    
    [Serializable] public class ImagenPrediction { public string bytesBase64Encoded; public string mimeType; public string prompt; }
    [Serializable] public class ImageInfo { public string url; public string mimeType; }
    [Serializable] public class CloudScriptImageResponse
    {
        public ImageInfo[] images;
        public ImagenPrediction[] predictions;
    }

    public async System.Threading.Tasks.Task<Texture2D[]> GenerateImagesImagenAsync(
        string prompt,
        int count = 1,
        string aspect = "1:1",
        int? timeoutMs = null,
        string cacheKey = null,
        string storyId = null,
        int? sceneIndex = null,
        CancellationToken cancellationToken = default)
    {
        if (executeFunction == null)
        {
            throw new Exception("Cloud Function executor is not configured for Imagen4Service");
        }

        await UniTask.SwitchToMainThread(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var actualCount = Mathf.Clamp(count, 1, 4);
        var actualTimeout = Math.Max(1, timeoutMs ?? config.ImageTimeoutMs);
        var imageModel = string.IsNullOrEmpty(config.ImageModel) 
            ? "imagen-4.0-generate-preview-06-06" 
            : config.ImageModel;
        var cloudScriptPayload = new
        {
            prompt = prompt,
            sampleCount = actualCount,
            aspectRatio = aspect,
            imageModel = imageModel,
            timeoutMs = actualTimeout,
            cacheKey = cacheKey,
            storyId = storyId,
            sceneIndex = sceneIndex
        };

        // One real-time budget covers cloud execution and all CDN downloads.
        // Per-URL timeouts alone could multiply the player's total wait.
        var elapsed = Stopwatch.StartNew();
        var textures = new List<Texture2D>();
        bool transferred = false;
        using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            deadline.CancelAfter(actualTimeout);
            try
            {
                var responseTask = executeFunction(config.ImageFunctionName, cloudScriptPayload, actualTimeout, deadline.Token);
                var response = await AwaitResponse(responseTask, deadline.Token);
                await UniTask.SwitchToMainThread();
                ThrowIfExpired(deadline.Token, cancellationToken, elapsed, actualTimeout);

                // Prefer CDN/Blob URLs if provided.
                if (response?.images != null && response.images.Length > 0)
                {
                    foreach (var info in response.images)
                    {
                        ThrowIfExpired(deadline.Token, cancellationToken, elapsed, actualTimeout);
                        if (info == null || string.IsNullOrEmpty(info.url))
                            continue;

                        try
                        {
                            var remaining = Math.Max(1, actualTimeout - (int)elapsed.ElapsedMilliseconds);
                            var data = await DownloadTexture(info.url, remaining, deadline.Token);
                            if (data != null)
                                textures.Add(data);
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            // URLs can contain signed access credentials.
                            Debug.LogWarning($"Failed to download generated image: {ex.Message}");
                        }
                    }

                    ThrowIfExpired(deadline.Token, cancellationToken, elapsed, actualTimeout);
                    if (textures.Count > 0)
                    {
                        transferred = true;
                        return textures.ToArray();
                    }
                }

                if (response?.predictions == null || response.predictions.Length == 0)
                    throw new Exception("No predictions returned from Gemini CloudScript");

                foreach (var prediction in response.predictions)
                {
                    ThrowIfExpired(deadline.Token, cancellationToken, elapsed, actualTimeout);
                    if (prediction == null || string.IsNullOrEmpty(prediction.bytesBase64Encoded))
                        continue;

                    Texture2D texture = null;
                    try
                    {
                        var bytes = Convert.FromBase64String(prediction.bytesBase64Encoded);
                        texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (texture.LoadImage(bytes))
                        {
                            textures.Add(texture);
                            texture = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to decode Gemini image prediction: {ex.Message}");
                    }
                    finally
                    {
                        if (texture != null)
                            UnityEngine.Object.Destroy(texture);
                    }
                }

                ThrowIfExpired(deadline.Token, cancellationToken, elapsed, actualTimeout);
                if (textures.Count == 0)
                    throw new Exception("All images failed to decode.");

                transferred = true;
                return textures.ToArray();
            }
            catch (OperationCanceledException)
            {
                ThrowIfExpired(deadline.Token, cancellationToken, elapsed, actualTimeout);
                throw;
            }
            finally
            {
                // Cancellation or a failed later download must not leak earlier textures.
                await UniTask.SwitchToMainThread();
                if (!transferred)
                    foreach (var texture in textures)
                        if (texture != null)
                            UnityEngine.Object.Destroy(texture);
            }
        }
    }

    private static void ThrowIfExpired(CancellationToken deadline, CancellationToken cancellation, Stopwatch elapsed, int timeoutMs)
    {
        cancellation.ThrowIfCancellationRequested();
        if (deadline.IsCancellationRequested || elapsed.ElapsedMilliseconds >= timeoutMs)
            throw new TimeoutException($"Image generation exceeded the local {timeoutMs}ms deadline.");
    }

    private static async Task<CloudScriptImageResponse> AwaitResponse(Task<CloudScriptImageResponse> response, CancellationToken cancellationToken)
    {
        if (response == null)
            throw new Exception("Cloud Function executor returned no task");

        var canceled = new TaskCompletionSource<CloudScriptImageResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (cancellationToken.Register(() => canceled.TrySetCanceled(cancellationToken)))
        {
            var winner = await Task.WhenAny(response, canceled.Task);
            if (winner != response)
            {
                // The legacy/injected executor may not support cancellation. Observe a
                // late failure without running image decoding or touching Unity objects.
                _ = response.ContinueWith(task => { var ignored = task.Exception; },
                    CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
            return await winner;
        }
    }

    private async Task<Texture2D> DownloadTexture(string url, int timeoutMs, CancellationToken cancellationToken)
    {
        await UniTask.SwitchToMainThread(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        using (var req = UnityWebRequestTexture.GetTexture(url))
        {
            req.timeout = Math.Max(1, (int)Math.Ceiling(timeoutMs / 1000d));
            var op = req.SendWebRequest();
            try
            {
                while (!op.isDone)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    // Resume on Unity's main thread; cancellation timers never call Abort.
                    await UniTask.Yield(PlayerLoopTiming.Update);
                }
                cancellationToken.ThrowIfCancellationRequested();

#if UNITY_2020_2_OR_NEWER
                if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isHttpError || req.isNetworkError)
#endif
                    throw new Exception(req.error);

                return DownloadHandlerTexture.GetContent(req);
            }
            finally
            {
                if (!op.isDone)
                    req.Abort();
            }
        }
    }
}
