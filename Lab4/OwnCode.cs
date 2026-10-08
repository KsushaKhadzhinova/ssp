using System.Diagnostics;
using System.Text;

namespace Lab4;

// Профилирование собственного небольшого кода:
// обработка текста с замером времени каждого метода (по принципу Call Tree)
public static class OwnCode
{
    private static Dictionary<string, double> _times = new Dictionary<string, double>();
    private static Dictionary<string, int> _counts = new Dictionary<string, int>();

    public static void Run()
    {
        Console.WriteLine("\n=== Профилирование собственного кода ===\n");

        var total = Stopwatch.StartNew();
        string text = BuildTextWithConcat(20_000);
        string textBuilder = BuildTextWithBuilder(20_000);
        int words = CountWords(text);
        var numbers = SortNumbers(200_000);
        total.Stop();

        Console.WriteLine($"Длина текстов: {text.Length} и {textBuilder.Length}, слов: {words}, чисел: {numbers.Count}");
        Console.WriteLine($"\nОбщее время: {total.Elapsed.TotalMilliseconds:F1} мс\n");
        Console.WriteLine($"{"Метод",-25}{"Вызовов",8}{"Время, мс",12}{"Доля, %",10}");
        foreach (var item in _times.OrderByDescending(t => t.Value))
            Console.WriteLine($"{item.Key,-25}{_counts[item.Key],8}{item.Value,12:F1}{item.Value / total.Elapsed.TotalMilliseconds * 100,10:F1}");
    }

    private static void Add(string name, Stopwatch sw)
    {
        sw.Stop();
        _times[name] = sw.Elapsed.TotalMilliseconds;
        _counts[name] = 1;
    }

    // Конкатенация строк в цикле: каждый раз создаётся новая строка
    private static string BuildTextWithConcat(int count)
    {
        var sw = Stopwatch.StartNew();
        string result = "";
        for (int i = 0; i < count; i++)
            result += "слово ";
        Add("BuildTextWithConcat", sw);
        return result;
    }

    private static string BuildTextWithBuilder(int count)
    {
        var sw = Stopwatch.StartNew();
        var sb = new StringBuilder();
        for (int i = 0; i < count; i++)
            sb.Append("слово ");
        Add("BuildTextWithBuilder", sw);
        return sb.ToString();
    }

    private static int CountWords(string text)
    {
        var sw = Stopwatch.StartNew();
        int count = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        Add("CountWords", sw);
        return count;
    }

    private static List<int> SortNumbers(int count)
    {
        var sw = Stopwatch.StartNew();
        var random = new Random(1);
        var numbers = new List<int>();
        for (int i = 0; i < count; i++)
            numbers.Add(random.Next());
        numbers.Sort();
        Add("SortNumbers", sw);
        return numbers;
    }
}
