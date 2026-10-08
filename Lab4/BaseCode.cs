using System.Diagnostics;

namespace Lab4;

// Базовый код для лабораторной работы

// Класс для демонстрации статической утечки
public class MemoryLeakExample
{
    private static List<byte[]> _staticList = new List<byte[]>();

    public static void CreateMemoryLeak()
    {
        for (int i = 0; i < 1000; i++)
            _staticList.Add(new byte[1024 * 10]); // 10KB
    }

    public static void ClearMemoryLeak()
    {
        _staticList.Clear();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}

// Класс для демонстрации событийной утечки
public class EventLeakExample
{
    public event EventHandler? BigEvent;
    private List<EventHandler> _handlers = new List<EventHandler>();

    public void SubscribeLeak()
    {
        EventHandler handler = (sender, e) =>
        {
            var data = new byte[1024 * 100];
        };
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

// Класс для мониторинга памяти
public class MemoryMonitor : IDisposable
{
    private bool _disposed;
    private List<byte[]> _allocatedMemory = new List<byte[]>();

    public void AllocateMemory(int sizeInMB)
    {
        _allocatedMemory.Add(new byte[sizeInMB * 1024 * 1024]);
    }

    public void AllocateLOHObjects(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var largeObject = new byte[90000 + i];
        }
    }

    public void SimulateBoxing(int count = 1000000)
    {
        var list = new List<object>();
        for (int i = 0; i < count; i++)
            list.Add(i);
    }

    public void PrintMemoryInfo(string message = "")
    {
        if (!string.IsNullOrEmpty(message))
            Console.WriteLine($"\n=== {message} ===");

        Console.WriteLine($"Total Memory: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
        Console.WriteLine($"Gen0: {GC.CollectionCount(0)}, Gen1: {GC.CollectionCount(1)}, Gen2: {GC.CollectionCount(2)}");

        using (Process process = Process.GetCurrentProcess())
            Console.WriteLine($"Working Set: {process.WorkingSet64 / (1024 * 1024)} MB");
    }

    public void Cleanup()
    {
        _allocatedMemory.Clear();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Cleanup();
            _disposed = true;
        }
    }
}

// Демонстрация статических ссылок
public class StaticReferenceDemo
{
    private static List<int> _staticList = new List<int>(); // ОДНА для всех
    private List<int> _instanceList = new List<int>();      // Своя для каждого

    public void AddToStatic(int value) => _staticList.Add(value);
    public void AddToInstance(int value) => _instanceList.Add(value);

    public void PrintLists(string name)
    {
        Console.WriteLine($"{name}: Статический [{string.Join(",", _staticList)}], " +
                         $"Экземплярный [{string.Join(",", _instanceList)}]");
    }
}

public static class BaseCode
{
    public static void Run()
    {
        Console.WriteLine("=== Лабораторная работа: Управление памятью ===\n");

        using (var monitor = new MemoryMonitor())
        {
            // 1. Демонстрация статических ссылок
            Console.WriteLine("1. Статические ссылки:");
            var obj1 = new StaticReferenceDemo();
            var obj2 = new StaticReferenceDemo();

            obj1.AddToStatic(100);
            obj1.AddToInstance(1);
            obj2.AddToStatic(200);
            obj2.AddToInstance(2);

            obj1.PrintLists("Объект 1");
            obj2.PrintLists("Объект 2");
            Console.WriteLine("Статический список ОБЩИЙ для всех объектов!\n");

            // 2. Тест памяти и LOH
            Console.WriteLine("2. Выделение памяти и LOH:");
            monitor.AllocateMemory(10);
            monitor.AllocateLOHObjects(100);
            monitor.PrintMemoryInfo("После выделения");

            // 3. Тест утечек
            Console.WriteLine("\n3. Тест утечек:");
            MemoryLeakExample.CreateMemoryLeak();
            var eventExample = new EventLeakExample();
            for (int i = 0; i < 100; i++)
                eventExample.SubscribeLeak();
            monitor.PrintMemoryInfo("После утечек");
            eventExample.UnsubscribeAll();

            // 4. Тест Boxing
            Console.WriteLine("\n4. Тест упаковки:");
            var sw = Stopwatch.StartNew();
            monitor.SimulateBoxing(1000000);
            sw.Stop();
            Console.WriteLine($"С упаковкой: {sw.ElapsedMilliseconds}ms");

            sw.Restart();
            var optimizedList = new List<int>();
            for (int i = 0; i < 1000000; i++)
                optimizedList.Add(i);
            sw.Stop();
            Console.WriteLine($"Без упаковки: {sw.ElapsedMilliseconds}ms");

            // 5. Очистка
            Console.WriteLine("\n5. После очистки:");
            MemoryLeakExample.ClearMemoryLeak();
            monitor.Cleanup();
            monitor.PrintMemoryInfo("Очищено");

            // 6. Поколения объектов
            Console.WriteLine("\n6. Поколения объектов:");
            var obj = new object();
            Console.WriteLine($"Начальное поколение: {GC.GetGeneration(obj)}");
            GC.Collect();
            Console.WriteLine($"После GC: {GC.GetGeneration(obj)}");

            GC.KeepAlive(eventExample);
            GC.KeepAlive(optimizedList);
        }
    }
}
