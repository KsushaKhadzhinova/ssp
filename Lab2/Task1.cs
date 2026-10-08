using System.Diagnostics;

namespace Lab2;

// Задание 1: базовый пример с утечкой памяти
public class MemoryLeakExample
{
    private static List<byte[]> _staticList = new List<byte[]>();

    public static void CreateMemoryLeak()
    {
        // Создаем объекты, которые никогда не будут собраны
        for (int i = 0; i < 1000; i++)
        {
            _staticList.Add(new byte[1024 * 10]); // 10KB каждый
        }
    }

    public static void ClearMemoryLeak()
    {
        _staticList.Clear();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}

public class EventLeakExample
{
    public event EventHandler? BigEvent;

    public void SubscribeLeak()
    {
        // Подписка на событие без отписки
        BigEvent += (sender, e) =>
        {
            var data = new byte[1024 * 100];
        };
    }
}

public static class Task1
{
    public static void Run()
    {
        Console.WriteLine("=== Лабораторная работа: Управление памятью ===");
        var monitor = new MemoryMonitor();

        // 1. Тест памяти и LOH
        Console.WriteLine("\n1. Выделение памяти:");
        monitor.AllocateMemory(10);
        monitor.AllocateLOHObjects(100);
        monitor.PrintMemoryInfo("После выделения");

        // 2. Тест утечек
        Console.WriteLine("\n2. Тест утечек:");
        MemoryLeakExample.CreateMemoryLeak();
        var eventExample = new EventLeakExample();
        for (int i = 0; i < 100; i++)
            eventExample.SubscribeLeak();
        monitor.PrintMemoryInfo("После утечек");

        // 3. Тест Boxing
        Console.WriteLine("\n3. Тест упаковки:");
        var sw = Stopwatch.StartNew();
        monitor.SimulateBoxing(1000000);
        sw.Stop();
        Console.WriteLine($"Время с упаковкой: {sw.ElapsedMilliseconds}ms");

        // Без упаковки
        sw.Restart();
        var optimizedList = new List<int>();
        for (int i = 0; i < 1000000; i++)
            optimizedList.Add(i);
        sw.Stop();
        Console.WriteLine($"Время без упаковки: {sw.ElapsedMilliseconds}ms");

        // 4. Очистка
        Console.WriteLine("\n4. После очистки:");
        MemoryLeakExample.ClearMemoryLeak();
        monitor.Cleanup();
        monitor.PrintMemoryInfo("Очищено");

        // 5. Поколения объектов
        Console.WriteLine("\n5. Поколения объектов:");
        var obj = new object();
        Console.WriteLine($"Начальное поколение: {GC.GetGeneration(obj)}");
        GC.Collect();
        Console.WriteLine($"После GC: {GC.GetGeneration(obj)}");

        GC.KeepAlive(eventExample);
        GC.KeepAlive(optimizedList);
    }
}

// Large Object Heap - пример
public class LOHExample
{
    public static void CreateLargeObjects()
    {
        // Объекты > 85KB попадают в LOH
        for (int i = 0; i < 100; i++)
        {
            var largeArray = new byte[100000]; // ~100KB
            // Используем largeArray
        }
    }
}

// Boxing и его влияние
public class BoxingExample
{
    private List<object> _boxedItems = new List<object>();
    private List<int> _unboxedItems = new List<int>();

    public void BoxingOperation()
    {
        // Упаковка - создает дополнительные объекты в куче
        for (int i = 0; i < 1000000; i++)
        {
            _boxedItems.Add(i); // int упаковывается в object
        }
    }

    public void UnboxedOperation()
    {
        // Без упаковки - эффективнее
        for (int i = 0; i < 1000000; i++)
        {
            _unboxedItems.Add(i);
        }
    }
}
