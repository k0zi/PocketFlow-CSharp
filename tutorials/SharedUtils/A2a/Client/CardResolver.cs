using System.Text.Json;

namespace A2aProtocol;

/// <summary>
/// Resolves an agent's <see cref="AgentCard"/> from its well-known URL.
/// Ported from <c>common/client/card_resolver.py</c>.
/// </summary>
public sealed class A2aCardResolver
{
    private static readonly HttpClient _http = new();

    private readonly string _baseUrl;

    /// <param name="baseUrl">Root URL of the remote agent, e.g. <c>http://localhost:10002</c>.</param>
    public A2aCardResolver(string baseUrl) =>
        _baseUrl = baseUrl.TrimEnd('/');

    /// <summary>
    /// Fetches <c>GET {baseUrl}/.well-known/agent.json</c> and deserialises
    /// the response into an <see cref="AgentCard"/>.
    /// </summary>
    public AgentCard GetAgentCard()
    {
        var url      = $"{_baseUrl}/.well-known/agent.json";
        var response = _http.GetAsync(url).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        return JsonSerializer.Deserialize<AgentCard>(json, A2aJsonOptions.Default)
               ?? throw new InvalidOperationException("Failed to deserialise AgentCard.");
    }

    /// <summary>Async version of <see cref="GetAgentCard"/>.</summary>
    public async Task<AgentCard> GetAgentCardAsync()
    {
        var url      = $"{_baseUrl}/.well-known/agent.json";
        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AgentCard>(json, A2aJsonOptions.Default)
               ?? throw new InvalidOperationException("Failed to deserialise AgentCard.");
    }
}

