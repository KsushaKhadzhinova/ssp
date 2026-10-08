using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Lab1;

// Исследование JIT-компиляции: примеры из методички и задание 4
public class JitBenchmark
{
    private const int Iterations = 1000;

    public int NormalMethod()
    {
        int sum = 0;
        for (int i = 0; i < Iterations; i++)
            sum += i;
        return sum;
    }

    public int VirtualMethod()
    {
        var calc = new Calculator();
        int sum = 0;
        for (int i = 0; i < Iterations; i++)
            sum = calc.Add(sum, i);
        return sum;
    }

    public int DelegateMethod()
    {
        Func<int, int, int> add = (x, y) => x + y;
        int sum = 0;
        for (int i = 0; i < Iterations; i++)
            sum = add(sum, i);
        return sum;
    }
}

public class Calculator
{
    public virtual int Add(int a, int b)
    {
        return a + b;
    }
}

public static class JitVisualization
{
    public static unsafe void InspectMethod(MethodInfo method)
    {
        Console.WriteLine($"\n=== Инспекция метода: {method.Name} ===\n");

        // Получаем указатель на метод
        RuntimeHelpers.PrepareMethod(method.MethodHandle);

        // Получаем адрес скомпилированного кода
        var methodHandle = method.MethodHandle;
        var functionPointer = methodHandle.GetFunctionPointer();

        Console.WriteLine($"Адрес метода: 0x{functionPointer.ToInt64():X16}");

        // Информация о JIT
        var methodBody = method.GetMethodBody();
        if (methodBody != null)
        {
            Console.WriteLine($"Размер IL: {methodBody.GetILAsByteArray()?.Length ?? 0} байт");
            Console.WriteLine($"Локальные переменные: {methodBody.LocalVariables.Count}");
            Console.WriteLine($"Максимальный стек: {methodBody.MaxStackSize}");
        }

        // Информация о вызовах
        Console.WriteLine($"\nАтрибуты метода:");
        Console.WriteLine($"  - IsVirtual: {method.IsVirtual}");
        Console.WriteLine($"  - IsStatic: {method.IsStatic}");
        Console.WriteLine($"  - IsAbstract: {method.IsAbstract}");
        Console.WriteLine($"  - IsFinal: {method.IsFinal}");
    }

    public static void CompareJitModes()
    {
        Console.WriteLine("\n=== Сравнение режимов JIT ===\n");

        var config = new Dictionary<string, bool>
        {
            ["TieredCompilation"] = GetTieredCompilationSetting()
        };

        foreach (var setting in config)
        {
            Console.WriteLine($"{setting.Key}: {(setting.Value ? "ВКЛЮЧЕН" : "ВЫКЛЮЧЕН")}");
        }

        Console.WriteLine("\nДля изменения режимов добавьте в .csproj:");
        Console.WriteLine("<TieredCompilation>false</TieredCompilation>");
    }

    private static bool GetTieredCompilationSetting()
    {
        try
        {
            var configProperty = typeof(RuntimeHelpers).GetProperty("IsTieredCompilation");
            if (configProperty != null)
            {
                return (bool)configProperty.GetValue(null)!;
            }
        }
        catch { }
        return true; // По умолчанию включена
    }
}

public static class JitResearch
{
    public static void Run()
    {
        Console.WriteLine("\n=== JIT Бенчмарк (упрощенная версия) ===\n");

        var benchmark = new JitBenchmark();
        var methods = typeof(JitBenchmark).GetMethods(BindingFlags.Public | BindingFlags.Instance);

        foreach (var method in methods)
        {
            if (method.Name == "GetType" || method.Name == "ToString" ||
                method.Name == "Equals" || method.Name == "GetHashCode")
                continue;

            Console.WriteLine($"Тестирование: {method.Name}");

            // Прогрев JIT
            for (int i = 0; i < 10; i++)
                method.Invoke(benchmark, null);

            // Основной замер
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++)
                method.Invoke(benchmark, null);
            sw.Stop();

            Console.WriteLine($"  Время: {sw.ElapsedMilliseconds} мс");
            Console.WriteLine($"  Тики: {sw.ElapsedTicks:N0}");
            Console.WriteLine();
        }

        JitVisualization.InspectMethod(typeof(Calculator).GetMethod("Add")!);
        JitVisualization.CompareJitModes();

        Console.WriteLine("\n=== Профилирование производительности ===\n");

        const int iterations = 10_000_000;
        var timer = new Stopwatch();

        // Direct call
        timer.Start();
        int sum1 = 0;
        for (int i = 0; i < iterations; i++)
            sum1 += i;
        timer.Stop();
        Console.WriteLine($"Прямой вызов: {timer.ElapsedMilliseconds} мс");

        // Through delegate
        Func<int, int, int> add = (x, y) => x + y;
        timer.Restart();
        int sum2 = 0;
        for (int i = 0; i < iterations; i++)
            sum2 = add(sum2, i);
        timer.Stop();
        Console.WriteLine($"Через делегат: {timer.ElapsedMilliseconds} мс");

        // Through virtual method
        var calc = new Calculator();
        timer.Restart();
        int sum3 = 0;
        for (int i = 0; i < iterations; i++)
            sum3 = calc.Add(sum3, i);
        timer.Stop();
        Console.WriteLine($"Через виртуальный метод: {timer.ElapsedMilliseconds} мс");

        ArraySumTask();
    }

    // Задание 4.1: сложение элементов массива из 10000 целых чисел
    // Первый вызов включает JIT-компиляцию метода, последующие выполняют уже скомпилированный код
    static void ArraySumTask()
    {
        Console.WriteLine("\n=== Задание 4.1: сумма элементов массива (10000 элементов) ===\n");

        int[] array = new int[10000];
        for (int i = 0; i < array.Length; i++)
            array[i] = i;

        // Первый вызов: JIT-компиляция + выполнение
        long start = Stopwatch.GetTimestamp();
        long result = SumArray(array);
        long firstCall = Stopwatch.GetTimestamp() - start;
        Console.WriteLine($"Сумма = {result}");
        Console.WriteLine($"Первый вызов (с JIT-компиляцией): {firstCall * 1_000_000.0 / Stopwatch.Frequency:F1} мкс");

        // Повторные вызовы: метод уже скомпилирован
        const int calls = 1000;
        start = Stopwatch.GetTimestamp();
        for (int i = 0; i < calls; i++)
            SumArray(array);
        long repeated = Stopwatch.GetTimestamp() - start;
        Console.WriteLine($"Повторный вызов (без JIT-компиляции): {repeated * 1_000_000.0 / Stopwatch.Frequency / calls:F1} мкс");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long SumArray(int[] array)
    {
        long sum = 0;
        for (int i = 0; i < array.Length; i++)
            sum += array[i];
        return sum;
    }
}
