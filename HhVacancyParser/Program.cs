using HhVacancyParser.Models;
using HhVacancyParser.Services;

const int defaultCount = 200;
const int defaultAreaId = 113; // Russia

try
{
    var options = ParseArguments(args);
    PrintHelpIfRequested(options);

    using var apiClient = new HhApiClient();
    var analytics = new VacancyAnalyticsService();
    var exporter = new ReportExporter();

    Console.WriteLine($"Loading up to {options.Count} vacancies for \"{options.SearchText}\" from hh.ru...");

    IReadOnlyList<VacancyItem> vacancies;
    int totalFound;
    try
    {
        (vacancies, totalFound) = await apiClient.FetchVacanciesAsync(
            options.SearchText,
            options.AreaId,
            options.Count);
    }
    catch (Exception ex) when (ex is InvalidOperationException or TimeoutException)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"API error: {ex.Message}");
        Console.ResetColor();
        return 1;
    }

    if (vacancies.Count == 0)
    {
        Console.WriteLine("No vacancies found for the specified query.");
        return 0;
    }

    var statistics = analytics.Analyze(
        vacancies,
        options.SearchText,
        options.AreaId,
        totalFound);

    exporter.PrintToConsole(statistics);

    if (options.SaveCsv)
    {
        var csvPath = exporter.SaveToCsv(statistics, options.OutputDirectory);
        Console.WriteLine($"CSV report saved: {csvPath}");
    }

    return 0;
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Unexpected error: {ex.Message}");
    Console.ResetColor();
    return 1;
}

static AppOptions ParseArguments(string[] args)
{
    var options = new AppOptions
    {
        SearchText = ".NET developer",
        AreaId = defaultAreaId,
        Count = defaultCount,
        SaveCsv = true
    };

    for (var i = 0; i < args.Length; i++)
    {
        var arg = args[i];

        switch (arg.ToLowerInvariant())
        {
            case "--text":
            case "-t":
                options.SearchText = ReadNextValue(args, ref i, arg);
                break;
            case "--area":
            case "-a":
                options.AreaId = int.Parse(ReadNextValue(args, ref i, arg), System.Globalization.CultureInfo.InvariantCulture);
                break;
            case "--all-areas":
                options.AreaId = null;
                break;
            case "--count":
            case "-c":
                options.Count = int.Parse(ReadNextValue(args, ref i, arg), System.Globalization.CultureInfo.InvariantCulture);
                break;
            case "--no-csv":
                options.SaveCsv = false;
                break;
            case "--output":
            case "-o":
                options.OutputDirectory = ReadNextValue(args, ref i, arg);
                break;
            case "--help":
            case "-h":
                options.ShowHelp = true;
                break;
            default:
                if (!arg.StartsWith('-'))
                {
                    options.SearchText = arg;
                }
                else
                {
                    throw new ArgumentException($"Unknown argument: {arg}");
                }

                break;
        }
    }

    return options;
}

static string ReadNextValue(string[] args, ref int index, string currentArg)
{
    if (index + 1 >= args.Length)
    {
        throw new ArgumentException($"Value is missing for argument '{currentArg}'.");
    }

    index++;
    return args[index];
}

static void PrintHelpIfRequested(AppOptions options)
{
    if (!options.ShowHelp)
    {
        return;
    }

    Console.WriteLine("""
        HeadHunter vacancy parser

        Usage:
          dotnet run -- --text ".NET developer" --count 200
          dotnet run -- "Python developer"

        Options:
          -t, --text <query>     Search query (default: ".NET developer")
          -a, --area <id>        Region id from hh.ru (default: 113 = Russia)
              --all-areas        Search in all regions
          -c, --count <n>        Number of vacancies to collect (default: 200)
          -o, --output <path>    Folder for CSV report
              --no-csv           Do not save CSV file
          -h, --help             Show this help

        Examples for practice screenshots:
          dotnet run -- --text ".NET developer"
          dotnet run -- --text "Python developer"
        """);

    Environment.Exit(0);
}

sealed class AppOptions
{
    public required string SearchText { get; set; }
    public int? AreaId { get; set; }
    public int Count { get; set; }
    public bool SaveCsv { get; set; }
    public string? OutputDirectory { get; set; }
    public bool ShowHelp { get; set; }
}
