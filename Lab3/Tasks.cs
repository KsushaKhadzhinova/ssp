using System.Reflection;

namespace LabReflection
{
    // Задание 1: класс с тремя приватными полями разных типов и свойствами с валидацией
    public class ReflectionTarget
    {
        private int _age;
        private string _name;
        private double _rating;

        public ReflectionTarget()
        {
            _name = "Unknown";
        }

        public ReflectionTarget(int age, string name, double rating)
        {
            Age = age;
            Name = name;
            Rating = rating;
        }

        // Возраст не может быть меньше нуля
        public int Age
        {
            get { return _age; }
            set { _age = value < 0 ? 0 : value; }
        }

        // Пустое имя заменяется на Unknown
        public string Name
        {
            get { return _name; }
            set { _name = string.IsNullOrWhiteSpace(value) ? "Unknown" : value; }
        }

        // Рейтинг ограничивается диапазоном от 0 до 5
        public double Rating
        {
            get { return _rating; }
            set { _rating = value < 0 ? 0 : (value > 5 ? 5 : value); }
        }

    }

    // Задание 4: "чёрный ящик" с приватным полем и приватным методом
    public class SecretCalculator
    {
        private int _secretValue = 10;

        // Публичный метод для чтения значения (для проверки)
        public int ReadValue()
        {
            return _secretValue;
        }

        // Приватный метод с логикой вычислений
        private int Calculate(int a, int b)
        {
            return a * b + _secretValue;
        }

        private void Reset()
        {
            _secretValue = 0;
        }
    }

    public static class Tasks
    {
        // Задание 1: проверка валидации свойств
        public static void Task1()
        {
            Console.WriteLine("\n=== ЗАДАНИЕ 1: КЛАСС С ПРИВАТНЫМИ ПОЛЯМИ И СВОЙСТВАМИ ===\n");

            var target = new ReflectionTarget();
            target.Age = -5;
            target.Name = "";
            target.Rating = 10;
            Console.WriteLine("После Age=-5, Name=\"\", Rating=10:");
            Console.WriteLine($"  Age={target.Age}, Name={target.Name}, Rating={target.Rating}");

            target.Age = 20;
            target.Name = "Anna";
            target.Rating = 4.5;
            Console.WriteLine("После Age=20, Name=\"Anna\", Rating=4.5:");
            Console.WriteLine($"  Age={target.Age}, Name={target.Name}, Rating={target.Rating}");
        }

        // Задание 2: информация о всех методах класса
        public static void Task2()
        {
            Console.WriteLine("\n=== ЗАДАНИЕ 2: МЕТОДЫ КЛАССА ЧЕРЕЗ РЕФЛЕКСИЮ ===\n");

            // Класс DynamicCalculator содержит публичные, приватные, статические и экземплярные методы
            Type type = typeof(DynamicCalculator);

            // Все методы: публичные, приватные, статические, экземплярные
            MethodInfo[] allMethods = type.GetMethods(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.Static);

            Console.WriteLine("Все методы (BindingFlags.Public | NonPublic | Instance | Static):");
            foreach (MethodInfo method in allMethods)
            {
                // Отфильтровываем методы, сгенерированные для свойств
                if (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
                    continue;

                string access = method.IsPublic ? "public" : (method.IsPrivate ? "private" : "protected");
                string isStatic = method.IsStatic ? " static" : "";
                string parameters = string.Join(", ",
                    method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Console.WriteLine($"  {access}{isStatic} {method.ReturnType.Name} {method.Name}({parameters})");
            }

            // Разница между флагами привязки
            int publicInstance = type.GetMethods(BindingFlags.Public | BindingFlags.Instance).Length;
            int nonPublicInstance = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Length;
            int publicStatic = type.GetMethods(BindingFlags.Public | BindingFlags.Static).Length;
            Console.WriteLine("\nРазница между флагами привязки (с учётом get_/set_ и методов object):");
            Console.WriteLine($"  Public | Instance:    {publicInstance} методов");
            Console.WriteLine($"  NonPublic | Instance: {nonPublicInstance} методов");
            Console.WriteLine($"  Public | Static:     {publicStatic} методов");
        }

        // Задание 3: создание экземпляра через Activator.CreateInstance
        public static void Task3()
        {
            Console.WriteLine("\n=== ЗАДАНИЕ 3: Activator.CreateInstance ===\n");

            Type type = typeof(ReflectionTarget);

            // Способ 1: конструктор без параметров
            object target1 = Activator.CreateInstance(type);
            Console.WriteLine("Создан объект конструктором без параметров.");

            // Установка значений свойств через рефлексию
            type.GetProperty("Age").SetValue(target1, 30);
            type.GetProperty("Name").SetValue(target1, "Reflection object");
            type.GetProperty("Rating").SetValue(target1, 4.0);

            // Чтение значений через рефлексию
            Console.WriteLine($"  Age = {type.GetProperty("Age").GetValue(target1)}");
            Console.WriteLine($"  Name = {type.GetProperty("Name").GetValue(target1)}");
            Console.WriteLine($"  Rating = {type.GetProperty("Rating").GetValue(target1)}");

            // Способ 2: конструктор с параметрами
            object target2 = Activator.CreateInstance(type, new object[] { 25, "With params", 3.5 });
            Console.WriteLine("Создан объект конструктором с параметрами (25, \"With params\", 3.5).");
            Console.WriteLine($"  Age = {type.GetProperty("Age").GetValue(target2)}");
            Console.WriteLine($"  Name = {type.GetProperty("Name").GetValue(target2)}");
            Console.WriteLine($"  Rating = {type.GetProperty("Rating").GetValue(target2)}");
        }

        // Задание 4: "чёрный ящик"
        public static void Task4()
        {
            Console.WriteLine("\n=== ЗАДАНИЕ 4: ЧЁРНЫЙ ЯЩИК SecretCalculator ===\n");

            var calculator = new SecretCalculator();
            Console.WriteLine($"ReadValue() = {calculator.ReadValue()}");
            Console.WriteLine("Поле _secretValue и метод Calculate недоступны извне (private).");
        }

        // Задание 5: доступ к приватному полю и его изменение
        public static void Task5()
        {
            Console.WriteLine("\n=== ЗАДАНИЕ 5: ИЗМЕНЕНИЕ ПРИВАТНОГО ПОЛЯ ===\n");

            var calculator = new SecretCalculator();
            Type type = typeof(SecretCalculator);

            try
            {
                FieldInfo field = type.GetField("_secretValue",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (field == null)
                    throw new MissingFieldException("Поле _secretValue не найдено");

                Console.WriteLine($"Значение до изменения: {field.GetValue(calculator)}");
                field.SetValue(calculator, 25);
                Console.WriteLine($"Значение через публичный метод ReadValue() после SetValue: {calculator.ReadValue()}");
                Console.WriteLine("Рефлексия нарушила инкапсуляцию: приватное поле изменено.");

                // Поле, которого нет в классе
                FieldInfo missing = type.GetField("_unknown",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (missing == null)
                    throw new MissingFieldException("Поле _unknown не найдено");
            }
            catch (MissingFieldException ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
            catch (FieldAccessException ex)
            {
                Console.WriteLine($"Недостаточно прав: {ex.Message}");
            }
        }

        // Задание 6: вызов приватного метода
        public static void Task6()
        {
            Console.WriteLine("\n=== ЗАДАНИЕ 6: ВЫЗОВ ПРИВАТНОГО МЕТОДА ===\n");

            var calculator = new SecretCalculator();
            Type type = typeof(SecretCalculator);

            MethodInfo calculate = type.GetMethod("Calculate",
                BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo reset = type.GetMethod("Reset",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Способ 1: MethodInfo.Invoke, метод с двумя параметрами
            object result = calculate.Invoke(calculator, new object[] { 3, 4 });
            Console.WriteLine($"Способ 1 (Invoke) Calculate(3, 4) = {result}");

            // Способ 2: делегат
            Func<int, int, int> delegateMethod =
                (Func<int, int, int>)calculate.CreateDelegate(typeof(Func<int, int, int>), calculator);
            Console.WriteLine($"Способ 2 (делегат) Calculate(5, 6) = {delegateMethod(5, 6)}");

            // Метод без параметров
            Console.WriteLine($"Перед Reset(): ReadValue() = {calculator.ReadValue()}");
            reset.Invoke(calculator, null);
            Console.WriteLine($"После Reset() (Invoke без параметров): ReadValue() = {calculator.ReadValue()}");
        }
    }
}
