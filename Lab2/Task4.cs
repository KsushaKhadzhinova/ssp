namespace Lab2;

// Задание 4: исследование статических ссылок и утечек памяти

// Создание статической утечки
public class StaticLeakDemo
{
    private static List<byte[]> _staticList = new List<byte[]>();

    // Добавляет 1000 объектов по 10KB в статический список
    public static void AddData()
    {
        for (int i = 0; i < 1000; i++)
            _staticList.Add(new byte[1024 * 10]);
    }

    public static void CheckMemory()
    {
        Console.WriteLine($"  Текущий размер памяти: {GC.GetTotalMemory(false) / 1024} KB");
        Console.WriteLine($"  Количество объектов в статическом списке: {_staticList.Count}");
        Console.WriteLine($"  Поколение статического списка: {GC.GetGeneration(_staticList)}");
    }
}

// Событийная утечка
public class EventLeakDemo
{
    public event EventHandler? DemoEvent;
    private List<EventHandler> _handlers = new List<EventHandler>();

    // Подписка на событие; большой объект создаётся в обработчике и живёт, пока есть подписка
    public void Subscribe()
    {
        var bigObject = new byte[1024 * 100];
        EventHandler handler = (sender, e) => { bigObject[0]++; };
        DemoEvent += handler;
        _handlers.Add(handler);
    }

    // Отписка от всех подписок
    public void Unsubscribe()
    {
        foreach (var handler in _handlers)
            DemoEvent -= handler;
        _handlers.Clear();
    }
}

public static class Task4
{
    public static void Run()
    {
        Console.WriteLine("\n=== Задание 4: статическая утечка ===");
        Console.WriteLine("До добавления данных:");
        StaticLeakDemo.CheckMemory();
        StaticLeakDemo.AddData();
        Console.WriteLine("После AddData():");
        StaticLeakDemo.CheckMemory();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Console.WriteLine("После GC.Collect() (объекты не удаляются):");
        StaticLeakDemo.CheckMemory();

        Console.WriteLine("\n=== Задание 4: событийная утечка ===");
        var eventDemo = new EventLeakDemo();
        Console.WriteLine($"До подписок: {GC.GetTotalMemory(false) / 1024} KB");
        for (int i = 0; i < 100; i++)
            eventDemo.Subscribe();
        Console.WriteLine($"После 100 подписок: {GC.GetTotalMemory(false) / 1024} KB");
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Console.WriteLine($"После GC.Collect() (подписки живы): {GC.GetTotalMemory(false) / 1024} KB");
        eventDemo.Unsubscribe();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Console.WriteLine($"После отписки и GC.Collect(): {GC.GetTotalMemory(false) / 1024} KB");
        GC.KeepAlive(eventDemo);

        Console.WriteLine("\n=== Задание 4: сравнение поколений объектов ===");
        var obj = new object();
        Console.WriteLine($"Новый объект: Gen{GC.GetGeneration(obj)}");
        for (int i = 1; i <= 3; i++)
        {
            GC.Collect();
            Console.WriteLine($"После GC.Collect() №{i}: Gen{GC.GetGeneration(obj)}");
        }

        var small = new byte[1000];
        var medium = new byte[50_000];
        var large = new byte[100_000];
        Console.WriteLine($"Массив 1000 байт: Gen{GC.GetGeneration(small)}");
        Console.WriteLine($"Массив 50 000 байт: Gen{GC.GetGeneration(medium)}");
        Console.WriteLine($"Массив 100 000 байт (> 85KB): Gen{GC.GetGeneration(large)}");
    }
}
