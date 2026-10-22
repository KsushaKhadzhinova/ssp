// Лабораторная работа 6 (Лабораторная работа 5. Часть 2)
// Параллельное программирование в C# с использованием TPL, PLINQ и разделения данных
// Все задания собраны в одном файле. Метод Main каждого задания методички
// переименован в Run и вызывается из общего меню.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LabWork_ParallelProgramming
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            while (true)
            {
                Console.WriteLine("\n=== Лабораторная работа 6: Параллельное программирование (TPL, PLINQ) ===");
                Console.WriteLine($"Логических процессоров: {Environment.ProcessorCount}");
                Console.WriteLine("1 - Задание 1. Параллельные циклы");
                Console.WriteLine("2 - Задание 2. Parallel.ForEach с коллекциями");
                Console.WriteLine("3 - Задание 3. PLINQ для анализа данных");
                Console.WriteLine("4 - Задание 4. Разделение данных и агрегация");
                Console.WriteLine("5 - Задание 5. Комплексная обработка изображений");
                Console.WriteLine("6 - Вариант 2. Параллельная фильтрация коллекций");
                Console.WriteLine("0 - Выход");
                Console.Write("> ");

                string choice = Console.ReadLine();
                if (choice == null || choice == "0") break;

                switch (choice)
                {
                    case "1": Task1.Run(); break;
                    case "2": Task2.Run(); break;
                    case "3": Task3.Run(); break;
                    case "4": Task4.Run(); break;
                    case "5": Task5.Run(); break;
                    case "6": Variant2.Run(); break;
                    default: Console.WriteLine("Неизвестный пункт меню"); break;
                }
            }
        }
    }

    // ===================== Задание 1. Введение в параллельные циклы =====================
    class Task1
    {
        public static void Run()
        {
            Console.WriteLine("=== ЗАДАНИЕ 1: ПАРАЛЛЕЛЬНЫЕ ЦИКЛЫ ===\n");

            // Создание большого массива данных
            const int N = 10000000;
            double[] data = new double[N];
            Random rand = new Random();

            // Инициализация данных
            for (int i = 0; i < N; i++)
            {
                data[i] = rand.NextDouble() * 1000;
            }

            // --- Последовательный цикл ---
            Stopwatch sw = Stopwatch.StartNew();
            double sumSeq = 0;
            for (int i = 0; i < N; i++)
            {
                sumSeq += Math.Sqrt(Math.Log(data[i] + 1));
            }
            sw.Stop();
            Console.WriteLine($"Последовательно: {sw.ElapsedMilliseconds} мс, Результат: {sumSeq:F2}");

            // --- Параллельный цикл (Parallel.For) ---
            sw.Restart();
            double sumPar = 0;
            object lockObj = new object();
            Parallel.For(0, N, i =>
            {
                double value = Math.Sqrt(Math.Log(data[i] + 1));
                lock (lockObj)
                {
                    sumPar += value;
                }
            });
            sw.Stop();
            Console.WriteLine($"Параллельно (с блокировкой): {sw.ElapsedMilliseconds} мс, Результат: {sumPar:F2}");

            // --- Параллельный цикл с локальными для потока переменными (оптимизированный) ---
            sw.Restart();
            double sumParLocal = 0;
            Parallel.For(0, N,
                () => 0.0, // Локальный инициализатор
                (i, state, local) => local + Math.Sqrt(Math.Log(data[i] + 1)), // Тело
                local => { lock (lockObj) { sumParLocal += local; } } // Агрегатор
            );
            sw.Stop();
            Console.WriteLine($"Параллельно (локальные переменные): {sw.ElapsedMilliseconds} мс, Результат: {sumParLocal:F2}");

            Console.WriteLine("\n=== Задание 1 Выполнено ===\n");
        }
    }

    // ===================== Задание 2. Parallel.ForEach с коллекциями =====================
    class Task2
    {
        public static void Run()
        {
            Console.WriteLine("=== ЗАДАНИЕ 2: ПАРАЛЛЕЛЬНАЯ ОБРАБОТКА ДАННЫХ ===\n");

            // Генерация данных
            const int N = 10_000_000;
            int[] numbers = new int[N];
            Random rand = new Random();
            for (int i = 0; i < N; i++) numbers[i] = rand.Next(1, 1000);

            // 1. Трансформация (Parallel.For)
            Console.WriteLine("--- Трансформация данных ---");
            double[] transformed = new double[N];
            Stopwatch sw = Stopwatch.StartNew();
            Parallel.For(0, N, i => transformed[i] = Math.Sqrt(Math.Pow(numbers[i], 2) + 1));
            sw.Stop();
            Console.WriteLine($"Время: {sw.ElapsedMilliseconds} мс");
            Console.WriteLine($"Пример: {transformed[0]:F2}, {transformed[1000]:F2}, {transformed[5000]:F2}\n");

            // 2. Фильтрация (Parallel.ForEach с локальными переменными)
            Console.WriteLine("--- Фильтрация (>500 и кратные 7) ---");
            List<int> filtered = new List<int>();
            object listLock = new object();
            var ranges = Enumerable.Range(0, N).Select(i => numbers[i]);

            sw.Restart();
            Parallel.ForEach(ranges,
                () => new List<int>(),
                (val, state, local) =>
                {
                    if (val > 500 && val % 7 == 0) local.Add(val);
                    return local;
                },
                local => { lock (listLock) filtered.AddRange(local); }
            );
            sw.Stop();
            Console.WriteLine($"Найдено: {filtered.Count}, Время: {sw.ElapsedMilliseconds} мс");
            Console.WriteLine($"Примеры: {string.Join(", ", filtered.Take(5))}\n");

            // 3. Поиск с отменой
            Console.WriteLine("--- Поиск с отменой ---");
            int[] targets = { 777, 999, 123 };
            bool[] found = new bool[targets.Length];
            int[] positions = new int[targets.Length];
            CancellationTokenSource cts = new CancellationTokenSource();

            sw.Restart();
            try
            {
                Parallel.For(0, N, new ParallelOptions { CancellationToken = cts.Token }, (i, state) =>
                {
                    for (int j = 0; j < targets.Length; j++)
                    {
                        if (!found[j] && numbers[i] == targets[j])
                        {
                            lock (found)
                            {
                                if (!found[j])
                                {
                                    found[j] = true;
                                    positions[j] = i;
                                    Console.WriteLine($"✅ Найдено {targets[j]} на позиции {i}");
                                    if (found.All(f => f)) { cts.Cancel(); state.Break(); return; }
                                }
                            }
                        }
                    }
                });
            }
            catch (OperationCanceledException) { Console.WriteLine("✅ Все найдено!"); }
            sw.Stop();
            Console.WriteLine($"Время поиска: {sw.ElapsedMilliseconds} мс\n");

            // 4. Сравнение производительности
            Console.WriteLine("--- Сравнение: блокировка vs локальные ---");
            long sumLock = 0, sumLocal = 0;
            object lockObj = new object();

            // С блокировкой
            sw.Restart();
            Parallel.For(0, N, i => { lock (lockObj) sumLock += numbers[i] * numbers[i]; });
            sw.Stop();
            Console.WriteLine($"С блокировкой: {sw.ElapsedMilliseconds} мс");

            // С локальными переменными
            sw.Restart();
            Parallel.For(0, N,
                () => 0L,
                (i, state, local) => local + numbers[i] * numbers[i],
                local => { lock (lockObj) sumLocal += local; }
            );
            sw.Stop();
            Console.WriteLine($"С локальными: {sw.ElapsedMilliseconds} мс");
            Console.WriteLine($"Результаты: {sumLock:N0} = {sumLocal:N0}\n");

            // 5. Интерактивный поиск
            Console.WriteLine("=== Интерактивный поиск ===");
            Console.WriteLine("Введите число (или 'exit'):");
            while (true)
            {
                Console.Write("> ");
                string input = Console.ReadLine();
                if (input?.ToLower() == "exit") break;

                if (int.TryParse(input, out int search))
                {
                    bool foundVal = false;
                    int pos = -1;
                    Parallel.For(0, N, (i, state) =>
                    {
                        if (numbers[i] == search) { pos = i; foundVal = true; state.Break(); }
                    });
                    Console.WriteLine(foundVal ? $"✅ Найдено на позиции {pos}" : "❌ Не найдено");
                }
            }

            Console.WriteLine("\n=== Задание 2 Выполнено ===");
            Console.ReadKey();
        }
    }

    // ===================== Задание 3. PLINQ для анализа данных =====================
    class Task3
    {
        public static void Run()
        {
            Console.WriteLine("=== ЗАДАНИЕ 3: PLINQ АНАЛИЗ ДАННЫХ ===\n");

            // Генерация большого набора данных
            const int N = 10000000;
            Random rand = new Random();
            var data = Enumerable.Range(0, N)
                .Select(i => rand.NextDouble() * 1000)
                .ToList();

            // --- Последовательный LINQ ---
            Stopwatch sw = Stopwatch.StartNew();
            var seqResult = data
                .Where(x => x > 500)
                .Select(x => Math.Sqrt(x))
                .Average();
            sw.Stop();
            Console.WriteLine($"Последовательный LINQ: {sw.ElapsedMilliseconds} мс, Результат: {seqResult:F4}");

            // --- PLINQ (базовый) ---
            sw.Restart();
            var plinqResult = data
                .AsParallel()
                .Where(x => x > 500)
                .Select(x => Math.Sqrt(x))
                .Average();
            sw.Stop();
            Console.WriteLine($"PLINQ (по умолчанию): {sw.ElapsedMilliseconds} мс, Результат: {plinqResult:F4}");

            // --- PLINQ со степенью параллелизма ---
            sw.Restart();
            var plinqResult2 = data
                .AsParallel()
                .WithDegreeOfParallelism(4)
                .Where(x => x > 500)
                .Select(x => Math.Sqrt(x))
                .Average();
            sw.Stop();
            Console.WriteLine($"PLINQ (степень=4): {sw.ElapsedMilliseconds} мс, Результат: {plinqResult2:F4}");

            // --- PLINQ с сохранением порядка ---
            sw.Restart();
            var plinqOrdered = data
                .AsParallel()
                .AsOrdered()
                .Where(x => x > 500)
                .Select(x => x * 2)
                .Take(5)
                .ToList();
            sw.Stop();
            Console.WriteLine($"PLINQ (упорядоченный): {sw.ElapsedMilliseconds} мс");
            Console.WriteLine($"Первые 5 результатов: {string.Join(", ", plinqOrdered)}");

            Console.WriteLine("\n=== Задание 3 Выполнено ===\n");
        }
    }

    // ===================== Задание 4. Разделение данных и агрегация =====================
    class Task4
    {
        public static void Run()
        {
            Console.WriteLine("=== ЗАДАНИЕ 4: РАЗДЕЛЕНИЕ ДАННЫХ ===\n");

            const int N = 10000000;
            int[] data = new int[N];
            Random rand = new Random();
            for (int i = 0; i < N; i++)
            {
                data[i] = rand.Next(1, 100);
            }

            // --- Разделение на диапазоны (по умолчанию) ---
            Stopwatch sw = Stopwatch.StartNew();
            long sumRange = 0;
            Parallel.For(0, N, i =>
            {
                Interlocked.Add(ref sumRange, data[i]);
            });
            sw.Stop();
            Console.WriteLine($"Разделение на диапазоны: {sw.ElapsedMilliseconds} мс, Сумма: {sumRange}");

            // --- Пользовательское разделение с Partitioner ---
            sw.Restart();
            long sumCustom = 0;
            var partitioner = Partitioner.Create(0, N, 100000);
            Parallel.ForEach(partitioner, range =>
            {
                long localSum = 0;
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    localSum += data[i];
                }
                Interlocked.Add(ref sumCustom, localSum);
            });
            sw.Stop();
            Console.WriteLine($"Пользовательское разделение: {sw.ElapsedMilliseconds} мс, Сумма: {sumCustom}");

            // --- Локальная для потока агрегация ---
            sw.Restart();
            long sumLocal = 0;
            Parallel.For(0, N,
                () => 0L,
                (i, state, local) => local + data[i],
                local => Interlocked.Add(ref sumLocal, local)
            );
            sw.Stop();
            Console.WriteLine($"Локальная агрегация: {sw.ElapsedMilliseconds} мс, Сумма: {sumLocal}");

            Console.WriteLine("\n=== Задание 4 Выполнено ===\n");
        }
    }

    // ===================== Задание 5. Комплексная обработка изображений =====================
    class Task5
    {
        // Симуляция функции обработки пикселя
        static double ProcessPixel(double pixel)
        {
            // Сложное вычисление, имитирующее фильтр изображения
            return Math.Sqrt(Math.Abs(Math.Sin(pixel) * Math.Cos(pixel) * 1000));
        }

        public static void Run()
        {
            Console.WriteLine("=== ЗАДАНИЕ 5: ПАРАЛЛЕЛЬНАЯ ОБРАБОТКА ИЗОБРАЖЕНИЙ ===\n");

            // Симуляция изображения как двумерного массива
            const int width = 2000;
            const int height = 2000;
            double[,] image = new double[height, width];
            double[,] result = new double[height, width];
            Random rand = new Random();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    image[y, x] = rand.NextDouble();
                }
            }

            // --- Последовательная обработка ---
            Stopwatch sw = Stopwatch.StartNew();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    result[y, x] = ProcessPixel(image[y, x]);
                }
            }
            sw.Stop();
            Console.WriteLine($"Последовательно: {sw.ElapsedMilliseconds} мс");

            // --- Параллельная обработка (по строкам) ---
            sw.Restart();
            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    result[y, x] = ProcessPixel(image[y, x]);
                }
            });
            sw.Stop();
            Console.WriteLine($"Параллельно (по строкам): {sw.ElapsedMilliseconds} мс");

            // --- Параллельная обработка с разделением ---
            sw.Restart();
            var partitioner = Partitioner.Create(0, height, 100);
            Parallel.ForEach(partitioner, range =>
            {
                for (int y = range.Item1; y < range.Item2; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        result[y, x] = ProcessPixel(image[y, x]);
                    }
                }
            });
            sw.Stop();
            Console.WriteLine($"Параллельно (с разделением): {sw.ElapsedMilliseconds} мс");

            // --- Обработка с помощью PLINQ ---
            sw.Restart();
            var pixels = Enumerable.Range(0, height * width)
                .AsParallel()
                .Select(i => ProcessPixel(image[i / width, i % width]))
                .ToArray();
            sw.Stop();
            Console.WriteLine($"PLINQ: {sw.ElapsedMilliseconds} мс");

            Console.WriteLine("\n=== Задание 5 Выполнено ===\n");
        }
    }

    // ===================== Вариант 2. Параллельная фильтрация коллекций =====================
    // PLINQ и пользовательские условия фильтрации
    class Variant2
    {
        // Пользовательское условие: число простое (затратная проверка делением)
        static bool IsPrime(int n)
        {
            if (n < 2) return false;
            for (int d = 2; (long)d * d <= n; d++)
            {
                if (n % d == 0) return false;
            }
            return true;
        }

        // Пользовательское условие: сумма цифр числа кратна заданному значению
        static bool DigitSumDivisibleBy(int n, int divisor)
        {
            int sum = 0;
            while (n > 0)
            {
                sum += n % 10;
                n /= 10;
            }
            return sum % divisor == 0;
        }

        public static void Run()
        {
            Console.WriteLine("=== ВАРИАНТ 2: ПАРАЛЛЕЛЬНАЯ ФИЛЬТРАЦИЯ КОЛЛЕКЦИЙ ===\n");

            // Генерация данных
            const int N = 5_000_000;
            Random rand = new Random();
            List<int> data = Enumerable.Range(0, N)
                .Select(i => rand.Next(1, 100_000_000))
                .ToList();

            // Набор пользовательских условий фильтрации
            var conditions = new List<(string Name, Func<int, bool> Predicate)>
            {
                ("Четные числа", x => x % 2 == 0),
                ("Простые числа", x => IsPrime(x)),
                ("Простые и сумма цифр кратна 5", x => IsPrime(x) && DigitSumDivisibleBy(x, 5))
            };

            foreach (var condition in conditions)
            {
                Console.WriteLine($"--- Условие: {condition.Name} ---");

                // --- Последовательный LINQ ---
                Stopwatch sw = Stopwatch.StartNew();
                List<int> seqResult = data.Where(condition.Predicate).ToList();
                sw.Stop();
                long seqTime = sw.ElapsedMilliseconds;
                Console.WriteLine($"LINQ последовательно:   {seqTime,6} мс, найдено: {seqResult.Count}");

                // --- PLINQ (по умолчанию) ---
                sw.Restart();
                List<int> plinqResult = data.AsParallel().Where(condition.Predicate).ToList();
                sw.Stop();
                long plinqTime = sw.ElapsedMilliseconds;
                Console.WriteLine($"PLINQ:                  {plinqTime,6} мс, найдено: {plinqResult.Count}");

                // --- PLINQ с сохранением порядка ---
                sw.Restart();
                List<int> orderedResult = data.AsParallel().AsOrdered().Where(condition.Predicate).ToList();
                sw.Stop();
                Console.WriteLine($"PLINQ AsOrdered:        {sw.ElapsedMilliseconds,6} мс, первые: {string.Join(", ", orderedResult.Take(3))}");

                // --- Parallel.ForEach с локальными списками ---
                sw.Restart();
                List<int> forEachResult = new List<int>();
                object listLock = new object();
                Parallel.ForEach(data,
                    () => new List<int>(),
                    (val, state, local) =>
                    {
                        if (condition.Predicate(val)) local.Add(val);
                        return local;
                    },
                    local => { lock (listLock) forEachResult.AddRange(local); }
                );
                sw.Stop();
                Console.WriteLine($"Parallel.ForEach local: {sw.ElapsedMilliseconds,6} мс, найдено: {forEachResult.Count}");

                bool same = seqResult.SequenceEqual(orderedResult) && plinqResult.Count == seqResult.Count
                            && forEachResult.Count == seqResult.Count;
                Console.WriteLine($"Результаты совпадают: {same}, ускорение PLINQ: {(double)seqTime / Math.Max(plinqTime, 1):F1} раз\n");
            }

            // --- Степень параллелизма для самого затратного условия ---
            Console.WriteLine("--- Простые числа: зависимость от степени параллелизма ---");
            for (int degree = 1; degree <= Environment.ProcessorCount; degree *= 2)
            {
                Stopwatch sw = Stopwatch.StartNew();
                int count = data.AsParallel().WithDegreeOfParallelism(degree).Count(x => IsPrime(x));
                sw.Stop();
                Console.WriteLine($"WithDegreeOfParallelism({degree,2}): {sw.ElapsedMilliseconds,6} мс, найдено: {count}");
            }

            Console.WriteLine("\n=== Вариант 2 Выполнен ===\n");
        }
    }
}
