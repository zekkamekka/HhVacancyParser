namespace HhVacancyParser.Models;

public sealed class VacancyStatistics
{
    public required string SearchText { get; set; }
    public int? AreaId { get; set; }
    public int TotalCollected { get; set; }
    public int TotalFoundOnHh { get; set; }
    public IReadOnlyList<TitleFrequency> TopTitles { get; set; } = [];
    public SalarySummary SalarySummary { get; set; } = new();
    public IReadOnlyList<CityCount> VacanciesByCity { get; set; } = [];
}

public sealed class TitleFrequency
{
    public required string Title { get; set; }
    public int Count { get; set; }
}

public sealed class SalarySummary
{
    public decimal? AverageSalaryRub { get; set; }
    public int VacanciesWithSalary { get; set; }
    public int VacanciesWithoutSalary { get; set; }
    public int SkippedNonRubSalaries { get; set; }
}

public sealed class CityCount
{
    public required string City { get; set; }
    public int Count { get; set; }
}
