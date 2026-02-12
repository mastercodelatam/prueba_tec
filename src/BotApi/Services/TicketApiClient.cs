using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BotApi.Models;

namespace BotApi.Services;

public class TicketApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TicketApiClient> _logger;

    private string? _cachedToken;
    private DateTime _tokenExpiration = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public TicketApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<TicketApiClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    private async Task<string> GetTokenAsync()
    {
        await _tokenLock.WaitAsync();
        try
        {
            // Si el token es válido y no está a punto de expirar, usarlo
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiration.AddMinutes(-1))
            {
                return _cachedToken;
            }

            // Obtener nuevo token
            var clientId = _configuration["ExternalApi:ClientId"] ?? "bot-client";
            var clientSecret = _configuration["ExternalApi:ClientSecret"] ?? "bot-secret";

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret)
            });

            var response = await _httpClient.PostAsync("/oauth/token", content);
            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                throw new InvalidOperationException("No se pudo obtener el token de acceso");
            }

            _cachedToken = tokenResponse.AccessToken;
            _tokenExpiration = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

            _logger.LogInformation("Token obtenido exitosamente. Expira en {ExpiresIn} segundos", tokenResponse.ExpiresIn);

            return _cachedToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task InvalidateTokenAsync()
    {
        await _tokenLock.WaitAsync();
        try
        {
            _cachedToken = null;
            _tokenExpiration = DateTime.MinValue;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public async Task<CreateTicketApiResponse?> CreateTicketAsync(string name, string email, string description)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var token = await GetTokenAsync();

            var request = new CreateTicketApiRequest
            {
                Name = name,
                Email = email,
                Description = description
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/tickets")
            {
                Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(httpRequest);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await InvalidateTokenAsync();
                throw new UnauthorizedAccessException("Token inválido o expirado");
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CreateTicketApiResponse>();
        });
    }

    public async Task<TicketStatusApiResponse?> GetTicketStatusAsync(string ticketId)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var token = await GetTokenAsync();

            var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"/tickets/{ticketId}");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(httpRequest);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await InvalidateTokenAsync();
                throw new UnauthorizedAccessException("Token inválido o expirado");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TicketStatusApiResponse>();
        });
    }

    private async Task<T?> ExecuteWithRetryAsync<T>(Func<Task<T?>> action, int maxRetries = 1)
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                return await action();
            }
            catch (UnauthorizedAccessException) when (attempts < maxRetries)
            {
                attempts++;
                _logger.LogWarning("Token inválido, reintentando... Intento {Attempt} de {MaxRetries}", attempts, maxRetries);
                // El token ya fue invalidado, el próximo intento obtendrá uno nuevo
            }
        }
    }
}
