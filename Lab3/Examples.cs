using System.Reflection;

namespace LabReflection
{
    // ===== Пример из методички: класс для исследования =====
    public class Product
    {
        // Поля
        private int _id;
        private string _name;
        protected double _price;
        public static int TotalProducts = 0;

        // Свойства
        public int Id { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public string Category { get; private set; }

        // Конструкторы
        public Product()
        {
            _id = 0;
            _name = "Unknown";
            Category = "Default";
            TotalProducts++;
        }

        public Product(int id, string name, double price)
        {
            _id = id;
            _name = name;
            _price = price;
            Id = id;
            Name = name;
            Price = price;
            Category = "Default";
            TotalProducts++;
        }

        // Методы
        public void DisplayInfo()
        {
            Console.WriteLine($"Product: {_name}, Price: {_price}");
        }

        public double ApplyDiscount(double percent)
        {
            return _price * (1 - percent / 100);
        }

        private void ValidatePrice()
        {
            if (_price < 0) _price = 0;
        }

        protected void UpdateCategory(string newCategory)
        {
            Category = newCategory;
        }

        public static int GetTotalCount()
        {
            return TotalProducts;
        }
    }

    // ===== Пример из методички: класс-калькулятор для позднего связывания =====
    public class DynamicCalculator
    {
        private int _result = 0;

        public DynamicCalculator()
        {
            Console.WriteLine("  [Конструктор DynamicCalculator без параметров]");
        }

        public DynamicCalculator(int initialValue)
        {
            _result = initialValue;
            Console.WriteLine($"  [Конструктор с параметром: начальное значение = {initialValue}]");
        }

        public int Add(int a, int b)
        {
            int res = a + b;
            Console.WriteLine($"  Add({a}, {b}) = {res}");
            return res;
        }

        public double Add(double a, double b)
        {
            double res = a + b;
            Console.WriteLine($"  Add({a}, {b}) = {res}");
            return res;
        }

        public int Multiply(int a, int b)
        {
            int res = a * b;
            Console.WriteLine($"  Multiply({a}, {b}) = {res}");
            return res;
        }

        private void LogOperation(string operation)
        {
            Console.WriteLine($"  [LOG] Выполнена операция: {operation}");
        }

        public int Result => _result;

        public void SetResult(int value)
        {
            _result = value;
            Console.WriteLine($"  Установлен результат: {_result}");
        }

        public static string GetVersion()
        {
            return "DynamicCalculator v1.0";
        }
    }

    public static class Examples
    {
        // ===== Пример: базовая инспекция типа (1.1 - 1.6) =====
        public static void InspectType()
        {
            Console.WriteLine("=== ПРИМЕР: БАЗОВАЯ ИНСПЕКЦИЯ ТИПА ===\n");

            Type productType = typeof(Product);

            // 1.1 Базовая информация о типе
            Console.WriteLine("1.1 Базовая информация:");
            Console.WriteLine($"  Имя типа: {productType.Name}");
            Console.WriteLine($"  Полное имя: {productType.FullName}");
            Console.WriteLine($"  Пространство имен: {productType.Namespace}");
            Console.WriteLine($"  Сборка: {productType.Assembly.GetName().Name}");
            Console.WriteLine($"  Базовый тип: {productType.BaseType?.Name ?? "None"}");

            // 1.2 Характеристики типа
            Console.WriteLine("\n1.2 Характеристики:");
            Console.WriteLine($"  IsClass: {productType.IsClass}");
            Console.WriteLine($"  IsAbstract: {productType.IsAbstract}");
            Console.WriteLine($"  IsSealed: {productType.IsSealed}");
            Console.WriteLine($"  IsPublic: {productType.IsPublic}");
            Console.WriteLine($"  IsValueType: {productType.IsValueType}");

            // 1.3 Конструкторы
            Console.WriteLine("\n1.3 Конструкторы:");
            foreach (ConstructorInfo ctor in productType.GetConstructors())
            {
                var parameters = string.Join(", ",
                    ctor.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Console.WriteLine($"  {ctor.Name}({parameters}) [Public: {ctor.IsPublic}]");
            }

            // 1.4 Методы (только первые 10)
            Console.WriteLine("\n1.4 Методы:");
            int methodCount = 0;
            foreach (MethodInfo method in productType.GetMethods())
            {
                if (methodCount++ < 10)
                {
                    var params_str = string.Join(", ",
                        method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    Console.WriteLine($"  {method.ReturnType.Name} {method.Name}({params_str})");
                }
            }
            Console.WriteLine($"  ... и еще {productType.GetMethods().Length - 10} методов");

            // 1.5 Свойства
            Console.WriteLine("\n1.5 Свойства:");
            foreach (PropertyInfo prop in productType.GetProperties())
            {
                string access = $"{(prop.CanRead ? "get" : "")}{(prop.CanWrite ? "set" : "")}";
                Console.WriteLine($"  {prop.PropertyType.Name} {prop.Name} {{ {access} }}");
            }

            // 1.6 Поля (включая приватные)
            Console.WriteLine("\n1.6 Поля:");
            foreach (FieldInfo field in productType.GetFields(BindingFlags.Public |
                                                              BindingFlags.NonPublic |
                                                              BindingFlags.Instance |
                                                              BindingFlags.Static))
            {
                string visibility = field.IsPublic ? "public" : (field.IsPrivate ? "private" : "protected");
                string isStatic = field.IsStatic ? "static" : "";
                Console.WriteLine($"  {visibility} {isStatic} {field.FieldType.Name} {field.Name}");
            }
        }

        // ===== Пример: работа с приватными членами =====
        public static void PrivateMembers()
        {
            Console.WriteLine("\n=== ПРИМЕР: РАБОТА С ПРИВАТНЫМИ ЧЛЕНАМИ ===\n");

            // Создаем экземпляр Product
            Product product = new Product(1, "Laptop", 1000.0);

            Type type = typeof(Product);

            // 2.1 Получение приватного поля _price
            Console.WriteLine("2.1 Работа с приватным полем _price:");

            FieldInfo priceField = type.GetField("_price",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (priceField != null)
            {
                double oldValue = (double)priceField.GetValue(product);
                Console.WriteLine($"  Текущее значение _price: {oldValue}");

                // Изменяем значение
                priceField.SetValue(product, 1500.0);
                double newValue = (double)priceField.GetValue(product);
                Console.WriteLine($"  Новое значение _price: {newValue}");
            }

            // 2.2 Вызов приватного метода ValidatePrice
            Console.WriteLine("\n2.2 Вызов приватного метода ValidatePrice:");

            MethodInfo validateMethod = type.GetMethod("ValidatePrice",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (validateMethod != null)
            {
                // Устанавливаем отрицательную цену
                priceField.SetValue(product, -500.0);
                Console.WriteLine($"  Цена до валидации: {priceField.GetValue(product)}");

                // Вызываем приватный метод
                validateMethod.Invoke(product, null);

                Console.WriteLine($"  Цена после валидации: {priceField.GetValue(product)}");
            }

            // 2.3 Получение приватного поля _name
            Console.WriteLine("\n2.3 Работа с приватным полем _name:");

            FieldInfo nameField = type.GetField("_name",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (nameField != null)
            {
                string currentName = (string)nameField.GetValue(product);
                Console.WriteLine($"  Текущее имя: {currentName}");

                nameField.SetValue(product, "Gaming Laptop");
                Console.WriteLine($"  Новое имя: {nameField.GetValue(product)}");
            }

            // 2.4 Вызов защищенного метода
            Console.WriteLine("\n2.4 Вызов защищенного метода UpdateCategory:");

            MethodInfo updateCategory = type.GetMethod("UpdateCategory",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (updateCategory != null)
            {
                PropertyInfo categoryProp = type.GetProperty("Category");
                Console.WriteLine($"  Категория до: {categoryProp.GetValue(product)}");

                updateCategory.Invoke(product, new object[] { "Electronics" });

                Console.WriteLine($"  Категория после: {categoryProp.GetValue(product)}");
            }
        }

        // ===== Пример: позднее связывание =====
        public static void LateBinding()
        {
            Console.WriteLine("\n=== ПРИМЕР: ПОЗДНЕЕ СВЯЗЫВАНИЕ ===\n");

            // 3.1 Получение типа через имя (позднее связывание)
            Console.WriteLine("3.1 Динамическое получение типа:");

            string typeName = "LabReflection.DynamicCalculator";
            Type calcType = Type.GetType(typeName);

            if (calcType != null)
            {
                Console.WriteLine($"  Тип найден: {calcType.Name}");
                Console.WriteLine($"  Полное имя: {calcType.FullName}");
            }

            // 3.2 Динамическое создание экземпляра
            Console.WriteLine("\n3.2 Динамическое создание экземпляра:");

            // Через конструктор без параметров
            object calculator = Activator.CreateInstance(calcType);
            Console.WriteLine($"  Экземпляр создан: {calculator.GetType().Name}");

            // 3.3 Динамический вызов методов
            Console.WriteLine("\n3.3 Динамический вызов методов:");

            // Вызов Add(int, int)
            MethodInfo addMethod = calcType.GetMethod("Add", new[] { typeof(int), typeof(int) });
            if (addMethod != null)
            {
                int result = (int)addMethod.Invoke(calculator, new object[] { 10, 20 });
                Console.WriteLine($"  Результат Add(10,20): {result}");
            }

            // Вызов Add(double, double)
            MethodInfo addDoubleMethod = calcType.GetMethod("Add", new[] { typeof(double), typeof(double) });
            if (addDoubleMethod != null)
            {
                double result = (double)addDoubleMethod.Invoke(calculator, new object[] { 5.5, 3.2 });
                Console.WriteLine($"  Результат Add(5.5,3.2): {result}");
            }

            // 3.4 Динамический вызов с параметрами разного типа
            Console.WriteLine("\n3.4 Вызов с параметрами разного типа:");

            MethodInfo multiplyMethod = calcType.GetMethod("Multiply");
            if (multiplyMethod != null)
            {
                int result = (int)multiplyMethod.Invoke(calculator, new object[] { 7, 8 });
                Console.WriteLine($"  Результат Multiply(7,8): {result}");
            }

            // 3.5 Работа со свойствами
            Console.WriteLine("\n3.5 Работа со свойствами через рефлексию:");

            PropertyInfo resultProp = calcType.GetProperty("Result");
            if (resultProp != null)
            {
                int currentResult = (int)resultProp.GetValue(calculator);
                Console.WriteLine($"  Текущий Result: {currentResult}");
            }

            MethodInfo setResultMethod = calcType.GetMethod("SetResult");
            if (setResultMethod != null)
            {
                setResultMethod.Invoke(calculator, new object[] { 999 });
                int newResult = (int)resultProp.GetValue(calculator);
                Console.WriteLine($"  Result после SetResult: {newResult}");
            }

            // 3.6 Вызов приватного метода
            Console.WriteLine("\n3.6 Вызов приватного метода:");

            MethodInfo logMethod = calcType.GetMethod("LogOperation",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (logMethod != null)
            {
                logMethod.Invoke(calculator, new object[] { "Тестовая операция" });
                Console.WriteLine("  Приватный метод LogOperation вызван успешно");
            }

            // 3.7 Вызов статического метода
            Console.WriteLine("\n3.7 Вызов статического метода:");

            MethodInfo versionMethod = calcType.GetMethod("GetVersion",
                BindingFlags.Public | BindingFlags.Static);
            if (versionMethod != null)
            {
                string version = (string)versionMethod.Invoke(null, null);
                Console.WriteLine($"  Версия: {version}");
            }

            // 3.8 Создание экземпляра через конструктор с параметром
            Console.WriteLine("\n3.8 Создание экземпляра через конструктор с параметром:");

            ConstructorInfo ctor = calcType.GetConstructor(new[] { typeof(int) });
            if (ctor != null)
            {
                object calcWithParam = ctor.Invoke(new object[] { 42 });
                Console.WriteLine($"  Экземпляр с параметром создан");

                PropertyInfo resProp = calcType.GetProperty("Result");
                Console.WriteLine($"  Начальное значение Result: {resProp.GetValue(calcWithParam)}");
            }
        }
    }
}
