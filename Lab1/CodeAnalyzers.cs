using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Lab1;

// Задание 3: анализатор кода на основе BenchmarkDotNet

// 3.1 Анализатор для пункта 2.1 (факториал)
[MemoryDiagnoser]
[ShortRunJob]
public class FactorialAnalyzer
{
    private Func<int, int> _dynamicFactorial = null!;
    private Func<int, int> _dynamicRecursiveFactorial = null!;
    private int _n = 10;

    [GlobalSetup]
    public void Setup()
    {
        _dynamicFactorial = ILGeneratorDemo.CreateFactorialMethod();
        _dynamicRecursiveFactorial = ILGeneratorDemo.CreateRecursiveFactorialMethod();
    }

    [Benchmark(Baseline = true)]
    public int CSharpRecursive() => Factorial(_n);

    [Benchmark]
    public int DynamicLoop() => _dynamicFactorial(_n);

    [Benchmark]
    public int DynamicRecursive() => _dynamicRecursiveFactorial(_n);

    private static int Factorial(int n) => n <= 1 ? 1 : n * Factorial(n - 1);
}

// 3.2 Анализатор для пункта 2.2 (максимум из трёх чисел)
[MemoryDiagnoser]
[ShortRunJob]
public class MaxOfThreeAnalyzer
{
    private Func<int, int, int, int> _dynamicMax = null!;
    private int _a = 3;
    private int _b = 9;
    private int _c = 5;

    [GlobalSetup]
    public void Setup()
    {
        _dynamicMax = ILGeneratorDemo.CreateMaxOfThreeMethod();
    }

    [Benchmark(Baseline = true)]
    public int CSharpMax() => Max(_a, _b, _c);

    [Benchmark]
    public int DynamicMax() => _dynamicMax(_a, _b, _c);

    private static int Max(int a, int b, int c)
    {
        int max = a;
        if (b > max) max = b;
        if (c > max) max = c;
        return max;
    }
}

public static class CodeAnalyzers
{
    public static void Run()
    {
        BenchmarkRunner.Run<FactorialAnalyzer>();
        BenchmarkRunner.Run<MaxOfThreeAnalyzer>();
    }
}
