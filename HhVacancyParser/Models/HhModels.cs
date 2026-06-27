using System.Text.Json.Serialization;

namespace HhVacancyParser.Models;

public sealed class VacancySearchResponse
{
    [JsonPropertyName("items")]
    public List<VacancyItem> Items { get; set; } = [];

    [JsonPropertyName("found")]
    public int Found { get; set; }

    [JsonPropertyName("pages")]
    public int Pages { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("per_page")]
    public int PerPage { get; set; }
}

public sealed class VacancyItem
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("salary")]
    public SalaryInfo? Salary { get; set; }

    [JsonPropertyName("area")]
    public AreaInfo? Area { get; set; }
}

public sealed class SalaryInfo
{
    [JsonPropertyName("from")]
    public decimal? From { get; set; }

    [JsonPropertyName("to")]
    public decimal? To { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("gross")]
    public bool? Gross { get; set; }
}

public sealed class AreaInfo
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
