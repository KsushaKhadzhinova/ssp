using System.Reflection.Emit;

namespace Lab1;

// Генерация и выполнение IL-кода: пример из методички и задание 2
public static class ILGeneratorDemo
{
    public static void Run()
    {
        Console.WriteLine("\n=== Динамическая генерация IL ===\n");

        var addMethod = CreateAddMethod();
        Console.WriteLine($"DynamicAdd(15, 27) = {addMethod(15, 27)}");

        var factorialMethod = CreateFactorialMethod();
        Console.WriteLine($"DynamicFactorial(5) = {factorialMethod(5)}");
        Console.WriteLine($"DynamicFactorial(7) = {factorialMethod(7)}");

        var gradeMethod = CreateGradeMethod();
        Console.WriteLine($"DynamicGrade(10) = {gradeMethod(10)}");
        Console.WriteLine($"DynamicGrade(-5) = {gradeMethod(-5)}");
        Console.WriteLine($"DynamicGrade(0) = {gradeMethod(0)}");

        Console.WriteLine("\n=== Задание 2.1: n! рекурсивно ===\n");
        var recursiveFactorial = CreateRecursiveFactorialMethod();
        Console.WriteLine($"RecursiveFactorial(5) = {recursiveFactorial(5)}");
        Console.WriteLine($"RecursiveFactorial(7) = {recursiveFactorial(7)}");

        Console.WriteLine("\n=== Задание 2.2: максимум из трёх чисел ===\n");
        var maxMethod = CreateMaxOfThreeMethod();
        Console.WriteLine($"DynamicMax(3, 9, 5) = {maxMethod(3, 9, 5)}");
        Console.WriteLine($"DynamicMax(10, 2, 7) = {maxMethod(10, 2, 7)}");
        Console.WriteLine($"DynamicMax(1, 4, 8) = {maxMethod(1, 4, 8)}");
    }

    public static Func<int, int, int> CreateAddMethod()
    {
        // Создаём динамический метод
        DynamicMethod dynamicMethod = new DynamicMethod(
            "DynamicAdd",
            typeof(int),                    // Возвращаемый тип
            new[] { typeof(int), typeof(int) }, // входные параметры
            typeof(ILGeneratorDemo).Module
        );

        // Получаем ILGenerator
        ILGenerator il = dynamicMethod.GetILGenerator();

        // Генерируем IL-код
        // int Add(int a, int b) => a + b;
        il.Emit(OpCodes.Ldarg_0);      // загрузить первый аргумент
        il.Emit(OpCodes.Ldarg_1);      // загрузить второй аргумент
        il.Emit(OpCodes.Add);          // сложить
        il.Emit(OpCodes.Ret);          // вернуть результат

        // Создаём делегат
        return (Func<int, int, int>)dynamicMethod.CreateDelegate(typeof(Func<int, int, int>));
    }

    public static Func<int, int> CreateFactorialMethod()
    {
        DynamicMethod dynamicMethod = new DynamicMethod(
            "DynamicFactorial",
            typeof(int),
            new[] { typeof(int) },
            typeof(ILGeneratorDemo).Module
        );

        ILGenerator il = dynamicMethod.GetILGenerator();

        // Метки для переходов
        Label startLoop = il.DefineLabel();
        Label endLoop = il.DefineLabel();

        // Локальные переменные
        il.DeclareLocal(typeof(int)); // result
        il.DeclareLocal(typeof(int)); // i

        // result = 1;
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Stloc_0);

        // i = 1;
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Stloc_1);

        // startLoop:
        il.MarkLabel(startLoop);

        // if (i > arg0) goto endLoop;
        il.Emit(OpCodes.Ldloc_1);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Bgt, endLoop);

        // result *= i;
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldloc_1);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Stloc_0);

        // i++;
        il.Emit(OpCodes.Ldloc_1);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Stloc_1);

        // goto startLoop;
        il.Emit(OpCodes.Br, startLoop);

        // endLoop:
        il.MarkLabel(endLoop);

        // return result;
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ret);

        return (Func<int, int>)dynamicMethod.CreateDelegate(typeof(Func<int, int>));
    }

    public static Func<int, string> CreateGradeMethod()
    {
        DynamicMethod dynamicMethod = new DynamicMethod(
            "DynamicGrade",
            typeof(string),
            new[] { typeof(int) },
            typeof(ILGeneratorDemo).Module
        );

        ILGenerator il = dynamicMethod.GetILGenerator();

        Label positive = il.DefineLabel();
        Label negative = il.DefineLabel();

        // if (value > 0) goto positive;
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Bgt, positive);

        // if (value < 0) goto negative;
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Blt, negative);

        // return "Zero";
        il.Emit(OpCodes.Ldstr, "Zero");
        il.Emit(OpCodes.Ret);

        // positive:
        il.MarkLabel(positive);
        il.Emit(OpCodes.Ldstr, "Positive");
        il.Emit(OpCodes.Ret);

        // negative:
        il.MarkLabel(negative);
        il.Emit(OpCodes.Ldstr, "Negative");
        il.Emit(OpCodes.Ret);

        return (Func<int, string>)dynamicMethod.CreateDelegate(typeof(Func<int, string>));
    }

    // Задание 2.1: n! рекурсивно
    // int Factorial(int n) { if (n <= 1) return 1; return n * Factorial(n - 1); }
    public static Func<int, int> CreateRecursiveFactorialMethod()
    {
        DynamicMethod dynamicMethod = new DynamicMethod(
            "RecursiveFactorial",
            typeof(int),
            new[] { typeof(int) },
            typeof(ILGeneratorDemo).Module
        );

        ILGenerator il = dynamicMethod.GetILGenerator();

        Label recurse = il.DefineLabel();

        // if (n > 1) goto recurse;
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Bgt, recurse);

        // return 1;
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Ret);

        // recurse: return n * Factorial(n - 1);
        il.MarkLabel(recurse);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Sub);
        il.Emit(OpCodes.Call, dynamicMethod);   // рекурсивный вызов самого метода
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Ret);

        return (Func<int, int>)dynamicMethod.CreateDelegate(typeof(Func<int, int>));
    }

    // Задание 2.2: максимум из трёх чисел
    // int Max(int a, int b, int c) { int max = a; if (b > max) max = b; if (c > max) max = c; return max; }
    public static Func<int, int, int, int> CreateMaxOfThreeMethod()
    {
        DynamicMethod dynamicMethod = new DynamicMethod(
            "DynamicMax",
            typeof(int),
            new[] { typeof(int), typeof(int), typeof(int) },
            typeof(ILGeneratorDemo).Module
        );

        ILGenerator il = dynamicMethod.GetILGenerator();

        Label skipB = il.DefineLabel();
        Label skipC = il.DefineLabel();

        il.DeclareLocal(typeof(int)); // max

        // max = a;
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Stloc_0);

        // if (b <= max) goto skipB; max = b;
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ble, skipB);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Stloc_0);
        il.MarkLabel(skipB);

        // if (c <= max) goto skipC; max = c;
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ble, skipC);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Stloc_0);
        il.MarkLabel(skipC);

        // return max;
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ret);

        return (Func<int, int, int, int>)dynamicMethod.CreateDelegate(typeof(Func<int, int, int, int>));
    }
}
