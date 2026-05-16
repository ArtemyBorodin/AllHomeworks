// часть 1 Интерфейс и события 
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Timers;

public interface IJournalEntry
{
    string ToLogLine();
    string ToScreenLine();
}

//  событие
public class PlacedEvent : IJournalEntry
{
    public DateTime Time { get; } // запоминает время твоего действия  
    public string ShelfId { get; }
    public int SlotNumber { get; }
    public string ProductName { get; }

    // это конструктор 
    public PlacedEvent(string shelfId, int slotNumber, string productName)
    {
        Time = DateTime.Now;
        ShelfId = shelfId;
        SlotNumber = slotNumber;
        ProductName = productName;
    }

    // 2 метода, которые я обязан написать по договорённости interface IJournalEntry
    public string ToLogLine()
    {
        return $"{Time:yyyy-MM-dd HH:mm:ss}|PLACED|{ShelfId}|{SlotNumber}|{ProductName}";
    }

    public string ToScreenLine()
    {
        return $"[{Time:HH:mm:ss}] ПОЛОЖИЛИ: на полку {ShelfId}, слот {SlotNumber} -> {ProductName}";
    }

    public static PlacedEvent FromLogLine(string line)
    {
        var parts = line.Split('|');
        DateTime time = DateTime.Parse(parts[0]);
        string shelfId = parts[2];
        int slotNumber = int.Parse(parts[3]);
        string productName = parts[4];

        var evt = new PlacedEvent(shelfId, slotNumber, productName);
        return evt;
    }
}

// событие 
public class TakenEvent : IJournalEntry
{
    public DateTime Time { get; }
    public string ShelfId { get; }
    public int SlotNumber { get; }
    public string ProductName { get; }

    public TakenEvent(string shelfId, int slotNumber, string productName)
    {
        Time = DateTime.Now;
        ShelfId = shelfId;
        SlotNumber = slotNumber;
        ProductName = productName;
    }

    public string ToLogLine() { return $"{Time:yyyy-MM-dd HH:mm:ss}|TAKEN|{ShelfId}|{SlotNumber}|{ProductName}"; }
    public string ToScreenLine() { return $"[{Time:HH:mm:ss}] ЗАБРАЛИ: с полки {ShelfId}, слот {SlotNumber} -> {ProductName}"; }

    public static TakenEvent FromLogLine(string line)
    {
        var parts = line.Split('|');
        DateTime time = DateTime.Parse(parts[0]);
        string shelfId = parts[2];
        int slotNumber = int.Parse(parts[3]);
        string productName = parts[4];

        var evt = new TakenEvent(shelfId, slotNumber, productName);
        return evt;
    }
}

// событие 
public class MovedEvent : IJournalEntry
{
    public DateTime Time { get; }
    public string FromShelf { get; }   // откуда
    public int FromSlot { get; }       // откуда
    public string ToShelf { get; }     // куда
    public int ToSlot { get; }         // куда
    public string ProductName { get; }

    public MovedEvent(string fromShelf, int fromSlot, string toShelf, int toSlot, string productName)
    {
        Time = DateTime.Now;
        FromShelf = fromShelf;
        FromSlot = fromSlot;
        ToShelf = toShelf;
        ToSlot = toSlot;
        ProductName = productName;
    }

    public string ToLogLine() { return $"{Time:yyyy-MM-dd HH:mm:ss}|MOVED|{FromShelf}|{FromSlot}|{ToShelf}|{ToSlot}|{ProductName}"; }
    public string ToScreenLine() { return $"[{Time:HH:mm:ss}] ПЕРЕНЕСЛИ: {ProductName} с {FromShelf}{FromSlot} на {ToShelf}{ToSlot}"; }

    public static MovedEvent FromLogLine(string line)
    {
        var parts = line.Split('|');
        DateTime time = DateTime.Parse(parts[0]);
        string fromShelf = parts[2];
        int fromSlot = int.Parse(parts[3]);
        string toShelf = parts[4];
        int toSlot = int.Parse(parts[5]);
        string productName = parts[6];

        var evt = new MovedEvent(fromShelf, fromSlot, toShelf, toSlot, productName);
        return evt;
    }
}

// ====================== НОВОЕ СОБЫТИЕ ДЛЯ УРОВНЯ 2 ======================
public class FailedAttemptEvent : IJournalEntry
{
    public DateTime Time { get; private set; }
    public string OperationType { get; }      // "Положить", "Забрать", "Перенести"
    public string? ShelfId { get; }           // null если не применимо
    public int? SlotNumber { get; }           // null если не применимо
    public string Reason { get; }             // причина ошибки

    // Конструктор
    public FailedAttemptEvent(string operationType, string? shelfId, int? slotNumber, string reason)
    {
        Time = DateTime.Now;
        OperationType = operationType;
        ShelfId = shelfId;
        SlotNumber = slotNumber;
        Reason = reason;
    }

    // Метод для записи в файл
    public string ToLogLine()
    {
        return $"{Time:yyyy-MM-dd HH:mm:ss}|FAILED|{OperationType}|{ShelfId ?? "-"}|{SlotNumber?.ToString() ?? "-"}|{Reason}";
    }

    // Метод для вывода в консоль
    public string ToScreenLine()
    {
        string location = "";
        if (ShelfId != null && SlotNumber.HasValue)
            location = $" на полке {ShelfId}, слот {SlotNumber}";
        else if (ShelfId != null)
            location = $" на полке {ShelfId}";

        return $"[{Time:HH:mm:ss}] ОШИБКА при {OperationType}{location}: {Reason}";
    }

    // Статический метод для превращения строки из файла в объект
    public static FailedAttemptEvent FromLogLine(string line)
    {
        var parts = line.Split('|');
        DateTime time = DateTime.Parse(parts[0]);
        string operationType = parts[2];
        string? shelfId = parts[3] == "-" ? null : parts[3];
        int? slotNumber = parts[4] == "-" ? null : int.Parse(parts[4]);
        string reason = parts[5];

        var evt = new FailedAttemptEvent(operationType, shelfId, slotNumber, reason);
        evt.Time = time;
        return evt;
    }
}

// ---------------------------------------часть 2 женерик журнал----------------------------------------
public class Journal<T> where T : IJournalEntry
{
    private List<T> _entries = new List<T>();
    private readonly string _filePath;

    public Journal(string filePath)
    {
        _filePath = filePath;
    }

    public void Add(T entry)
    {
        _entries.Add(entry);
    }

    public IReadOnlyList<T> GetAllEntries()
    {
        return _entries.AsReadOnly();
    }

    public void SaveToFile()
    {
        File.WriteAllLines(_filePath, _entries.Select(e => e.ToLogLine()));
    }

    // Загрузка журналов при старте 
    public void LoadFromFile(Func<string, T> parser)
    {
        if (!File.Exists(_filePath)) return;

        var lines = File.ReadAllLines(_filePath);
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                var entry = parser(line);
                _entries.Add(entry);
            }
        }
    }

    // новый метод 
}

// -------------------------------часть 3 создаю класс Shelf----------------------------------
public class Shelf // полки 
{
    private readonly string _id; // А или В
    private readonly int _size; // колво слотов 
    private readonly string?[] _slots;  // массив слотов 

    public Shelf(string id, int size)
    {
        _id = id;
        _size = size;
        _slots = new string?[size]; // создаем массив нужного размера 
    }

    public string Id => _id;    // свойство для чтения

    // положить товар 
    public bool Place(int slotNumber, string productName)
    {
        int index = slotNumber - 1;
        if (index < 0 || index >= _size) return false;  // неверный слот
        if (_slots[index] != null) return false;    // слот занят 

        _slots[index] = productName;
        return true;
    }

    // забрать товар из слота 
    public bool Take(int slotNumber, out string? productName)
    {
        productName = null;
        int index = slotNumber - 1;
        if (index < 0 || index >= _size) return false;
        if (_slots[index] == null) return false;

        productName = _slots[index];
        _slots[index] = null;
        return true;
    }

    // посмотреть не забирая 
    public string? Peek(int slotNumber)
    {
        int index = slotNumber - 1;
        if (index < 0 || index >= _size) return null;
        return _slots[index];
    }

    // показываю состояние полки 
    public void Display()
    {
        Console.WriteLine($"\n=== Полка {_id} ===");
        for (int i = 0; i < _size; i++)
        {
            string content = _slots[i] ?? "[пусто]";
            Console.WriteLine($"Слот {i + 1,2}: {content}");
        }
    }
}

//---------------------------------Основная часть прога---------------------------------
class Program
{
    // константа - количество слотов на каждой полке 
    const int SLOT_COUNT = 5;

    // две полки 
    static Shelf shelfA;
    static Shelf shelfB;

    // Три журнала (каждый для своего типа событий)
    static Journal<PlacedEvent> placedJournal;
    static Journal<TakenEvent> takenJournal;
    static Journal<MovedEvent> movedJournal;

    // Четвёртый журнал для неудачных попыток (Уровень 2)
    static Journal<FailedAttemptEvent> failedJournal;

    // метод 
    static void RestoreShelfState()
    {
        // сначала очистка полок
        for (int i = 0; i < SLOT_COUNT; i++)
        {
            shelfA.Take(i, out _);
            shelfB.Take(i, out _);

        }
        // Проходим по всем размещениям (PlacedEvent)
        var placedEvents = placedJournal.GetAllEntries();
        foreach (var evt in placedEvents)
        {
            Shelf targetShelf = GetShelf(evt.ShelfId);
            if (targetShelf != null)
            {
                targetShelf.Place(evt.SlotNumber, evt.ProductName);
            }
        }

        // Проходим по всем изъятиям (TakenEvent) и удаляем товары 
        var takenEvents = takenJournal.GetAllEntries(); 
        foreach (var evt in takenEvents)
        {
            Shelf targetShelf = GetShelf(evt.ShelfId);
            if (targetShelf != null)
            {
                targetShelf.Take(evt.SlotNumber, out _);
            }
        }

        Console.WriteLine("Состояние полок восстановлено из журналов");
    } 

    static void Main()
    {
        // создание полочек
        shelfA = new Shelf("A", SLOT_COUNT);
        shelfB = new Shelf("B", SLOT_COUNT);

        // Создаём журналы - каждый с именем файла для сохранения 
        placedJournal = new Journal<PlacedEvent>("placed.log");
        takenJournal = new Journal<TakenEvent>("taken.log");
        movedJournal = new Journal<MovedEvent>("moved.log");
        failedJournal = new Journal<FailedAttemptEvent>("failed.log"); // новый журнал

        // загружаем журналы из файлов 
        Console.WriteLine("Зазрузка журналов ");
        placedJournal.LoadFromFile(PlacedEvent.FromLogLine);
        takenJournal.LoadFromFile(TakenEvent.FromLogLine);  
        movedJournal.LoadFromFile(MovedEvent.FromLogLine);  
        failedJournal.LoadFromFile(FailedAttemptEvent.FromLogLine);   

        Console.WriteLine($"Загружено Placed={placedJournal.GetAllEntries().Count}, " +
                      $"Taken={takenJournal.GetAllEntries().Count}, " +
                      $"Moved={movedJournal.GetAllEntries().Count}, " +
                      $"Failed={failedJournal.GetAllEntries().Count}");

        // Восстанавливаем состояние полок из журнала размещений и изъятий 
        RestoreShelfState();

        Console.WriteLine("Ангар запущен");

        ShowMenu();
    }

    static void ShowMenu()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("1 - Положить товар");
            Console.WriteLine("2 - Забрать товар");
            Console.WriteLine("3 - Перенести товар");
            Console.WriteLine("4 - Показать журналы");
            Console.WriteLine("5 - Выход");
            Console.Write("Выберите действие: ");

            string input = Console.ReadLine();
            if (!int.TryParse(input, out int choice))
            {
                Console.WriteLine("Ошибка: введите число!");
                continue;
            }

            switch (choice)
            {
                case 1: PlaceProduct(); break;
                case 2: TakeProduct(); break;
                case 3: MoveProduct(); break;
                case 4: ShowJournals(); break;
                case 5:
                    SaveAndExit();
                    return;
                default:
                    Console.WriteLine("Неверный пункт меню");
                    break;
            }
        }
    }

    // метод получить полку по букве
    static Shelf GetShelf(string shelfId)
    {
        if (shelfId == "A") return shelfA;
        if (shelfId == "B") return shelfB;
        return null; // на случай если ввели не A и не B
    }

    // этот метод показывает текущее состояние обеих полок 
    static void ShowCurrentState()
    {
        Console.WriteLine("\n Текущее состояние склада");
        shelfA.Display();
        shelfB.Display();
    }

    static void PlaceProduct()
    {
        Console.Write("Выберите полку (A или B): ");
        string shelfChoice = Console.ReadLine().ToUpper();
        Shelf targetShelf = GetShelf(shelfChoice);
        if (targetShelf == null)
        {
            Console.WriteLine("Неверная полка!");
            var failEvent = new FailedAttemptEvent("Положить", shelfChoice, null, "неверная полка");
            failedJournal.Add(failEvent);
            return;
        }

        Console.Write("Номер слота (1-5): ");
        if (!int.TryParse(Console.ReadLine(), out int slot) || slot < 1 || slot > SLOT_COUNT)
        {
            Console.WriteLine("Неверный номер слота!");
            var failEvent = new FailedAttemptEvent("Положить", shelfChoice, null, "неверный номер слота");
            failedJournal.Add(failEvent);
            return;
        }

        Console.Write("Название товара: ");
        string product = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(product))
        {
            Console.WriteLine("Название товара не может быть пустым!");
            var failEvent = new FailedAttemptEvent("Положить", shelfChoice, slot, "пустое название товара");
            failedJournal.Add(failEvent);
            return;
        }

        // Проверяем, свободен ли слот
        if (targetShelf.Peek(slot) != null)
        {
            Console.WriteLine($"Слот {slot} на полке {shelfChoice} уже занят!");
            var failEvent = new FailedAttemptEvent("Положить", shelfChoice, slot, "слот занят");
            failedJournal.Add(failEvent);
            return;
        }

        // Кладём товар 
        if (targetShelf.Place(slot, product))
        {
            // создаю событие и добавляю в журнал 
            var placedEvent = new PlacedEvent(shelfChoice, slot, product);
            placedJournal.Add(placedEvent);
            Console.WriteLine($"Товар '{product}' успешно размещён на полке {shelfChoice}, слот {slot}");

            ShowCurrentState();
        }
        else
        {
            Console.WriteLine("Не удалось разместить товар!");
            var failEvent = new FailedAttemptEvent("Положить", shelfChoice, slot, "неизвестная ошибка");
            failedJournal.Add(failEvent);
        }
    }

    // метод забрать товар 
    static void TakeProduct()
    {
        Console.Write("Выберите полку (A или В) ");
        string shelfChoice = Console.ReadLine().ToUpper();
        Shelf targetShelf = GetShelf(shelfChoice);
        if (targetShelf == null)
        {
            Console.WriteLine("Неверная полка!");
            var failEvent = new FailedAttemptEvent("Забрать", shelfChoice, null, "неверная полка");
            failedJournal.Add(failEvent);
            return;
        }

        Console.Write("Номер слота (1-5): ");
        if (!int.TryParse(Console.ReadLine(), out int slot) || slot < 1 || slot > SLOT_COUNT)
        {
            Console.WriteLine("Неверный номер слота!");
            var failEvent = new FailedAttemptEvent("Забрать", shelfChoice, null, "неверный номер слота");
            failedJournal.Add(failEvent);
            return;
        }

        // Проверяем, есть ли товар в слоте
        string? product = targetShelf.Peek(slot);
        if (product == null)
        {
            Console.WriteLine($"Слот {slot} на полке {shelfChoice} пуст!");
            var failEvent = new FailedAttemptEvent("Забрать", shelfChoice, slot, "слот пуст");
            failedJournal.Add(failEvent);
            return;
        }

        // Забираем товар 
        if (targetShelf.Take(slot, out string? takenProduct))
        {
            // Создаём событие и добавляем в журнал 
            var takenEvent = new TakenEvent(shelfChoice, slot, takenProduct);
            takenJournal.Add(takenEvent);
            Console.WriteLine($"Товар '{takenProduct}' забран с полки {shelfChoice}, слот {slot}");
            ShowCurrentState();
        }
        else
        {
            Console.WriteLine("Не удалось забрать товар!");
            var failEvent = new FailedAttemptEvent("Забрать", shelfChoice, slot, "неизвестная ошибка");
            failedJournal.Add(failEvent);
        }
    }

    // Метод: перенести товар с одной полки на другую 
    static void MoveProduct()
    {
        // Спрашиваю откуда
        Console.Write("Откуда (полка A или B): ");
        string fromShelf = Console.ReadLine().ToUpper();
        Shelf sourceShelf = GetShelf(fromShelf);

        if (sourceShelf == null)
        {
            Console.WriteLine("Неверная полка!");
            var failEvent = new FailedAttemptEvent("Перенести", fromShelf, null, "неверная полка источника");
            failedJournal.Add(failEvent);
            return;
        }

        // Спрашиваю номер слота источника 
        Console.Write("Номер слота источника (1 - 5) ");
        if (!int.TryParse(Console.ReadLine(), out int fromSlot) || fromSlot < 1 || fromSlot > SLOT_COUNT)
        {
            Console.WriteLine("Неверный номер слота!");
            var failEvent = new FailedAttemptEvent("Перенести", fromShelf, null, "неверный номер слота источника");
            failedJournal.Add(failEvent);
            return;
        }

        // Проверка есть ли товар в источнике 
        string? product = sourceShelf.Peek(fromSlot);
        if (product == null)
        {
            Console.WriteLine($"Слот {fromSlot} на полке {fromShelf} пуст!");
            var failEvent = new FailedAttemptEvent("Перенести", fromShelf, fromSlot, "слот источника пуст");
            failedJournal.Add(failEvent);
            return;
        }

        // Спрашиваем куда
        Console.Write("Куда (полка A или B):");
        string toShelf = Console.ReadLine().ToUpper();
        Shelf destShelf = GetShelf(toShelf);
        if (destShelf == null)
        {
            Console.WriteLine("Неверная полка!");
            var failEvent = new FailedAttemptEvent("Перенести", toShelf, null, "неверная полка назначения");
            failedJournal.Add(failEvent);
            return;
        }

        // Спрашиваю номер слота назначения 
        Console.Write("Номер слота назначения (1 - 5)");
        if (!int.TryParse(Console.ReadLine(), out int toSlot) || toSlot < 1 || toSlot > SLOT_COUNT)
        {
            Console.WriteLine("Неверный номер слота!");
            var failEvent = new FailedAttemptEvent("Перенести", toShelf, null, "неверный номер слота назначения");
            failedJournal.Add(failEvent);
            return;
        }

        // Проверка свободен ли пункт назначения 
        if (destShelf.Peek(toSlot) != null)
        {
            Console.WriteLine($"Слот {toSlot} на полке {toShelf} уже занят!");
            var failEvent = new FailedAttemptEvent("Перенести", toShelf, toSlot, "слот назначения занят");
            failedJournal.Add(failEvent);
            return;
        }

        // Выполняем перенос 
        if (sourceShelf.Take(fromSlot, out string? movedProduct))
        {
            if (destShelf.Place(toSlot, movedProduct))
            {
                // Создаю событие и добавляю в журнал
                var movedEvent = new MovedEvent(fromShelf, fromSlot, toShelf, toSlot, movedProduct);
                movedJournal.Add(movedEvent);
                Console.WriteLine($"Товар '{movedProduct}' перенесён с {fromShelf}{fromSlot} на {toShelf}{toSlot}");
                ShowCurrentState();
            }
            else
            {
                // Откат - возвращаем товар обратно в источник 
                sourceShelf.Place(fromSlot, movedProduct);
                Console.WriteLine("Не удалось перенести: слот назначения занят или неверен!");
                var failEvent = new FailedAttemptEvent("Перенести", toShelf, toSlot, "ошибка при размещении");
                failedJournal.Add(failEvent);
            }
        }
        else
        {
            Console.WriteLine("Не удалось забрать товар из источника!");
            var failEvent = new FailedAttemptEvent("Перенести", fromShelf, fromSlot, "ошибка при изъятии");
            failedJournal.Add(failEvent);
        }
    }

    // показываем все журналы событий 
    static void ShowJournals()
    {
        Console.WriteLine("\nРазмещения");
        var placedEntries = placedJournal.GetAllEntries();
        if (placedEntries.Count == 0)
            Console.WriteLine("  (пусто)");
        else
            foreach (var entry in placedEntries)
                Console.WriteLine($"  {entry.ToScreenLine()}");

        Console.WriteLine("\nИзъятия");
        var takenEntries = takenJournal.GetAllEntries();
        if (takenEntries.Count == 0)
            Console.WriteLine("  (пусто)");
        else
            foreach (var entry in takenEntries)
                Console.WriteLine($"  {entry.ToScreenLine()}");

        Console.WriteLine("\nПереносы");
        var movedEntries = movedJournal.GetAllEntries();
        if (movedEntries.Count == 0)
            Console.WriteLine("  (пусто)");
        else
            foreach (var entry in movedEntries)
                Console.WriteLine($"  {entry.ToScreenLine()}");

        Console.WriteLine("\nНеуспешные попытки");
        var failedEntries = failedJournal.GetAllEntries();
        if (failedEntries.Count == 0)
            Console.WriteLine("  (пусто)");
        else
            foreach (var entry in failedEntries)
                Console.WriteLine($"  {entry.ToScreenLine()}");
    }

    // Метод который сохраняет журналы и делает выход 
    static void SaveAndExit()
    {
        Console.WriteLine("\n Сохранение журналов");
        placedJournal.SaveToFile();
        takenJournal.SaveToFile();
        movedJournal.SaveToFile();
        failedJournal.SaveToFile();
        Console.WriteLine($" Журнал размещений сохранён в placed.log ({placedJournal.GetAllEntries().Count} записей)");
        Console.WriteLine($" Журнал изъятий сохранён в taken.log ({takenJournal.GetAllEntries().Count} записей)");
        Console.WriteLine($" Журнал перемещений сохранён в moved.log ({movedJournal.GetAllEntries().Count} записей)");
        Console.WriteLine($" Журнал ошибок сохранён в failed.log ({failedJournal.GetAllEntries().Count} записей)");
        Console.WriteLine("До свидания!");
    }
}