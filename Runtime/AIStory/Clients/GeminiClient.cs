using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using PlayFab;
using PlayFab.CloudScriptModels;
using UnityEngine;

public class GeminiClient : IAIClient
{
    private readonly GeminiConfig config;
    private readonly Imagen4Service imagenService;
    public Imagen4Service Imagen4Service => imagenService;
    
    public string ProviderName => "Gemini";
    public bool IsConfigured => config != null && config.IsValid();
    
    public GeminiClient(GeminiConfig config)
    {
        this.config = config;
        this.imagenService = new Imagen4Service(config, ExecuteFunctionAsync<Imagen4Service.CloudScriptImageResponse>);
        Debug.Log("GeminiClient Generated:" + this.config);
    }

    [Serializable]
    private class GeminiTextResponse
    {
        public string text;
    }

    [Serializable]
    private class GeminiTextCacheResponse
    {
        public string text;
        public string cacheKey;
        public bool cached;
    }
    
    /// <summary>
    /// Send a "question" and return Gemini's text response
    /// </summary>
    public Task<string> AskAsync(string question, int? timeoutMs = null)
    {
        return AskAsync(question, timeoutMs, CancellationToken.None);
    }

    public async Task<string> AskAsync(string question, int? timeoutMs, CancellationToken cancellationToken)
    {
        var actualTimeout = Math.Max(1, timeoutMs ?? config.DefaultTimeoutMs);
        var response = await ExecuteFunctionAsync<GeminiTextResponse>(
            config.TextFunctionName,
            new
            {
                prompt = question,
                model = config.Model,
                timeoutMs = actualTimeout
            }, actualTimeout, cancellationToken);

        return response?.text ?? string.Empty;
    }

    public Task<string> AskWithCacheAsync(string question, string cacheKey, int? timeoutMs = null)
    {
        return AskWithCacheAsync(question, cacheKey, timeoutMs, CancellationToken.None);
    }

    public async Task<string> AskWithCacheAsync(string question, string cacheKey, int? timeoutMs, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            return await AskAsync(question, timeoutMs, cancellationToken);
        }

        var actualTimeout = Math.Max(1, timeoutMs ?? config.DefaultTimeoutMs);
        var response = await ExecuteFunctionAsync<GeminiTextCacheResponse>(
            config.TextFunctionName,
            new
            {
                prompt = question,
                model = config.Model,
                timeoutMs = actualTimeout,
                cacheKey = cacheKey
            }, actualTimeout, cancellationToken);

        return response?.text ?? string.Empty;
    }

    public Task<Texture2D[]> GeneratePic(string prompt, int? count = null, string aspectRatio = null)
    {
        return GeneratePic(prompt, count, aspectRatio, CancellationToken.None);
    }

    public async Task<Texture2D[]> GeneratePic(string prompt, int? count, string aspectRatio, CancellationToken cancellationToken)
    {
        var actualCount = count ?? 1;
        var actualAspectRatio = aspectRatio ?? "1:1";
        
        var data = await imagenService.GenerateImagesImagenAsync(prompt, actualCount, actualAspectRatio, cancellationToken: cancellationToken);
        return data;
    }

    public Task<Texture2D[]> GeneratePicWithCache(string prompt, string cacheKey, string storyId, int? sceneIndex, int? count = null, string aspectRatio = null)
    {
        return GeneratePicWithCache(prompt, cacheKey, storyId, sceneIndex, count, aspectRatio, CancellationToken.None);
    }

    public async Task<Texture2D[]> GeneratePicWithCache(string prompt, string cacheKey, string storyId, int? sceneIndex, int? count, string aspectRatio, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            return await GeneratePic(prompt, count, aspectRatio, cancellationToken);
        }

        var actualCount = count ?? 1;
        var actualAspectRatio = aspectRatio ?? "1:1";
        var data = await imagenService.GenerateImagesImagenAsync(prompt, actualCount, actualAspectRatio, null, cacheKey, storyId, sceneIndex, cancellationToken);
        return data;
    }

    private async Task<T> ExecuteFunctionAsync<T>(string functionName, object parameters, int timeoutMs, CancellationToken cancellationToken)
    {
        await UniTask.SwitchToMainThread(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        // The payload's timeoutMs only limits server work. Bound the local wait too.
        // SDK requests cannot be individually aborted, so callbacks only complete this
        // gate; a late callback must not parse data, log payloads, or touch Unity state.
        var completion = new TaskCompletionSource<ExecuteFunctionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            deadline.CancelAfter(Math.Max(1, timeoutMs));
            using (deadline.Token.Register(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                    completion.TrySetCanceled(cancellationToken);
                else
                    completion.TrySetException(new TimeoutException($"Azure Function '{functionName}' exceeded the local {timeoutMs}ms deadline."));
            }))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    PlayFabCloudScriptAPI.ExecuteFunction(
                        new ExecuteFunctionRequest
                        {
                            FunctionName = functionName,
                            FunctionParameter = parameters,
                            GeneratePlayStreamEvent = false
                        },
                        result => completion.TrySetResult(result),
                        error =>
                        {
                            if (completion.Task.IsCompleted)
                                return;
                            var message = error?.GenerateErrorReport() ?? "Unknown PlayFab error";
                            completion.TrySetException(new Exception($"Azure Function '{functionName}' failed: {message}"));
                        });
                }
                catch (Exception exception)
                {
                    // Observe this gate even when the SDK rejects the request before
                    // installing callbacks (for example, missing authentication).
                    completion.TrySetException(exception);
                }

                var result = await completion.Task;
                await UniTask.SwitchToMainThread(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (deadline.IsCancellationRequested)
                    throw new TimeoutException($"Azure Function '{functionName}' exceeded the local {timeoutMs}ms deadline.");

                if (result == null)
                    throw new Exception($"Azure Function '{functionName}' returned no response");
                if (result.Error != null)
                {
                    var errorMessage = PlayFab.Json.PlayFabSimpleJson.SerializeObject(result.Error);
                    throw new Exception($"Azure Function '{functionName}' error: {errorMessage}");
                }

                if (result.FunctionResult == null)
                    throw new Exception($"Azure Function '{functionName}' returned no result");

                try
                {
                    var json = PlayFab.Json.PlayFabSimpleJson.SerializeObject(result.FunctionResult);
                    return PlayFab.Json.PlayFabSimpleJson.DeserializeObject<T>(json);
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to parse Azure Function '{functionName}' result: {e.Message}", e);
                }
            }
        }
    }
}
