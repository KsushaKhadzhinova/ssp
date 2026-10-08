namespace Lab2;

// Наглядная демонстрация: статическая и экземплярная ссылки
public class StaticReferenceDemo
{
    // СТАТИЧЕСКАЯ ССЫЛКА - ОДНА для ВСЕХ объектов
    private static List<int> _staticList = new List<int>();

    // ЭКЗЕМПЛЯРНАЯ ССЫЛКА - своя для КАЖДОГО объекта
    private List<int> _instanceList = new List<int>();

    public void AddToStatic(int value)
    {
        _staticList.Add(value);  // Все объекты используют ОДИН и тот же список
    }

    public void AddToInstance(int value)
    {
        _instanceList.Add(value); // Каждый объект использует СВОЙ список
    }

    public void PrintLists(string objectName)
    {
        Console.WriteLine($"{objectName}:");
        Console.WriteLine($"  Статический список (общий): [{string.Join(", ", _staticList)}]");
        Console.WriteLine($"  Экземплярный список (свой): [{string.Join(", ", _instanceList)}]");
        Console.WriteLine();
    }
}

public class StaticFieldDemo
{
    private static int _staticCounter = 0;  // ОДИН счетчик для всех
    private int _instanceCounter = 0;       // СВОЙ счетчик для каждого

    public void Increment()
    {
        _staticCounter++;     // Увеличивает ОБЩИЙ счетчик
        _instanceCounter++;   // Увеличивает СВОЙ счетчик
    }

    public void ShowCounters(string objectName)
    {
        Console.WriteLine($"{objectName}:");
        Console.WriteLine($"  Статический счетчик (общий): {_staticCounter}");
        Console.WriteLine($"  Экземплярный счетчик (свой): {_instanceCounter}");
    }

    public static void ShowStaticFieldDemo()
    {
        var objA = new StaticFieldDemo();
        var objB = new StaticFieldDemo();

        objA.Increment(); // staticCounter = 1, instanceCounter objA = 1
        objA.Increment(); // staticCounter = 2, instanceCounter objA = 2

        objB.Increment(); // staticCounter = 3, instanceCounter objB = 1

        Console.WriteLine("После операций:");
        objA.ShowCounters("Объект A");
        objB.ShowCounters("Объект B");

        Console.WriteLine("\nСтатический счетчик ОДИН для обоих объектов!");
        Console.WriteLine("Экземплярные счетчики РАЗНЫЕ для каждого!");
    }
}

public static class Demo
{
    public static void Run()
    {
        Console.WriteLine("=== ОДНА статическая ссылка на ВСЕ объекты ===\n");

        // Создаем три разных объекта
        var obj1 = new StaticReferenceDemo();
        var obj2 = new StaticReferenceDemo();
        var obj3 = new StaticReferenceDemo();

        // Добавляем данные через разные объекты
        Console.WriteLine("1. Добавляем данные через разные объекты:\n");

        obj1.AddToStatic(100);  // Статический список для ВСЕХ объектов
        obj1.AddToInstance(1);   // Только для obj1

        obj2.AddToStatic(200);  // Добавляем в ТОТ ЖЕ статический список
        obj2.AddToInstance(2);   // Только для obj2

        obj3.AddToStatic(300);  // Опять в ТОТ ЖЕ статический список
        obj3.AddToInstance(3);   // Только для obj3

        // Выводим состояние каждого объекта
        obj1.PrintLists("Объект 1");
        obj2.PrintLists("Объект 2");
        obj3.PrintLists("Объект 3");

        Console.WriteLine("Статический список ОДИН для всех!");
        Console.WriteLine("Экземплярные списки РАЗНЫЕ для каждого!\n");

        // Демонстрация через статическое поле
        Console.WriteLine("2. Демонстрация через статическое поле:\n");

        StaticFieldDemo.ShowStaticFieldDemo();
    }
}
