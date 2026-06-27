using HhVacancyParser.Models;

namespace HhVacancyParser.Services;

public sealed class HhApiClient : IDisposable
{
    private const string SearchBaseUrl = "https://hh.ru/search/vacancy";
    private const int ItemsPerPage = 50;
    private const int RequestDelayMs = 500;

    private readonly HttpClient _httpClient;
    private readonly HhPageParser _pageParser;
    private readonly bool _ownsHttpClient;

    public HhApiClient(HttpClient? httpClient = null, HhPageParser? pageParser = null)
    {
        if (httpClient is null)
        {
            _httpClient = CreateHttpClient();
            _ownsHttpClient = true;
        }
        else
        {
            _httpClient = httpClient;
        }

        _pageParser = pageParser ?? new HhPageParser();
    }

    public async Task<(IReadOnlyList<VacancyItem> Vacancies, int TotalFound)> FetchVacanciesAsync(
        string searchText,
        int? areaId,
        int targetCount,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            throw new ArgumentException("Search text cannot be empty.", nameof(searchText));
        }

        if (targetCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetCount), "Target count must be greater than zero.");
        }

        var collected = new List<VacancyItem>(Math.Min(targetCount, ItemsPerPage));
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var page = 0;
        var totalFound = 0;
        var totalPages = int.MaxValue;

        while (collected.Count < targetCount && page < totalPages)
        {
            var html = await DownloadSearchPageAsync(searchText, areaId, page, cancellationToken);
            var (pageVacancies, pageTotalFound) = _pageParser.ParseSearchPage(html);

            if (pageTotalFound > 0)
            {
                totalFound = pageTotalFound;
            }

            if (pageVacancies.Count == 0)
            {
                break;
            }

            foreach (var vacancy in pageVacancies)
            {
                var key = vacancy.Id ?? vacancy.Name ?? Guid.NewGuid().ToString("N");
                if (!seenIds.Add(key))
                {
                    continue;
                }

                collected.Add(vacancy);
                if (collected.Count >= targetCount)
                {
                    break;
                }
            }

            totalPages = totalFound > 0
                ? (int)Math.Ceiling(totalFound / (double)ItemsPerPage)
                : page + 1;

            if (pageVacancies.Count < ItemsPerPage)
            {
                break;
            }

            page++;
            await Task.Delay(RequestDelayMs, cancellationToken);
        }

        return (collected, totalFound);
    }

    private async Task<string> DownloadSearchPageAsync(
        string searchText,
        int? areaId,
        int page,
        CancellationToken cancellationToken)
    {
        var query =
            $"text={Uri.EscapeDataString(searchText)}" +
            $"&page={page}";

        if (areaId.HasValue)
        {
            query += $"&area={areaId.Value}";
        }

        var url = $"{SearchBaseUrl}?{query}";

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(url, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                "Failed to connect to hh.ru. Check your internet connection.",
                ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("hh.ru request timed out.", ex);
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"hh.ru returned {(int)response.StatusCode} ({response.ReasonPhrase}). " +
                $"Response: {TrimForDisplay(body)}");
        }

        if (body.Contains("Произошла ошибка", StringComparison.Ordinal) &&
            !body.Contains("vacancySearchResult", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "HeadHunter returned an error page instead of search results.");
        }

        return body;
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "ru-RU,ru;q=0.9,en;q=0.8");
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

        return client;
    }

    private static string TrimForDisplay(string value)
    {
        const int maxLength = 300;
        var normalized = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength] + "...";
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}
