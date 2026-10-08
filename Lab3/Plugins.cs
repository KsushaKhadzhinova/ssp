using System.Reflection;

namespace LabReflection
{
    // ============ АТРИБУТЫ ============

    [AttributeUsage(AttributeTargets.Class)]
    public class PluginAttribute : Attribute
    {
        public string Name { get; }
        public PluginAttribute(string name) => Name = name;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class PluginMethodAttribute : Attribute
    {
        public string Description { get; }
        public PluginMethodAttribute(string description) => Description = description;
    }

    // ============ ИНТЕРФЕЙС ============

    public interface IPlugin
    {
        void Execute();
    }

    // ============ ПЛАГИНЫ ============

    [Plugin("MySimplePlugin")]
    public class MyPlugin : IPlugin
    {
        [PluginMethod("Основной метод")]
        public void Execute()
        {
            Console.WriteLine("Привет из плагина!");
        }

        [PluginMethod("Метод для сложения")]
        public int Add(int a, int b)
        {
            return a + b;
        }
    }

    [Plugin("Logger")]
    public class LoggerPlugin : IPlugin
    {
        [PluginMethod("Основной метод логгера")]
        public void Execute()
        {
            Console.WriteLine("[LOG] Logger plugin executed");
        }
    }

    // Новый плагин: для его подключения достаточно добавить имя в plugins.config
    [Plugin("Greeter")]
    public class GreeterPlugin : IPlugin
    {
        [PluginMethod("Основной метод приветствия")]
        public void Execute()
        {
            Console.WriteLine("Greeter plugin: добрый день!");
        }
    }

    // ============ ЗАГРУЗЧИК ПЛАГИНОВ ============

    public static class PluginLoader
    {
        // Загружает плагины, имена которых перечислены в конфигурационном файле
        public static void Load(string configPath)
        {
            string[] names = File.ReadAllLines(configPath)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .ToArray();
            Console.WriteLine($"Плагины в {Path.GetFileName(configPath)}: {string.Join(", ", names)}");

            // Сборка и все её типы
            Assembly assembly = Assembly.GetExecutingAssembly();
            Type[] types = assembly.GetTypes();

            foreach (Type type in types)
            {
                // Класс должен реализовывать интерфейс IPlugin
                if (!typeof(IPlugin).IsAssignableFrom(type) || type.IsInterface || type.IsAbstract)
                    continue;

                // Атрибут PluginAttribute
                PluginAttribute attribute = type.GetCustomAttribute<PluginAttribute>();
                if (attribute == null || !names.Contains(attribute.Name))
                    continue;

                // Создание экземпляра через Activator.CreateInstance
                IPlugin plugin = (IPlugin)Activator.CreateInstance(type);
                Console.WriteLine($"\nЗагружен плагин: {attribute.Name} ({type.Name})");

                // Вызов методов плагина через рефлексию
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    PluginMethodAttribute methodAttribute = method.GetCustomAttribute<PluginMethodAttribute>();
                    if (methodAttribute == null)
                        continue;

                    Console.WriteLine($"  Метод {method.Name}: {methodAttribute.Description}");
                    if (method.Name == "Add")
                        Console.WriteLine($"    Результат Add(5, 3) = {method.Invoke(plugin, new object[] { 5, 3 })}");
                    else
                        method.Invoke(plugin, null);
                }
            }
        }

        // Задание 7
        public static void Task7()
        {
            Console.WriteLine("\n=== ЗАДАНИЕ 7: СИСТЕМА ПЛАГИНОВ ===\n");

            string configPath = Path.Combine(AppContext.BaseDirectory, "plugins.config");
            string original = File.ReadAllText(configPath);
            Load(configPath);

            // Добавляем новый плагин в конфигурацию: основной код не меняется
            Console.WriteLine("\n--- В plugins.config добавлена строка Greeter ---\n");
            File.AppendAllText(configPath, "Greeter" + Environment.NewLine);
            Load(configPath);

            File.WriteAllText(configPath, original);
        }
    }
}
