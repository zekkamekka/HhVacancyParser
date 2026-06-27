using System.Globalization;
using System.Text;
using HhVacancyParser.Models;

namespace HhVacancyParser.Services;

public sealed class ReportExporter
{
    public void PrintToConsole(VacancyStatistics statistics)
    {
        var separator = new string('=', 70);

        Console.WriteLine();
        Console.WriteLine(separator);
        Console.WriteLine($"HeadHunter vacancy report: \"{statistics.SearchText}\"");
        if (statistics.AreaId.HasValue)
        {
            Console.WriteLine($"Region (area id): {statistics.AreaId.Value}");
        }
        else
        {
            Console.WriteLine("Region: all areas");
        }

        Console.WriteLine($"Collected vacancies: {statistics.TotalCollected}");
        if (statistics.TotalFoundOnHh > 0)
        {
            Console.WriteLine($"Total found on hh.ru: {statistics.TotalFoundOnHh}");
        }

        Console.WriteLine(separator);
        Console.WriteLine("Top-10 job titles:");
        if (statistics.TopTitles.Count == 0)
        {
            Console.WriteLine("  (no data)");
        }
        else
        {
            var rank = 1;
            foreach (var item in statistics.TopTitles)
            {
                Console.WriteLine($"  {rank,2}. {item.Title} — {item.Count}");
                rank++;
            }
        }

        Console.WriteLine();
        Console.WriteLine("Average salary (RUB, from/to midpoint):");
        var salary = statistics.SalarySummary;
        if (salary.AverageSalaryRub.HasValue)
        {
            Console.WriteLine($"  {salary.AverageSalaryRub.Value:N0} RUB");
        }
        else
        {
            Console.WriteLine("  not enough salary data");
        }

        Console.WriteLine($"  with salary: {salary.VacanciesWithSalary}");
        Console.WriteLine($"  without salary: {salary.VacanciesWithoutSalary}");
        if (salary.SkippedNonRubSalaries > 0)
        {
            Console.WriteLine($"  skipped (non-RUB): {salary.SkippedNonRubSalaries}");
        }

        Console.WriteLine();
        Console.WriteLine("Vacancies by city:");
        if (statistics.VacanciesByCity.Count == 0)
        {
            Console.WriteLine("  (no data)");
        }
        else
        {
            foreach (var city in statistics.VacanciesByCity)
            {
                Console.WriteLine($"  {city.City}: {city.Count}");
            }
        }

        Console.WriteLine(separator);
        Console.WriteLine();
    }

    public string SaveToCsv(VacancyStatistics statistics, string? outputDirectory = null)
    {
        outputDirectory ??= Directory.GetCurrentDirectory();
        Directory.CreateDirectory(outputDirectory);

        var safeQuery = SanitizeFileName(statistics.SearchText);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var filePath = Path.Combine(outputDirectory, $"hh_report_{safeQuery}_{timestamp}.csv");

        var lines = new List<string>
        {
            "Section,Key,Value"
        };

        lines.Add($"Summary,SearchText,\"{EscapeCsv(statistics.SearchText)}\"");
        lines.Add($"Summary,AreaId,{statistics.AreaId?.ToString(CultureInfo.InvariantCulture) ?? "all"}");
        lines.Add($"Summary,Collected,{statistics.TotalCollected}");
        lines.Add($"Summary,TotalFoundOnHh,{statistics.TotalFoundOnHh}");

        var salary = statistics.SalarySummary;
        lines.Add($"Salary,AverageRub,{salary.AverageSalaryRub?.ToString(CultureInfo.InvariantCulture) ?? ""}");
        lines.Add($"Salary,WithSalary,{salary.VacanciesWithSalary}");
        lines.Add($"Salary,WithoutSalary,{salary.VacanciesWithoutSalary}");
        lines.Add($"Salary,SkippedNonRub,{salary.SkippedNonRubSalaries}");

        var rank = 1;
        foreach (var title in statistics.TopTitles)
        {
            lines.Add($"TopTitle,{rank},\"{EscapeCsv(title.Title)}\"");
            lines.Add($"TopTitleCount,{rank},{title.Count}");
            rank++;
        }

        foreach (var city in statistics.VacanciesByCity)
        {
            lines.Add($"City,\"{EscapeCsv(city.City)}\",{city.Count}");
        }

        File.WriteAllLines(filePath, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        return filePath;
    }

    private static string EscapeCsv(string value) =>
        value.Replace("\"", "\"\"", StringComparison.Ordinal);

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);

        foreach (var ch in value)
        {
            builder.Append(invalidChars.Contains(ch) ? '_' : ch);
        }

        var result = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(result) ? "report" : result;
    }
}
