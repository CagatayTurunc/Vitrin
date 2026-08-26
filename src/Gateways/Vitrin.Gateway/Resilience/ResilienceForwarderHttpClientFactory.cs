using System.Net;
using Microsoft.Extensions.Http.Resilience;
using Yarp.ReverseProxy.Forwarder;

namespace Vitrin.Gateway.Resilience;

/// <summary>
/// YARP'ın IForwarderHttpClientFactory'sini wrap ederek cluster ID'ye göre
/// doğru Polly resilience pipeline'lı HttpMessageInvoker döndürür.
///
/// Not: YARP, CreateClient'tan HttpClient değil HttpMessageInvoker bekler.
/// HttpClient, HttpMessageInvoker'dan türese de YARP tip kontrolü yapar ve
/// HttpClient gelirse ArgumentException fırlatır. Bu yüzden HttpClientFactory
/// yerine HttpMessageHandlerFactory kullanılır.
/// </summary>
public sealed class ResilienceForwarderHttpClientFactory : IForwarderHttpClientFactory
{
    private readonly IHttpMessageHandlerFactory _handlerFactory;

    private static readonly Dictionary<string, string> ClusterClientMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["auth-cluster"]         = ResilienceClientNames.Critical,
            ["product-cluster"]      = ResilienceClientNames.Critical,
            ["comment-cluster"]      = ResilienceClientNames.Critical,
            ["vote-cluster"]         = ResilienceClientNames.Voting,
            ["analytics-cluster"]    = ResilienceClientNames.Tolerant,
            ["notification-cluster"] = ResilienceClientNames.Tolerant,
            ["ai-cluster"]           = ResilienceClientNames.Tolerant,
        };

    public ResilienceForwarderHttpClientFactory(IHttpMessageHandlerFactory handlerFactory)
    {
        _handlerFactory = handlerFactory;
    }

    public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context)
    {
        if (ClusterClientMap.TryGetValue(context.ClusterId, out var clientName))
        {
            // IHttpMessageHandlerFactory → HttpMessageHandler → HttpMessageInvoker
            // YARP'ın beklediği tip tam olarak bu
            var handler = _handlerFactory.CreateHandler(clientName);
            return new HttpMessageInvoker(handler, disposeHandler: true);
        }

        // Bilinmeyen cluster → YARP varsayılan davranışıyla aynı handler
        return new HttpMessageInvoker(new SocketsHttpHandler
        {
            UseProxy               = false,
            AllowAutoRedirect      = false,
            AutomaticDecompression = DecompressionMethods.None,
            UseCookies             = false,
            ConnectTimeout         = TimeSpan.FromSeconds(15),
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        });
    }
}
