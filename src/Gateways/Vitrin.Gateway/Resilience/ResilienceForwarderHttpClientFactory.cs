using System.Net;
using Yarp.ReverseProxy.Forwarder;

namespace Vitrin.Gateway.Resilience;

/// <summary>
/// YARP'ın IForwarderHttpClientFactory'sini wrap ederek cluster ID'ye göre
/// doğru Polly resilience pipeline'lı HttpClient'ı seçer.
///
/// Cluster → profil eşlemesi:
///   auth-cluster, product-cluster, comment-cluster → Vitrin.Critical
///   vote-cluster                                   → Vitrin.Voting
///   analytics-cluster, notification-cluster,
///   ai-cluster                                     → Vitrin.Tolerant
/// </summary>
public sealed class ResilienceForwarderHttpClientFactory : IForwarderHttpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;

    // Cluster ID → named HttpClient eşlemesi
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

    public ResilienceForwarderHttpClientFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context)
    {
        if (ClusterClientMap.TryGetValue(context.ClusterId, out var clientName))
        {
            // IHttpClientFactory'den Polly resilience pipeline'lı handler al
            return _httpClientFactory.CreateClient(clientName);
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
