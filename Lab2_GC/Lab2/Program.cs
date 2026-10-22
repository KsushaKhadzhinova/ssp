// Лабораторная работа 2. Управление памятью в C# и анализ утечек
// Все задания собраны в одном файле. Каждый пример методички лежит в своем
// пространстве имен, а его метод Main переименован в Run и вызывается из меню.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Lab2
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            while (true)
            {
                Console.WriteLine("\n=== Лабораторная работа 2: Управление памятью и анализ утечек ===");
                Console.WriteLine("1 - Статическая и экземплярная ссылки (демонстрация)");
                Console.WriteLine("2 - Задание 1. Базовый пример с утечкой памяти");
                Console.WriteLine("3 - Задания 2 и 3. MemoryMonitor : IDisposable и тестирование");
                Console.WriteLine("4 - Задание 4. Статическая утечка");
                Console.WriteLine("5 - Задание 4. Событийная утечка");
                Console.WriteLine("6 - Задание 4. Сравнение поколений объектов");
                Console.WriteLine("7 - Задание 5. Объекты в LOH");
                Console.WriteLine("8 - Задание 5. Мониторинг LOH");
                Console.WriteLine("9 - Задание 6. Сравнение производительности boxing");
                Console.WriteLine("10 - Задание 6. Оптимизация UnoptimizedCode");
                Console.WriteLine("11 - Задание 7. BuggyCode и исправленная версия");
                Console.WriteLine("0 - Выход");
                Console.Write("> ");

                string choice = Console.ReadLine();
                if (choice == null || choice == "0") break;

                switch (choice)
                {
                    case "1": StaticReference.Program.Run(); break;
                    case "2": Task1.Program.Run(); break;
                    case "3": Task2.Program.Run(); break;
                    case "4": Task4.StaticLeakDemo.Run(); break;
                    case "5": Task4.EventLeakDemo.Run(); break;
                    case "6": Task4.GenerationsDemo.Run(); break;
                    case "7": Task5.LOHResearch.Run(); break;
                    case "8": Task5.LOHMonitor.Run(); break;
                    case "9": Task6.BoxingComparison.Run(); break;
                    case "10": Task6.OptimizationDemo.Run(); break;
                    case "11": Task7.LeakSearch.Run(); break;
                    default: Console.WriteLine("Неизвестный пункт меню"); break;
                }
            }
        }
    }
}

// ===================== Статическая ссылка (пример из методички) =====================
namespace Lab2.StaticReference
{
    public class StaticReferenceDemo
    {
        // СТАТИЧЕСКАЯ ССЫЛКА - ОДНА для ВСЕХ объектов
        private static List<int> _staticList = new List<int>();

        // ЭКЗЕМПЛЯРНАЯ ССЫЛКА - своя для КАЖДОГО объекта
        private List<int> _instanceList = new List<int>();

        public void AddToStatic(int value)
        {
            _staticList.Add(value); // Все объекты используют ОДИН и тот же список
        }

        public void AddToInstance(int value)
        {
            _instanceList.Add(value); // Каждый объект использует СВОЙ список
        }

        public void PrintLists(string objectName)
        {
            Console.WriteLine($"{objectName}:");
            Console.WriteLine($" Статический список (общий): [{string.Join(", ", _staticList)}]");
            Console.WriteLine($" Экземплярный список (свой): [{string.Join(", ", _instanceList)}]");
            Console.WriteLine();
        }
    }

    class Program
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

            obj1.AddToStatic(100);   // Статический список для ВСЕХ объектов
            obj1.AddToInstance(1);   // Только для obj1

            obj2.AddToStatic(200);   // Добавляем в ТОТ ЖЕ статический список
            obj2.AddToInstance(2);   // Только для obj2

            obj3.AddToStatic(300);   // Опять в ТОТ ЖЕ статический список
            obj3.AddToInstance(3);   // Только для obj3

            // Выводим состояние каждого объекта
            obj1.PrintLists("Объект 1");
            obj2.PrintLists("Объект 2");
            obj3.PrintLists("Объект 3");

            Console.WriteLine("🔵 Статический список ОДИН для всех!");
            Console.WriteLine("🟢 Экземплярные списки РАЗНЫЕ для каждого!\n");

            // Демонстрация через статическое поле
            Console.WriteLine("2. Демонстрация через статическое поле:\n");
            StaticFieldDemo.ShowStaticFieldDemo();
        }
    }

    public class StaticFieldDemo
    {
        private static int _staticCounter = 0;   // ОДИН счетчик для всех
        private int _instanceCounter = 0;        // СВОЙ счетчик для каждого

        public void Increment()
        {
            _staticCounter++;    // Увеличивает ОБЩИЙ счетчик
            _instanceCounter++;  // Увеличивает СВОЙ счетчик
        }

        public void ShowCounters(string objectName)
        {
            Console.WriteLine($"{objectName}:");
            Console.WriteLine($" Статический счетчик (общий): {_staticCounter}");
            Console.WriteLine($" Экземплярный счетчик (свой): {_instanceCounter}");
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

            Console.WriteLine("\n✅ Статический счетчик ОДИН для обоих объектов!");
            Console.WriteLine("❌ Экземплярные счетчики РАЗНЫЕ для каждого!");
        }
    }
}

// ===================== Задание 1. Базовый пример с утечкой памяти =====================
namespace Lab2.Task1
{
    public class MemoryLeakExample
    {
        private static List<byte[]> _staticList = new List<byte[]>();

        public static void CreateMemoryLeak()
        {
            // Создаем объекты, которые никогда не будут собраны
            for (int i = 0; i < 1000; i++)
            {
                _staticList.Add(new byte[1024 * 10]); // 10KB каждый
            }
        }

        public static void ClearMemoryLeak()
        {
            _staticList.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    public class EventLeakExample
    {
        public event EventHandler BigEvent;

        public void SubscribeLeak()
        {
            // Подписка на событие без отписки
            BigEvent += (sender, e) =>
            {
                var data = new byte[1024 * 100];
            };
        }
    }

    public class MemoryMonitor
    {
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
            {
                list.Add(i);
            }
        }

        public void PrintMemoryInfo(string message = "")
        {
            if (!string.IsNullOrEmpty(message))
                Console.WriteLine($"\n=== {message} ===");

            Console.WriteLine($"Total Memory: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
            Console.WriteLine($"Gen0: {GC.CollectionCount(0)}, Gen1: {GC.CollectionCount(1)}, Gen2: {GC.CollectionCount(2)}");

            using (Process process = Process.GetCurrentProcess())
            {
                Console.WriteLine($"Working Set: {process.WorkingSet64 / (1024 * 1024)} MB");
            }
        }

        public void Cleanup()
        {
            _allocatedMemory.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    // Large Object Heap - пример
    public class LOHExample
    {
        public static void CreateLargeObjects()
        {
            // Объекты > 85KB попадают в LOH
            for (int i = 0; i < 100; i++)
            {
                var largeArray = new byte[100000]; // ~100KB
                // Используем largeArray
            }
        }
    }

    // Boxing и его влияние
    public class BoxingExample
    {
        private List<object> _boxedItems = new List<object>();
        private List<int> _unboxedItems = new List<int>();

        public void BoxingOperation()
        {
            // Упаковка - создает дополнительные объекты в куче
            for (int i = 0; i < 1000000; i++)
            {
                _boxedItems.Add(i); // int упаковывается в object
            }
        }

        public void UnboxedOperation()
        {
            // Без упаковки - эффективнее
            for (int i = 0; i < 1000000; i++)
            {
                _unboxedItems.Add(i);
            }
        }
    }

    class Program
    {
        public static void Run()
        {
            Console.WriteLine("=== Лабораторная работа: Управление памятью ===");

            var monitor = new MemoryMonitor();

            // 1. Тест памяти и LOH
            Console.WriteLine("\n1. Выделение памяти:");
            monitor.AllocateMemory(10);
            monitor.AllocateLOHObjects(100);
            monitor.PrintMemoryInfo("После выделения");

            // 2. Тест утечек
            Console.WriteLine("\n2. Тест утечек:");
            MemoryLeakExample.CreateMemoryLeak();

            var eventExample = new EventLeakExample();
            for (int i = 0; i < 100; i++)
                eventExample.SubscribeLeak();

            monitor.PrintMemoryInfo("После утечек");

            // 3. Тест Boxing
            Console.WriteLine("\n3. Тест упаковки:");
            var sw = Stopwatch.StartNew();
            monitor.SimulateBoxing(1000000);
            sw.Stop();
            Console.WriteLine($"Время с упаковкой: {sw.ElapsedMilliseconds}ms");

            // Без упаковки
            sw.Restart();
            var optimizedList = new List<int>();
            for (int i = 0; i < 1000000; i++)
                optimizedList.Add(i);
            sw.Stop();
            Console.WriteLine($"Время без упаковки: {sw.ElapsedMilliseconds}ms");

            // 4. Очистка
            Console.WriteLine("\n4. После очистки:");
            MemoryLeakExample.ClearMemoryLeak();
            monitor.Cleanup();
            monitor.PrintMemoryInfo("Очищено");

            // 5. Поколения объектов
            Console.WriteLine("\n5. Поколения объектов:");
            var obj = new object();
            Console.WriteLine($"Начальное поколение: {GC.GetGeneration(obj)}");
            GC.Collect();
            Console.WriteLine($"После GC: {GC.GetGeneration(obj)}");

            Console.WriteLine("\nНажмите любую клавишу...");
            Console.ReadKey();
        }
    }
}

// ===================== Задания 2 и 3. Класс мониторинга памяти и тестирование =====================
namespace Lab2.Task2
{
    public class MemoryMonitor : IDisposable
    {
        private bool _disposed;
        private List<byte[]> _allocatedMemory = new List<byte[]>();
        private Random _random = new Random();

        public void AllocateMemory(int sizeInMB)
        {
            // Выделяем память с возможной утечкой
            var data = new byte[sizeInMB * 1024 * 1024];
            _allocatedMemory.Add(data);
        }

        public void AllocateLOHObjects(int count)
        {
            for (int i = 0; i < count; i++)
            {
                // Объекты > 85KB
                var largeObject = new byte[90000 + i];
            }
        }

        public void SimulateBoxing()
        {
            var list = new List<object>();
            for (int i = 0; i < 1000000; i++)
            {
                list.Add(i); // Упаковка
            }
        }

        public void PrintMemoryInfo()
        {
            Console.WriteLine($"GC Generation: {GC.GetGeneration(this)}");
            Console.WriteLine($"Total Memory: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
            Console.WriteLine($"Collection Count Gen0: {GC.CollectionCount(0)}");
            Console.WriteLine($"Collection Count Gen1: {GC.CollectionCount(1)}");
            Console.WriteLine($"Collection Count Gen2: {GC.CollectionCount(2)}");
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

    class Program
    {
        public static void Run()
        {
            Console.WriteLine("=== Лабораторная работа: Управление памятью ===");

            using (var monitor = new MemoryMonitor())
            {
                // 1. Тестирование базовой памяти
                Console.WriteLine("\n1. Базовое выделение памяти:");
                monitor.AllocateMemory(10);
                monitor.PrintMemoryInfo();

                // 2. Тестирование LOH
                Console.WriteLine("\n2. Создание объектов в LOH:");
                monitor.AllocateLOHObjects(100);
                monitor.PrintMemoryInfo();

                // 3. Тестирование Boxing
                Console.WriteLine("\n3. Тест упаковки:");
                var stopwatch = Stopwatch.StartNew();
                monitor.SimulateBoxing();
                stopwatch.Stop();
                Console.WriteLine($"Время выполнения с упаковкой: {stopwatch.ElapsedMilliseconds}ms");

                // 4. Принудительная сборка
                Console.WriteLine("\n4. После принудительной сборки:");
                monitor.Cleanup();
                monitor.PrintMemoryInfo();

                // 5. Оптимизированная версия
                Console.WriteLine("\n5. Оптимизированная версия (без упаковки):");
                stopwatch.Restart();
                var optimizedList = new List<int>();
                for (int i = 0; i < 1000000; i++)
                {
                    optimizedList.Add(i);
                }
                stopwatch.Stop();
                Console.WriteLine($"Время выполнения без упаковки: {stopwatch.ElapsedMilliseconds}ms");
            }

            Console.WriteLine("\nНажмите любую клавишу для завершения...");
            Console.ReadKey();
        }
    }
}

// ===================== Задание 4. Статические ссылки, события, поколения =====================
namespace Lab2.Task4
{
    // Статическая утечка: список живет столько же, сколько приложение
    public class StaticLeakDemo
    {
        private static List<byte[]> _staticList = new List<byte[]>();

        public static void AddData()
        {
            for (int i = 0; i < 1000; i++)
            {
                _staticList.Add(new byte[1024 * 10]); // 10KB каждый
            }
        }

        public static void CheckMemory(string message)
        {
            Console.WriteLine($"\n=== {message} ===");
            Console.WriteLine($"Total Memory: {GC.GetTotalMemory(false) / 1024} KB");
            Console.WriteLine($"Объектов в статическом списке: {_staticList.Count}");
            Console.WriteLine($"Поколение статического списка: {GC.GetGeneration(_staticList)}");
        }

        public static void Run()
        {
            Console.WriteLine("=== ЗАДАНИЕ 4: СТАТИЧЕСКАЯ УТЕЧКА ===");

            CheckMemory("До добавления");

            AddData();
            CheckMemory("После AddData()");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            CheckMemory("После GC.Collect()");

            AddData();
            GC.Collect();
            CheckMemory("После второго AddData() и GC.Collect()");

            Console.WriteLine("\nОбъекты в статическом списке не собираются GC, память растет.");
        }
    }

    // Событийная утечка: издатель хранит ссылки на обработчики
    public class EventLeakDemo
    {
        public event EventHandler BigEvent;

        public void Subscribe()
        {
            var data = new byte[1024 * 100]; // 100KB, захватывается обработчиком

            BigEvent += (sender, e) =>
            {
                var temp = new byte[data.Length]; // большой объект в обработчике
            };
        }

        public void Unsubscribe()
        {
            if (BigEvent == null) return;

            foreach (EventHandler handler in BigEvent.GetInvocationList())
            {
                BigEvent -= handler;
            }
        }

        public int SubscribersCount => BigEvent?.GetInvocationList().Length ?? 0;

        public void RaiseEvent()
        {
            BigEvent?.Invoke(this, EventArgs.Empty);
        }

        public static void Run()
        {
            Console.WriteLine("=== ЗАДАНИЕ 4: СОБЫТИЙНАЯ УТЕЧКА ===");

            var demo = new EventLeakDemo();
            long before = GC.GetTotalMemory(true);
            Console.WriteLine($"\nПамять до подписки: {before / 1024} KB");

            for (int i = 0; i < 100; i++)
            {
                demo.Subscribe();
            }
            demo.RaiseEvent();

            long afterSubscribe = GC.GetTotalMemory(true);
            Console.WriteLine($"После 100 подписок и GC: {afterSubscribe / 1024} KB, подписчиков: {demo.SubscribersCount}");
            Console.WriteLine($"Рост памяти: {(afterSubscribe - before) / 1024} KB");

            demo.Unsubscribe();

            long afterUnsubscribe = GC.GetTotalMemory(true);
            Console.WriteLine($"После отписки и GC: {afterUnsubscribe / 1024} KB, подписчиков: {demo.SubscribersCount}");
            Console.WriteLine($"Освобождено: {(afterSubscribe - afterUnsubscribe) / 1024} KB");
        }
    }

    // Перемещение объектов между поколениями
    public class GenerationsDemo
    {
        public static void Run()
        {
            Console.WriteLine("=== ЗАДАНИЕ 4: СРАВНЕНИЕ ПОКОЛЕНИЙ ОБЪЕКТОВ ===\n");

            var obj = new object();
            Console.WriteLine($"Новый объект: поколение {GC.GetGeneration(obj)}");

            for (int i = 1; i <= 3; i++)
            {
                GC.Collect();
                Console.WriteLine($"После GC.Collect() №{i}: поколение {GC.GetGeneration(obj)}");
            }

            Console.WriteLine("\nОбъекты разных размеров:");
            var small = new byte[100];
            var medium = new byte[1024 * 10];
            var big = new byte[1024 * 50];
            var large = new byte[1024 * 90]; // > 85KB

            Console.WriteLine($"byte[100]       : поколение {GC.GetGeneration(small)}");
            Console.WriteLine($"byte[10KB]      : поколение {GC.GetGeneration(medium)}");
            Console.WriteLine($"byte[50KB]      : поколение {GC.GetGeneration(big)}");
            Console.WriteLine($"byte[90KB] (LOH): поколение {GC.GetGeneration(large)}");

            GC.Collect();
            Console.WriteLine("\nПосле GC.Collect():");
            Console.WriteLine($"byte[100]       : поколение {GC.GetGeneration(small)}");
            Console.WriteLine($"byte[10KB]      : поколение {GC.GetGeneration(medium)}");
            Console.WriteLine($"byte[50KB]      : поколение {GC.GetGeneration(big)}");
            Console.WriteLine($"byte[90KB] (LOH): поколение {GC.GetGeneration(large)}");

            Console.WriteLine("\nОбъекты перемещаются Gen0 -> Gen1 -> Gen2, объект > 85KB сразу в LOH (Gen2).");
        }
    }
}

// ===================== Задание 5. Исследование Large Object Heap =====================
namespace Lab2.Task5
{
    public class LOHResearch
    {
        // Выделяет 100 массивов по 100KB (объекты > 85KB попадают в LOH)
        public static List<byte[]> AllocateLargeArrays()
        {
            var arrays = new List<byte[]>();
            for (int i = 0; i < 100; i++)
            {
                arrays.Add(new byte[100 * 1024]);
            }
            return arrays;
        }

        public static void Run()
        {
            Console.WriteLine("=== ЗАДАНИЕ 5: ОБЪЕКТЫ В LOH ===\n");

            long before = GC.GetTotalMemory(true);
            Console.WriteLine($"Память до выделения: {before / 1024} KB");

            var arrays = AllocateLargeArrays();
            long after = GC.GetTotalMemory(false);
            Console.WriteLine($"После выделения 100 x 100KB: {after / 1024} KB (+{(after - before) / 1024} KB)");

            GC.Collect();
            long afterCollect = GC.GetTotalMemory(false);
            Console.WriteLine($"После GC.Collect() (ссылки живы): {afterCollect / 1024} KB, массивов: {arrays.Count}");

            arrays = null;
            GC.Collect();
            long afterRelease = GC.GetTotalMemory(false);
            Console.WriteLine($"После обнуления ссылки и GC.Collect(): {afterRelease / 1024} KB");

            Console.WriteLine("\nСравнение 90KB и 80KB:");
            var array90 = new byte[90 * 1024];
            var array80 = new byte[80 * 1024];
            Console.WriteLine($"byte[90KB] = {array90.Length} байт: поколение {GC.GetGeneration(array90)} (LOH)");
            Console.WriteLine($"byte[80KB] = {array80.Length} байт: поколение {GC.GetGeneration(array80)} (SOH)");
        }
    }

    public class LOHMonitor
    {
        public static void Run()
        {
            Console.WriteLine("=== ЗАДАНИЕ 5: МОНИТОРИНГ LOH ===\n");

            int[] sizesKB = { 10, 50, 90, 200 };
            var objects = new List<byte[]>();

            foreach (int size in sizesKB)
            {
                objects.Add(new byte[size * 1024]);
            }

            PrintGenerations(objects, "Сразу после создания");

            GC.Collect();
            PrintGenerations(objects, "После 1-го GC.Collect()");

            GC.Collect();
            PrintGenerations(objects, "После 2-го GC.Collect()");

            Console.WriteLine($"\nСборки: Gen0={GC.CollectionCount(0)}, Gen1={GC.CollectionCount(1)}, Gen2={GC.CollectionCount(2)}");
        }

        static void PrintGenerations(List<byte[]> objects, string message)
        {
            Console.WriteLine($"--- {message} ---");
            foreach (var obj in objects)
            {
                string heap = obj.Length > 85000 ? "LOH" : "SOH";
                Console.WriteLine($"{obj.Length / 1024,4} KB: поколение {GC.GetGeneration(obj)} ({heap})");
            }
        }
    }
}

// ===================== Задание 6. Устранение упаковки =====================
namespace Lab2.Task6
{
    public class BoxingComparison
    {
        const int Count = 1000000;

        public static List<object> ProcessWithBoxing(out long sum)
        {
            var list = new List<object>();
            for (int i = 0; i < Count; i++)
            {
                list.Add(i); // упаковка
            }

            sum = 0;
            foreach (object item in list)
            {
                sum += (int)item; // распаковка
            }
            return list;
        }

        public static List<int> ProcessWithoutBoxing(out long sum)
        {
            var list = new List<int>();
            for (int i = 0; i < Count; i++)
            {
                list.Add(i);
            }

            sum = 0;
            foreach (int item in list)
            {
                sum += item;
            }
            return list;
        }

        public static void Run()
        {
            Console.WriteLine("=== ЗАДАНИЕ 6: СРАВНЕНИЕ ПРОИЗВОДИТЕЛЬНОСТИ ===\n");

            ProcessWithBoxing(out _);      // прогрев JIT
            ProcessWithoutBoxing(out _);

            // память, которую занимают данные, измеряется через GC.GetTotalMemory, пока список жив
            long memBefore = GC.GetTotalMemory(true);
            var sw = Stopwatch.StartNew();
            var boxedList = ProcessWithBoxing(out long boxedSum);
            sw.Stop();
            long boxedTime = sw.ElapsedMilliseconds;
            long boxedMemory = GC.GetTotalMemory(true) - memBefore;
            GC.KeepAlive(boxedList);
            boxedList = null;

            memBefore = GC.GetTotalMemory(true);
            sw.Restart();
            var unboxedList = ProcessWithoutBoxing(out long unboxedSum);
            sw.Stop();
            long unboxedTime = sw.ElapsedMilliseconds;
            long unboxedMemory = GC.GetTotalMemory(true) - memBefore;
            GC.KeepAlive(unboxedList);

            Console.WriteLine($"{"Метод",-22}|{"Время, мс",10} |{"Память, KB",12} |{"Сумма",14}");
            Console.WriteLine(new string('-', 64));
            Console.WriteLine($"{"ProcessWithBoxing",-22}|{boxedTime,10} |{boxedMemory / 1024,12} |{boxedSum,14}");
            Console.WriteLine($"{"ProcessWithoutBoxing",-22}|{unboxedTime,10} |{unboxedMemory / 1024,12} |{unboxedSum,14}");
            Console.WriteLine(new string('-', 64));
            Console.WriteLine($"Ускорение: {(double)boxedTime / Math.Max(unboxedTime, 1):F1} раз, " +
                              $"экономия памяти: {(double)boxedMemory / unboxedMemory:F1} раз");
        }
    }

    // Код из методички с упаковкой
    public class UnoptimizedCode
    {
        private ArrayList _data = new ArrayList();

        public void AddData(int value)
        {
            _data.Add(value); // Упаковка!
        }

        public int GetSum()
        {
            int sum = 0;
            foreach (object item in _data) // Распаковка!
            {
                sum += (int)item;
            }
            return sum;
        }
    }

    // Оптимизированная версия: List<int> хранит int без упаковки
    public class OptimizedCode
    {
        private List<int> _data = new List<int>();

        public void AddData(int value)
        {
            _data.Add(value);
        }

        public int GetSum()
        {
            int sum = 0;
            foreach (int item in _data)
            {
                sum += item;
            }
            return sum;
        }
    }

    public class OptimizationDemo
    {
        const int Count = 1000000;

        public static void Run()
        {
            Console.WriteLine("=== ЗАДАНИЕ 6: ОПТИМИЗАЦИЯ UnoptimizedCode ===\n");

            Measure("UnoptimizedCode (ArrayList)", () =>
            {
                var code = new UnoptimizedCode();
                for (int i = 0; i < Count; i++) code.AddData(i % 100);
                return code.GetSum();
            });

            Measure("OptimizedCode (List<int>)", () =>
            {
                var code = new OptimizedCode();
                for (int i = 0; i < Count; i++) code.AddData(i % 100);
                return code.GetSum();
            });
        }

        static void Measure(string name, Func<int> action)
        {
            action(); // прогрев JIT
            GC.Collect();

            long memBefore = GC.GetTotalAllocatedBytes(true);
            var sw = Stopwatch.StartNew();
            int sum = action();
            sw.Stop();
            long memory = GC.GetTotalAllocatedBytes(true) - memBefore;

            Console.WriteLine($"{name,-28}: время {sw.ElapsedMilliseconds,4} мс, выделено {memory / 1024,6} KB, сумма {sum}");
        }
    }
}

// ===================== Задание 7. Поиск утечек в коде =====================
namespace Lab2.Task7
{
    // Код с утечками из методички
    public class BuggyCode
    {
        private static List<IDisposable> _cache = new List<IDisposable>();
        private static event EventHandler _globalEvent;
        private byte[] _data = new byte[1024 * 1024];

        public BuggyCode()
        {
            _globalEvent += OnEvent;
            _cache.Add(new MemoryStream(1024));
        }

        private void OnEvent(object sender, EventArgs e)
        {
            var temp = new byte[1024 * 100];
        }

        public void SubscribeAll()
        {
            for (int i = 0; i < 1000; i++)
            {
                new BuggyCode();
            }
        }

        public static int CacheCount => _cache.Count;
    }

    // Исправленная версия
    public class FixedCode : IDisposable
    {
        private static event EventHandler _globalEvent;
        private MemoryStream _stream = new MemoryStream(1024); // ресурс принадлежит объекту
        private byte[] _data = new byte[1024 * 1024];
        private bool _disposed;

        public FixedCode()
        {
            _globalEvent += OnEvent;
        }

        private void OnEvent(object sender, EventArgs e)
        {
            var temp = new byte[1024 * 100];
        }

        public void SubscribeAll()
        {
            for (int i = 0; i < 1000; i++)
            {
                using (var obj = new FixedCode())
                {
                    // работа с объектом
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _globalEvent -= OnEvent;  // отписка от статического события
                _stream.Dispose();        // освобождение IDisposable
                _stream = null;
                _data = null;
                _disposed = true;
            }
        }

        public static int SubscribersCount => _globalEvent?.GetInvocationList().Length ?? 0;
    }

    public class LeakSearch
    {
        public static void Run()
        {
            Console.WriteLine("=== ЗАДАНИЕ 7: ПОИСК УТЕЧЕК В КОДЕ ===\n");

            long before = GC.GetTotalMemory(true);
            using (var fixedCode = new FixedCode())
            {
                fixedCode.SubscribeAll();
            }
            long afterFixed = GC.GetTotalMemory(true);
            Console.WriteLine("FixedCode:");
            Console.WriteLine($" Память до: {before / (1024 * 1024)} MB, после SubscribeAll и GC: {afterFixed / (1024 * 1024)} MB");
            Console.WriteLine($" Подписчиков статического события: {FixedCode.SubscribersCount}");

            // SubscribeAll() у BuggyCode удерживает 1000 объектов по 1 MB (около 1 GB),
            // поэтому для наглядного замера создаем 200 объектов тем же способом
            before = GC.GetTotalMemory(true);
            for (int i = 0; i < 200; i++)
            {
                new BuggyCode();
            }
            long afterBuggy = GC.GetTotalMemory(true);
            Console.WriteLine("\nBuggyCode (200 объектов):");
            Console.WriteLine($" Память до: {before / (1024 * 1024)} MB, после создания и GC: {afterBuggy / (1024 * 1024)} MB");
            Console.WriteLine($" Объектов MemoryStream в статическом кэше: {BuggyCode.CacheCount}");
            Console.WriteLine($" Удержано памяти: {(afterBuggy - before) / (1024 * 1024)} MB");
        }
    }
}
