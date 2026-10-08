namespace Lab4;

// Задание 4: поиск и исправление утечек

public class BuggyCode
{
    private static List<IDisposable> _cache = new List<IDisposable>();
    private static event EventHandler? _globalEvent;
    private byte[] _data = new byte[1024 * 1024];

    public BuggyCode()
    {
        _globalEvent += OnEvent;              // Утечка 1: подписка на статическое событие без отписки
        _cache.Add(new MemoryStream(1024));   // Утечка 2: статический кэш хранит MemoryStream и не освобождает его
    }

    private void OnEvent(object? sender, EventArgs e)
    {
        var temp = new byte[1024 * 100];
    }

    public static int CacheCount => _cache.Count;
}

// Исправленная версия
public class FixedCode : IDisposable
{
    private static List<IDisposable> _cache = new List<IDisposable>();
    private static event EventHandler? _globalEvent;
    private byte[]? _data = new byte[1024 * 1024];
    private MemoryStream _stream = new MemoryStream(1024);

    public FixedCode()
    {
        _globalEvent += OnEvent;
        _cache.Add(_stream);
    }

    private void OnEvent(object? sender, EventArgs e)
    {
        var temp = new byte[1024 * 100];
    }

    public void Dispose()
    {
        _globalEvent -= OnEvent;   // отписка от события
        _cache.Remove(_stream);    // удаление из статического кэша
        _stream.Dispose();         // освобождение потока
        _data = null;
    }

    public static int CacheCount => _cache.Count;
}

public static class LeakSearch
{
    public static void Run()
    {
        Console.WriteLine("\n=== Задание 4: код с утечками (BuggyCode, 100 объектов) ===");
        Console.WriteLine($"Память до: {GC.GetTotalMemory(true) / (1024 * 1024)} MB");
        for (int i = 0; i < 100; i++)
            new BuggyCode();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Console.WriteLine($"Память после создания и GC.Collect(): {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
        Console.WriteLine($"Объектов в статическом кэше: {BuggyCode.CacheCount}");
    }

    public static void RunFixed()
    {
        Console.WriteLine("\n=== Задание 4: исправленная версия (FixedCode, 100 объектов) ===");
        Console.WriteLine($"Память до: {GC.GetTotalMemory(true) / (1024 * 1024)} MB");
        for (int i = 0; i < 100; i++)
        {
            using (new FixedCode())
            {
            }
        }
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Console.WriteLine($"Память после создания, Dispose и GC.Collect(): {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
        Console.WriteLine($"Объектов в статическом кэше: {FixedCode.CacheCount}");
    }
}
