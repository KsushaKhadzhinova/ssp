using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Lab4;

// Call Tree с горячими путями (код из методички)
public static class CallTree
{
    private static List<string> _logs = new List<string>();
    private static Random _rnd = new Random(1);

    // Счетчики вызовов для построения Call Tree
    private static Dictionary<string, int> _callCounts = new Dictionary<string, int>();
    private static Dictionary<string, double> _callTimes = new Dictionary<string, double>();
    private static Stopwatch _globalSw = new Stopwatch();

    // false: DateTime.TryParse (код из методички), true: DateTime.ParseExact (задание 2)
    private static bool _parseExact;

    public static void Run(bool parseExact)
    {
        _parseExact = parseExact;
        _logs.Clear();
        _callCounts.Clear();
        _callTimes.Clear();
        _globalSw.Restart();

        GenerateData(10_000);
        var result = ProcessLogs();

        _globalSw.Stop();

        Console.WriteLine($"Найдено аномалий: {result.Count}, Общее время: {_globalSw.ElapsedMilliseconds} мс\n");

        // Выводим Call Tree
        PrintCallTree();
    }

    private static void GenerateData(int count)
    {
        var sw = Stopwatch.StartNew();

        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        for (int i = 0; i < count; i++)
        {
            var sb = new StringBuilder();
            sb.Append($"USER{_rnd.Next(1, 100)}: ");
            sb.Append($"{DateTime.Now.AddMinutes(-_rnd.Next(0, 10000)):yyyy-MM-dd HH:mm:ss.fff} | ");
            sb.Append(_rnd.Next(0, 4) switch
            {
                0 => "INFO | ",
                1 => "WARNING | ",
                2 => "ERROR | ",
                _ => "DEBUG | "
            });
            for (int j = 0; j < 50; j++)
                sb.Append(chars[_rnd.Next(chars.Length)]);
            sb.Append($" | Code: {_rnd.Next(100, 999)}");
            _logs.Add(sb.ToString());
        }
        for (int i = 0; i < 100; i++)
            _logs.Add($"ANOMALY: {Guid.NewGuid()} | CRITICAL | Ошибка");

        sw.Stop();
        AddCall("GenerateData", sw.Elapsed.TotalMilliseconds);
    }

    private static List<string> ProcessLogs()
    {
        var sw = Stopwatch.StartNew();
        var anomalies = new List<string>();

        // Уровень 1: Фильтрация
        var filterSw = Stopwatch.StartNew();
        var filtered = new List<string>();
        foreach (var log in _logs)
        {
            if (log.IndexOf("ERROR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                log.IndexOf("WARNING", StringComparison.OrdinalIgnoreCase) >= 0 ||
                log.IndexOf("CRITICAL", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                filtered.Add(log);
            }
        }
        filterSw.Stop();
        AddCall("FilterLogs (IndexOf)", filterSw.Elapsed.TotalMilliseconds);

        // Уровень 2: Анализ аномалий (САМЫЙ ГОРЯЧИЙ)
        var anomalySw = Stopwatch.StartNew();
        foreach (var log in filtered)
        {
            if (IsAnomaly(log))
                anomalies.Add(log);
        }
        anomalySw.Stop();
        AddCall("IsAnomaly (total with children)", anomalySw.Elapsed.TotalMilliseconds);

        sw.Stop();
        AddCall("ProcessLogs", sw.Elapsed.TotalMilliseconds);
        return anomalies;
    }

    private static bool IsAnomaly(string log)
    {
        var methodSw = Stopwatch.StartNew();
        bool result = false;

        // Шаг 1: Проверка длины (быстро)
        if (log.Length < 100)
        {
            methodSw.Stop();
            AddCall("IsAnomaly.CheckLength", methodSw.Elapsed.TotalMilliseconds);
            return false;
        }

        // Шаг 2: String.Split (средняя тяжесть)
        var splitSw = Stopwatch.StartNew();
        var parts = log.Split('|');
        splitSw.Stop();
        AddCall("IsAnomaly.String.Split", splitSw.Elapsed.TotalMilliseconds);

        if (parts.Length < 3)
        {
            methodSw.Stop();
            AddCall("IsAnomaly", methodSw.Elapsed.TotalMilliseconds);
            return false;
        }

        // Шаг 3: разбор даты (тяжелый). Дата находится в первой части строки после "USERnn:"
        string dateText = parts[0].Substring(parts[0].IndexOf(':') + 1).Trim();
        var dateSw = Stopwatch.StartNew();
        DateTime logDate = default;
        bool dateOk;
        if (_parseExact)
        {
            // Задание 2: DateTime.ParseExact с фиксированным форматом
            try
            {
                logDate = DateTime.ParseExact(dateText, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                dateOk = true;
            }
            catch (FormatException)
            {
                dateOk = false;
            }
        }
        else
        {
            dateOk = DateTime.TryParse(dateText, out logDate);
        }
        dateSw.Stop();
        AddCall(_parseExact ? "IsAnomaly.DateTime.ParseExact" : "IsAnomaly.DateTime.TryParse", dateSw.Elapsed.TotalMilliseconds);

        if (!dateOk)
        {
            methodSw.Stop();
            AddCall("IsAnomaly", methodSw.Elapsed.TotalMilliseconds);
            return false;
        }

        // Шаг 4: Regex (САМЫЙ ТЯЖЕЛЫЙ!)
        var regexSw = Stopwatch.StartNew();
        var codeMatch = Regex.Match(parts.Length > 3 ? parts[3] : "", @"Code:\s*(\d+)");
        var code = codeMatch.Success && int.TryParse(codeMatch.Groups[1].Value, out int c) ? c : 0;
        regexSw.Stop();
        AddCall("IsAnomaly.Regex.Match", regexSw.Elapsed.TotalMilliseconds);

        // Шаг 5: Проверки (легкие)
        var checkSw = Stopwatch.StartNew();
        var hasAnomaly = log.Contains("ANOMALY", StringComparison.OrdinalIgnoreCase);
        var isRecent = logDate > DateTime.Now.AddDays(-1);
        result = (hasAnomaly && isRecent) || (code > 500 && isRecent) || (hasAnomaly && code > 500);
        checkSw.Stop();
        AddCall("IsAnomaly.Checks", checkSw.Elapsed.TotalMilliseconds);

        methodSw.Stop();
        AddCall("IsAnomaly (own time)", methodSw.Elapsed.TotalMilliseconds);
        return result;
    }

    private static void AddCall(string methodName, double timeMs)
    {
        if (!_callCounts.ContainsKey(methodName))
        {
            _callCounts[methodName] = 0;
            _callTimes[methodName] = 0;
        }
        _callCounts[methodName]++;
        _callTimes[methodName] += timeMs;
    }

    private static double Time(string name) => _callTimes.TryGetValue(name, out var t) ? t : 0;

    private static void PrintCallTree()
    {
        Console.WriteLine("=================== CALL TREE (CPU TIME) ===================\n");

        double totalTime = _globalSw.Elapsed.TotalMilliseconds;
        string dateKey = _parseExact ? "IsAnomaly.DateTime.ParseExact" : "IsAnomaly.DateTime.TryParse";

        // Корень
        PrintNode("Main", totalTime, totalTime);

        // Первый уровень
        PrintNode("  |-- GenerateData", Time("GenerateData"), totalTime);
        PrintNode("  `-- ProcessLogs", Time("ProcessLogs"), totalTime);

        // Второй уровень (дети ProcessLogs)
        PrintNode("      |-- FilterLogs (IndexOf)", Time("FilterLogs (IndexOf)"), Time("ProcessLogs"));
        PrintNode("      `-- IsAnomaly (total with children)", Time("IsAnomaly (total with children)"), Time("ProcessLogs"));

        // Третий уровень (дети IsAnomaly)
        double isAnomalyTotal = Time("IsAnomaly (total with children)");
        PrintNode("          |-- IsAnomaly.String.Split", Time("IsAnomaly.String.Split"), isAnomalyTotal);
        PrintNode("          |-- " + dateKey, Time(dateKey), isAnomalyTotal);
        PrintNode("          |-- IsAnomaly.Regex.Match", Time("IsAnomaly.Regex.Match"), isAnomalyTotal);
        PrintNode("          |-- IsAnomaly.Checks", Time("IsAnomaly.Checks"), isAnomalyTotal);
        PrintNode("          `-- IsAnomaly (own time)", Time("IsAnomaly (own time)"), isAnomalyTotal);

        Console.WriteLine("\n=================== Горячий путь (Hot Path) ===================");
        Console.WriteLine("Main -> ProcessLogs -> IsAnomaly -> " + (Time("IsAnomaly.Regex.Match") > Time(dateKey) ? "Regex.Match" : dateKey));

        Console.WriteLine($"\nСтатистика вызовов:");
        foreach (var kvp in _callCounts)
            Console.WriteLine($"   {kvp.Key}: {kvp.Value} вызовов, {_callTimes[kvp.Key]:F1} мс");
    }

    private static void PrintNode(string name, double time, double parentTime)
    {
        double percent = parentTime > 0 ? time / parentTime * 100 : 0;

        // Создаем визуальную гистограмму
        int barLength = (int)(percent / 2);
        string bar = new string('#', Math.Min(barLength, 50));

        Console.WriteLine($"{name.PadRight(45)} {time,8:F1} мс  ({percent,5:F1}%)  {bar}");
    }
}
