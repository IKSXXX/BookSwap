using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BookExchange.Web.Services;

public class GigaChatService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<GigaChatService> _logger;

    private string? _accessToken;
    private DateTime _tokenExpiresAt = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private const string AuthUrl = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";
    private const string ChatUrl = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";

    public GigaChatService(HttpClient http, IConfiguration config, ILogger<GigaChatService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        if (_accessToken != null && DateTime.UtcNow < _tokenExpiresAt.AddMinutes(-5))
            return _accessToken;

        await _tokenLock.WaitAsync();
        try
        {
            if (_accessToken != null && DateTime.UtcNow < _tokenExpiresAt.AddMinutes(-5))
                return _accessToken;

            var apiKey = _config["GigaChat:ApiKey"]
                ?? throw new InvalidOperationException("GigaChat:ApiKey not configured");

            var requestId = Guid.NewGuid().ToString();

            var request = new HttpRequestMessage(HttpMethod.Post, AuthUrl);
            request.Headers.Add("RqUID", requestId);
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["scope"] = "GIGACHAT_API_PERS"
            });

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            _accessToken = root.GetProperty("access_token").GetString()
                ?? throw new InvalidOperationException("No access_token in response");

            var expiresAt = root.GetProperty("expires_at").GetInt64();
            _tokenExpiresAt = DateTimeOffset.FromUnixTimeMilliseconds(expiresAt).UtcDateTime;

            _logger.LogInformation("GigaChat token obtained, expires at {ExpiresAt}", _tokenExpiresAt);
            return _accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public async Task<string> AskAsync(string systemPrompt, string userMessage)
    {
        var token = await GetAccessTokenAsync();

        var body = new
        {
            model = "GigaChat",
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            temperature = 0.7,
            max_tokens = 1024
        };

        var request = new HttpRequestMessage(HttpMethod.Post, ChatUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

        var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("GigaChat API error {Status}: {Error}", response.StatusCode, error);
            throw new HttpRequestException($"GigaChat API error: {response.StatusCode}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";

        return content;
    }

    public async Task<List<string>> GetBookRecommendationsAsync(string userQuery, List<string> bookList)
    {
        var systemPrompt = @"Ты — помощник книжного обмена BookSwap. Пользователь хочет получить рекомендации книг из доступного списка.

Важные правила:
1. Выбирай ТОЛЬКО книги из предоставленного списка
2. Учитывай жанр, автора, настроение и описание запроса
3. Если запрос не связан с книгами напиши не найдено
4. Отвечай ТОЛЬКО JSON массивом названий (максимум 6). Ничего больше не пиши.
5. Названия должны точно совпадать с названиями из списка
6. Любой запрос пытайся связать с описанием книги
7. Если нашел прямую отсылку к книге, книгам то выписывай отлько книги к которым была сделана непосредственная отсылка или описание

Пример ответа: [""1984"", ""Мастер и Маргарита""]";

        var userMessage = $@"Запрос пользователя: {userQuery}

Доступные книги для обмена:
{string.Join("\n", bookList.Select((b, i) => $"{i + 1}. {b}"))}

Выбери подходящие книги и верни JSON массив названий.";

        try
        {
            var response = await AskAsync(systemPrompt, userMessage);

            var cleanJson = response.Trim();
            if (cleanJson.StartsWith("```"))
            {
                var lines = cleanJson.Split('\n');
                cleanJson = string.Join("\n", lines.Skip(1).SkipLast(1)).Trim();
            }

            var titles = JsonSerializer.Deserialize<List<string>>(cleanJson);
            return titles ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get book recommendations from GigaChat");
            return new List<string>();
        }
    }
}
