namespace Lab2;

// Этапы для снимков памяти (snapshot) в профилировщике:
// между этапами программа ждёт нажатия Enter
public static class Snapshots
{
    public static void Run()
    {
        var lohObjects = new List<byte[]>();

        Pause("Снимок 1: начальное состояние");

        StaticLeakDemo.AddData();                 // статический список: 1000 x 10KB
        var eventDemo = new EventLeakDemo();
        for (int i = 0; i < 100; i++)
            eventDemo.Subscribe();                // 100 подписок по 100KB
        for (int i = 0; i < 100; i++)
            lohObjects.Add(new byte[100 * 1024]); // 100 объектов LOH по 100KB

        Pause("Снимок 2: после создания утечек и объектов LOH");

        eventDemo.Unsubscribe();
        lohObjects.Clear();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        Pause("Снимок 3: после отписки и очистки списка (статический список остался)");

        GC.KeepAlive(eventDemo);
    }

    static void Pause(string message)
    {
        Console.WriteLine(message);
        Console.WriteLine("READY");
        Console.ReadLine();
    }
}
