using System.Reflection;

namespace Lab1;

// Анализ IL-кода: пример из методички и задание 1
public static class IlAnalysis
{
    public static void Run()
    {
        Console.WriteLine("=== Лабораторная работа: Анализ IL-кода ===\n");

        AnalyzeIL("Метод SimpleMethod", SimpleMethod);
        AnalyzeIL("Метод MethodWithLoop", MethodWithLoop);
        AnalyzeIL("Метод MethodWithCondition", MethodWithCondition);

        Console.WriteLine("\n=== Задание 1: произведение двух целых чисел ===");
        Console.WriteLine($"Multiply(6, 7) = {Multiply(6, 7)}");
        AnalyzeIL("Метод Multiply", Multiply);
    }

    // Простой метод
    static int SimpleMethod(int a, int b) => a + b;

    // Метод с циклом
    static int MethodWithLoop(int n)
    {
        int sum = 0;
        for (int i = 1; i <= n; i++)
            sum += i;
        return sum;
    }

    // Метод с условием
    static string MethodWithCondition(int value)
    {
        if (value > 0)
            return "Positive";
        else if (value < 0)
            return "Negative";
        else
            return "Zero";
    }

    // Задание 1: произведение двух целых чисел
    static int Multiply(int a, int b) => a * b;

    // Вспомогательный метод для анализа IL
    static void AnalyzeIL(string methodName, Delegate method)
    {
        Console.WriteLine($"\n--- Анализ: {methodName} ---");

        var methodInfo = method.Method;
        var methodBody = methodInfo.GetMethodBody();

        if (methodBody == null)
        {
            Console.WriteLine("Тело метода не доступно (Release режим?)");
            Console.WriteLine("Перекомпилируйте в Debug режиме!\n");
            return;
        }

        byte[] ilBytes = methodBody.GetILAsByteArray();

        if (ilBytes == null || ilBytes.Length == 0)
        {
            Console.WriteLine("IL-код не получен");
            return;
        }

        Console.WriteLine($"Размер IL: {ilBytes.Length} байт");
        Console.WriteLine($"Локальные переменные: {methodBody.LocalVariables.Count}");
        Console.WriteLine($"Максимальный размер стека: {methodBody.MaxStackSize}");

        Console.WriteLine("\nIL-инструкции (HEX):");
        for (int i = 0; i < ilBytes.Length; i++)
        {
            Console.Write($"{ilBytes[i]:X2} ");
            if ((i + 1) % 16 == 0) Console.WriteLine();
        }
        Console.WriteLine("\n");
    }
}
