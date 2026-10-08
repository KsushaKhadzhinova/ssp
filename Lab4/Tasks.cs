using System.Collections;
using System.Diagnostics;

namespace Lab4;

public static class Tasks
{
    // ===== Задание 1.1: статическая утечка =====
    public static void Task1_1()
    {
        Console.WriteLine("\n=== Задание 1.1: статическая утечка ===");
        Console.WriteLine($"До: {GC.GetTotalMemory(false) / 1024} KB");
        StaticLeak.AddData();       // 1000 объектов по 10KB
        Console.WriteLine($"После добавления 1000 x 10KB: {GC.GetTotalMemory(false) / 1024} KB");
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Console.WriteLine($"После GC.Collect() (память не освобождается): {GC.GetTotalMemory(false) / 1024} KB");
    }

    // ===== Задание 1.2: событийная утечка =====
    public static void Task1_2()
    {
        Console.WriteLine("\n=== Задание 1.2: событийная утечка ===");
        var source = new EventLeak();
        Console.WriteLine($"До подписок: {GC.GetTotalMemory(false) / 1024} KB");
        for (int i = 0; i < 100; i++)
            source.Subscribe();
        Console.WriteLine($"После 100 подписок: {GC.GetTotalMemory(false) / 1024} KB");
        GC.Collect();
        Console.WriteLine($"После GC.Collect() (подписки живы): {GC.GetTotalMemory(false) / 1024} KB");
        source.UnsubscribeAll();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Console.WriteLine($"После отписки и GC.Collect(): {GC.GetTotalMemory(false) / 1024} KB");
        GC.KeepAlive(source);
    }

    // ===== Задание 2.1: объекты разных размеров, поколение =====
    public static void Task2_1()
    {
        Console.WriteLine("\n=== Задание 2.1: создание объектов в LOH ===");
        var small = new byte[80_000];   // 80KB - не в LOH
        var large = new byte[90_000];   // 90KB - в LOH

        Console.WriteLine($"small: Gen{GC.GetGeneration(small)}");
        Console.WriteLine($"large: Gen{GC.GetGeneration(large)}");
    }

    // ===== Задание 2.2: мониторинг LOH =====
    public static void Task2_2()
    {
        Console.WriteLine("\n=== Задание 2.2: мониторинг LOH ===");
        var objects = new List<byte[]>();
        Console.WriteLine($"Память до: {GC.GetTotalMemory(false) / 1024} KB");
        for (int i = 0; i < 100; i++)
            objects.Add(new byte[100 * 1024]);
        Console.WriteLine($"Память после создания 100 x 100KB: {GC.GetTotalMemory(false) / 1024} KB");

        objects.Clear();
        GC.Collect(2);
        Console.WriteLine($"Память после очистки списка и GC.Collect(2): {GC.GetTotalMemory(false) / 1024} KB");
    }

    // ===== Задание 3.1: сравнение производительности boxing =====
    public static void Task3_1()
    {
        Console.WriteLine("\n=== Задание 3.1: boxing ===");

        long mem = GC.GetTotalMemory(true);
        var sw = Stopwatch.StartNew();
        // С упаковкой
        var boxed = new List<object>();
        for (int i = 0; i < 1000000; i++)
            boxed.Add(i);
        sw.Stop();
        Console.WriteLine($"С упаковкой (List<object>): {sw.ElapsedMilliseconds} мс, +{(GC.GetTotalMemory(false) - mem) / 1024} KB");

        mem = GC.GetTotalMemory(true);
        sw.Restart();
        // Без упаковки
        var unboxed = new List<int>();
        for (int i = 0; i < 1000000; i++)
            unboxed.Add(i);
        sw.Stop();
        Console.WriteLine($"Без упаковки (List<int>): {sw.ElapsedMilliseconds} мс, +{(GC.GetTotalMemory(false) - mem) / 1024} KB");

        GC.KeepAlive(boxed);
        GC.KeepAlive(unboxed);
    }

    // ===== Задание 3.2: оптимизация существующего кода =====
    public static void Task3_2()
    {
        Console.WriteLine("\n=== Задание 3.2: UnoptimizedCode и OptimizedCode ===");

        long mem = GC.GetTotalMemory(true);
        var sw = Stopwatch.StartNew();
        var unoptimized = new UnoptimizedCode();
        for (int i = 0; i < 1000000; i++)
            unoptimized.Add(i);
        sw.Stop();
        Console.WriteLine($"UnoptimizedCode (ArrayList): {sw.ElapsedMilliseconds} мс, +{(GC.GetTotalMemory(false) - mem) / 1024} KB");

        mem = GC.GetTotalMemory(true);
        sw.Restart();
        var optimized = new OptimizedCode();
        for (int i = 0; i < 1000000; i++)
            optimized.Add(i);
        sw.Stop();
        Console.WriteLine($"OptimizedCode (List<int>): {sw.ElapsedMilliseconds} мс, +{(GC.GetTotalMemory(false) - mem) / 1024} KB");

        GC.KeepAlive(unoptimized);
        GC.KeepAlive(optimized);
    }
}

// Класс со статическим списком (1.1)
public class StaticLeak
{
    private static List<byte[]> _list = new List<byte[]>();

    public static void AddData()
    {
        for (int i = 0; i < 1000; i++)
            _list.Add(new byte[1024 * 10]);
    }
}

// Класс с событием (1.2): большой объект создаётся при подписке и удерживается обработчиком
public class EventLeak
{
    public event EventHandler? BigEvent;
    private List<EventHandler> _handlers = new List<EventHandler>();

    public void Subscribe()
    {
        var data = new byte[1024 * 100];
        EventHandler handler = (sender, e) => { data[0]++; };
        BigEvent += handler;
        _handlers.Add(handler);
    }

    public void UnsubscribeAll()
    {
        foreach (var handler in _handlers)
            BigEvent -= handler;
        _handlers.Clear();
    }
}

// НЕОПТИМИЗИРОВАННО
public class UnoptimizedCode
{
    private ArrayList _data = new ArrayList();
    public void Add(int value) => _data.Add(value); // Boxing!
}

// ОПТИМИЗИРОВАННО
public class OptimizedCode
{
    private List<int> _data = new List<int>();
    public void Add(int value) => _data.Add(value); // Без boxing
}
