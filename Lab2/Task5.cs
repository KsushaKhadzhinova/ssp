namespace Lab2;

// Задание 5: исследование Large Object Heap (LOH)
public static class Task5
{
    static List<byte[]> _keep = new List<byte[]>();

    public static void Run()
    {
        Console.WriteLine("\n=== Задание 5: объекты в LOH ===");

        long before = GC.GetTotalMemory(false);
        Console.WriteLine($"Память до выделения: {before / 1024} KB");

        // 100 массивов по 100KB
        for (int i = 0; i < 100; i++)
            _keep.Add(new byte[100 * 1024]);

        long after = GC.GetTotalMemory(false);
        Console.WriteLine($"Память после выделения 100 x 100KB: {after / 1024} KB");

        GC.Collect();
        GC.WaitForPendingFinalizers();
        long afterGc = GC.GetTotalMemory(false);
        Console.WriteLine($"Память после GC.Collect() (ссылки удерживаются): {afterGc / 1024} KB");

        _keep.Clear();
        GC.Collect(2);
        GC.WaitForPendingFinalizers();
        Console.WriteLine($"Память после очистки списка и GC.Collect(2): {GC.GetTotalMemory(false) / 1024} KB");

        // Массивы 90KB и 80KB
        var small = new byte[80_000];
        var large = new byte[90_000];
        Console.WriteLine("\nСравнение размещения:");
        Console.WriteLine($"small (80 000 байт): Gen{GC.GetGeneration(small)}");
        Console.WriteLine($"large (90 000 байт): Gen{GC.GetGeneration(large)}");

        // Мониторинг LOH: объекты разного размера
        Console.WriteLine("\n=== Задание 5: мониторинг LOH ===");
        int[] sizesKb = { 10, 50, 90, 200 };
        foreach (int kb in sizesKb)
        {
            var array = new byte[kb * 1000];
            Console.WriteLine($"Объект {kb}KB: Gen{GC.GetGeneration(array)}");
        }
    }
}
