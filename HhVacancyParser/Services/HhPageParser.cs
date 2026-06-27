using System.Text.Json;
using HhVacancyParser.Models;

namespace HhVacancyParser.Services;

public sealed class HhPageParser
{
    private const string InitialStateMarker = "id=\"HH-Lux-InitialState\"";

    public string ExtractInitialStateJson(string html)
    {
        var markerIndex = html.IndexOf(InitialStateMarker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            throw new InvalidOperationException(
                "HeadHunter page structure changed: initial state block not found.");
        }

        var jsonStart = html.IndexOf('>', markerIndex);
        if (jsonStart < 0)
        {
            throw new InvalidOperationException("Failed to locate JSON payload on HeadHunter page.");
        }

        jsonStart++;
        var jsonEnd = html.IndexOf("</template>", jsonStart, StringComparison.OrdinalIgnoreCase);
        if (jsonEnd < 0)
        {
            throw new InvalidOperationException("Failed to locate end of JSON payload on HeadHunter page.");
        }

        var json = html[jsonStart..jsonEnd].Trim();
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("HeadHunter returned an empty vacancy payload.");
        }

        return json;
    }

    public (IReadOnlyList<VacancyItem> Vacancies, int TotalFound) ParseSearchPage(string html)
    {
        var json = ExtractInitialStateJson(html);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!root.TryGetProperty("vacancySearchResult", out var searchResult))
        {
            throw new InvalidOperationException(
                "HeadHunter page does not contain vacancySearchResult section.");
        }

        var totalFound = 0;
        if (searchResult.TryGetProperty("totalResults", out var totalElement) &&
            totalElement.ValueKind == JsonValueKind.Number)
        {
            totalFound = totalElement.GetInt32();
        }

        if (!searchResult.TryGetProperty("vacancies", out var vacanciesElement) ||
            vacanciesElement.ValueKind != JsonValueKind.Array)
        {
            return ([], totalFound);
        }

        var vacancies = new List<VacancyItem>();
        foreach (var vacancyElement in vacanciesElement.EnumerateArray())
        {
            var vacancy = ParseVacancy(vacancyElement);
            if (vacancy is not null)
            {
                vacancies.Add(vacancy);
            }
        }

        return (vacancies, totalFound);
    }

    private static VacancyItem? ParseVacancy(JsonElement vacancyElement)
    {
        var name = ReadString(vacancyElement, "name");
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var id = ReadVacancyId(vacancyElement);
        var salary = ParseSalary(vacancyElement);
        var areaName = ParseAreaName(vacancyElement);

        return new VacancyItem
        {
            Id = id,
            Name = name,
            Salary = salary,
            Area = string.IsNullOrWhiteSpace(areaName)
                ? null
                : new AreaInfo { Name = areaName }
        };
    }

    private static SalaryInfo? ParseSalary(JsonElement vacancyElement)
    {
        if (!vacancyElement.TryGetProperty("compensation", out var compensation) ||
            compensation.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        decimal? from = ReadDecimal(compensation, "from");
        decimal? to = ReadDecimal(compensation, "to");
        var currency = ReadString(compensation, "currencyCode");
        var gross = ReadBool(compensation, "gross");

        if (!from.HasValue && !to.HasValue)
        {
            return null;
        }

        return new SalaryInfo
        {
            From = from,
            To = to,
            Currency = currency,
            Gross = gross
        };
    }

    private static string? ParseAreaName(JsonElement vacancyElement)
    {
        if (vacancyElement.TryGetProperty("area", out var area) &&
            area.ValueKind == JsonValueKind.Object)
        {
            var areaName = ReadString(area, "name");
            if (!string.IsNullOrWhiteSpace(areaName))
            {
                return areaName;
            }
        }

        if (vacancyElement.TryGetProperty("address", out var address) &&
            address.ValueKind == JsonValueKind.Object)
        {
            return ReadString(address, "city");
        }

        return null;
    }

    private static string? ReadVacancyId(JsonElement element)
    {
        if (element.TryGetProperty("vacancyId", out var vacancyId) &&
            vacancyId.ValueKind == JsonValueKind.Number)
        {
            return vacancyId.GetInt64().ToString();
        }

        return ReadString(element, "id");
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
    }

    private static decimal? ReadDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) ||
            value.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return value.GetDecimal();
    }

    private static bool? ReadBool(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }
}
