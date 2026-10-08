using Lab1;

// Без аргументов: задания 1, 2 и 4. С аргументом "bench": задание 3 (BenchmarkDotNet, режим Release).
if (args.Length > 0 && args[0] == "bench")
{
    CodeAnalyzers.Run();
    return;
}

IlAnalysis.Run();
ILGeneratorDemo.Run();
JitResearch.Run();
