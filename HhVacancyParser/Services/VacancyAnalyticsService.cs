using HhVacancyParser.Models;

namespace HhVacancyParser.Services;

public sealed class VacancyAnalyticsService
{
    private static readonly HashSet<string>         RubCurrencies = new(StringComparer.OrdinalIgnoreCase)
        {
            "RUR",
            "RUB"
        };

    public VacancyStatistics Analyze(
        IReadOnlyList<VacancyItem> vacancies,
        string searchText,
        int? areaId,
        int totalFoundOnHh = 0)
    {
        var topTitles = vacancies
            .Select(v => v.Name?.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .GroupBy(name => name!, StringComparer.OrdinalIgnoreCase)
            .Select(group => new TitleFrequency
            {
                Title = group.Key,
                Count = group.Count()
            })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();

        var salarySummary = CalculateSalarySummary(vacancies);

        var byCity = vacancies
            .Select(v => v.Area?.Name?.Trim())
            .Where(city => !string.IsNullOrWhiteSpace(city))
            .GroupBy(city => city!, StringComparer.OrdinalIgnoreCase)
            .Select(group => new CityCount
            {
                City = group.Key,
                Count = group.Count()
            })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.City, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new VacancyStatistics
        {
            SearchText = searchText,
            AreaId = areaId,
            TotalCollected = vacancies.Count,
            TotalFoundOnHh = totalFoundOnHh,
            TopTitles = topTitles,
            SalarySummary = salarySummary,
            VacanciesByCity = byCity
        };
    }

    private static SalarySummary CalculateSalarySummary(IReadOnlyList<VacancyItem> vacancies)
    {
        var summary = new SalarySummary();
        var salaryValues = new List<decimal>();

        foreach (var vacancy in vacancies)
        {
            var salary = vacancy.Salary;
            if (salary is null)
            {
                summary.VacanciesWithoutSalary++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(salary.Currency) ||
                !RubCurrencies.Contains(salary.Currency))
            {
                summary.SkippedNonRubSalaries++;
                continue;
            }

            var value = ResolveSalaryValue(salary.From, salary.To);
            if (!value.HasValue)
            {
                summary.VacanciesWithoutSalary++;
                continue;
            }

            salaryValues.Add(value.Value);
        }

        summary.VacanciesWithSalary = salaryValues.Count;
        summary.AverageSalaryRub = salaryValues.Count > 0
            ? Math.Round(salaryValues.Average(), 0)
            : null;

        return summary;
    }

    public static decimal? ResolveSalaryValue(decimal? from, decimal? to)
    {
        return (from, to) switch
        {
            (not null, not null) => (from.Value + to.Value) / 2m,
            (not null, null) => from.Value,
            (null, not null) => to.Value,
            _ => null
        };
    }
}
