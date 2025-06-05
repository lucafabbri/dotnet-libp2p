using Microsoft.Extensions.Logging;
using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Core.Extensions;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Nethermind.Libp2p.Protocols.RequestResponse;

public interface IRequestResponseSessionProtocol<TRequest, TResponse> : ISessionProtocol<TRequest, TResponse>
{
    Task<TResponse> Handle(TRequest request);
}

public abstract class RequestResponseSessionProtocol<TRequest, TResponse>(
    ILogger Logger,
    JsonSerializerOptions? JsonSerializerOptions = null
    ) : IRequestResponseSessionProtocol<TRequest, TResponse>
{
    public virtual string Id { get; }

    public int MaxMessageSize { get; set; } = 1024 * 1024; // Default to 1 MB

    public JsonSerializerOptions JsonSerializerOptions { get; } = JsonSerializerOptions ?? new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public int TimeoutSeconds { get; set; } = 30;

    private CancellationTokenSource _cancellationTokenSource = new();

    public async Task<TResponse> DialAsync(IChannel downChannel, ISessionContext context, TRequest request)
    {
        var cancellationToken = _cancellationTokenSource.Token;
        _cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds)); // Set a timeout for the request

        await downChannel.WriteAsync(new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request, JsonSerializerOptions))), cancellationToken).OrThrow();

        var responseStr = await downChannel.ReadAllAsync().AsUtf8String();

        TResponse? responseObj = JsonSerializer.Deserialize<TResponse>(responseStr, JsonSerializerOptions);

        if (responseObj is null)
        {
            throw new InvalidOperationException($"Unable to deserialize response: {responseStr}");
        }

        return responseObj;
    }

    public async Task ListenAsync(IChannel downChannel, ISessionContext context)
    {
        var cancellationToken = _cancellationTokenSource.Token;
        _cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds)); // Set a timeout for the request

        var requestStr = await downChannel.ReadAllAsync().AsUtf8String();

        TRequest? requestObj = JsonSerializer.Deserialize<TRequest>(requestStr, JsonSerializerOptions);

        TResponse response = await Handle(requestObj);

        await downChannel.WriteAsync(new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response, JsonSerializerOptions)))).OrThrow();
    }

    public abstract Task<TResponse> Handle(TRequest? request);

}
