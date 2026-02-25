using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Agent_Framework_8_AWeber_Newsletter.Models;

namespace Agent_Framework_8_AWeber_Newsletter.Services
{
    public class AWeberClient
    {
        private const string BaseUrl = "https://api.aweber.com/1.0";
        private const string TokenUrl = "https://auth.aweber.com/oauth2/token";
        private const string TokenFilePath = "aweber_tokens.json";

        private readonly HttpClient _httpClient = new();
        private AWeberTokens _tokens;

        public AWeberClient()
        {
            _tokens = LoadTokens();
        }

        private AWeberTokens LoadTokens()
        {
            var path = GetTokenFilePath();

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"AWeber tokens file not found at '{path}'. " +
                    "Please copy the aweber_tokens.json template and fill in your credentials. " +
                    "See https://labs.aweber.com/ to register your app and obtain tokens.");
            }

            var json = File.ReadAllText(path);
            var tokens = JsonSerializer.Deserialize<AWeberTokens>(json)
                ?? throw new InvalidOperationException("Failed to deserialize aweber_tokens.json");

            if (string.IsNullOrEmpty(tokens.ClientId) || tokens.ClientId == "YOUR_CLIENT_ID_HERE")
            {
                throw new InvalidOperationException(
                    "AWeber credentials not configured. Please edit aweber_tokens.json with your " +
                    "client_id, client_secret, access_token, and refresh_token from https://labs.aweber.com/");
            }

            return tokens;
        }

        private void SaveTokens()
        {
            var path = GetTokenFilePath();
            var json = JsonSerializer.Serialize(_tokens, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        private static string GetTokenFilePath()
        {
            // Check next to the executable first, then current directory
            var exeDir = AppContext.BaseDirectory;
            var exePath = Path.Combine(exeDir, TokenFilePath);
            if (File.Exists(exePath)) return exePath;

            var cwdPath = Path.Combine(Directory.GetCurrentDirectory(), TokenFilePath);
            if (File.Exists(cwdPath)) return cwdPath;

            return exePath;
        }

        private async Task RefreshAccessTokenAsync()
        {
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_tokens.ClientId}:{_tokens.ClientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = _tokens.RefreshToken
            });

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Failed to refresh AWeber token: {response.StatusCode} - {responseBody}");
            }

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            _tokens.AccessToken = root.GetProperty("access_token").GetString() ?? "";
            _tokens.RefreshToken = root.GetProperty("refresh_token").GetString() ?? "";

            SaveTokens();
        }

        private async Task<HttpResponseMessage> SendAuthenticatedRequestAsync(HttpMethod method, string url, HttpContent? content = null)
        {
            // First attempt
            var response = await SendRequestAsync(method, url, content);

            // If unauthorized, refresh token and retry
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await RefreshAccessTokenAsync();
                response = await SendRequestAsync(method, url, content);
            }

            return response;
        }

        private async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, string url, HttpContent? content)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens.AccessToken);

            if (content != null)
                request.Content = content;

            return await _httpClient.SendAsync(request);
        }

        public async Task EnsureAccountAndListAsync()
        {
            if (!string.IsNullOrEmpty(_tokens.AccountId) && !string.IsNullOrEmpty(_tokens.ListId))
                return;

            // Discover account
            var accountResponse = await SendAuthenticatedRequestAsync(HttpMethod.Get, $"{BaseUrl}/accounts");
            accountResponse.EnsureSuccessStatusCode();

            var accounts = await accountResponse.Content.ReadFromJsonAsync<AWeberAccountsResponse>();
            if (accounts?.Entries == null || accounts.Entries.Count == 0)
                throw new InvalidOperationException("No AWeber accounts found.");

            var account = accounts.Entries[0];
            _tokens.AccountId = account.Id.ToString();

            // Discover first list
            var listsUrl = $"{BaseUrl}/accounts/{_tokens.AccountId}/lists";
            var listsResponse = await SendAuthenticatedRequestAsync(HttpMethod.Get, listsUrl);
            listsResponse.EnsureSuccessStatusCode();

            var lists = await listsResponse.Content.ReadFromJsonAsync<AWeberListsResponse>();
            if (lists?.Entries == null || lists.Entries.Count == 0)
                throw new InvalidOperationException("No AWeber lists found. Please create a list in your AWeber account first.");

            _tokens.ListId = lists.Entries[0].Id.ToString();

            SaveTokens();
        }

        public async Task<AWeberBroadcastResponse> CreateBroadcastAsync(string subject, string bodyHtml, string bodyText)
        {
            await EnsureAccountAndListAsync();

            var url = $"{BaseUrl}/accounts/{_tokens.AccountId}/lists/{_tokens.ListId}/broadcasts";

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["subject"] = subject,
                ["body_html"] = bodyHtml,
                ["body_text"] = bodyText
            });

            var response = await SendAuthenticatedRequestAsync(HttpMethod.Post, url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Failed to create AWeber broadcast: {response.StatusCode} - {responseBody}");
            }

            var broadcast = JsonSerializer.Deserialize<AWeberBroadcastResponse>(responseBody)
                ?? throw new InvalidOperationException($"Failed to parse broadcast response: {responseBody}");

            return broadcast;
        }

        public async Task<string> ScheduleBroadcastAsync(string broadcastSelfLink, int minutesFromNow)
        {
            var scheduledTime = DateTime.UtcNow.AddMinutes(minutesFromNow);
            var scheduleUrl = $"{broadcastSelfLink}/schedule";

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["scheduled_for"] = scheduledTime.ToString("yyyy-MM-ddTHH:mm:ssZ")
            });

            var response = await SendAuthenticatedRequestAsync(HttpMethod.Post, scheduleUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Failed to schedule AWeber broadcast: {response.StatusCode} - {responseBody}");
            }

            return scheduledTime.ToString("yyyy-MM-dd HH:mm:ss UTC");
        }
    }
}
