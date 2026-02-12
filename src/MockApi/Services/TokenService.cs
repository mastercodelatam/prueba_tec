using System.Collections.Concurrent;

namespace MockApi.Services;

public class TokenService
{
    private const string ValidClientId = "bot-client";
    private const string ValidClientSecret = "bot-secret";
    private readonly ConcurrentDictionary<string, DateTime> _validTokens = new();

    public (bool IsValid, string? Token, int ExpiresIn) GenerateToken(string clientId, string clientSecret)
    {
        if (clientId != ValidClientId || clientSecret != ValidClientSecret)
        {
            return (false, null, 0);
        }

        var token = Guid.NewGuid().ToString("N");
        var expiresIn = 3600; // 1 hora
        _validTokens[token] = DateTime.UtcNow.AddSeconds(expiresIn);

        return (true, token, expiresIn);
    }

    public bool ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        // Remover el prefijo "Bearer " si existe
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            token = token.Substring(7);

        if (_validTokens.TryGetValue(token, out var expirationTime))
        {
            if (DateTime.UtcNow < expirationTime)
                return true;

            // Token expirado, removerlo
            _validTokens.TryRemove(token, out _);
        }

        return false;
    }
}
