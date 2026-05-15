// часть 1 Интерфейс и события 
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

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

    static void Main()
    {
        // создание полочек
        shelfA = new Shelf("A", SLOT_COUNT);
        shelfB = new Shelf("B", SLOT_COUNT);

        // Создаём журналы - каждый с именем файла для сохранения 
        placedJournal = new Journal<PlacedEvent>("placed.log");
        takenJournal = new Journal<TakenEvent>("taken.log");
        movedJournal = new Journal<MovedEvent>("moved.log");

        Console.WriteLine("Ангар запущен");

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
                    return;
                }

                Console.Write("Номер слота (1-5): ");
                if (!int.TryParse(Console.ReadLine(), out int slot) || slot < 1 || slot > SLOT_COUNT)
                {
                    Console.WriteLine("Неверный номер слота!");
                    return;
                }

                Console.Write("Название товара: ");
                string product = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(product))
                {
                    Console.WriteLine("Название товара не может быть пустым!");
                    return;
                }

                // Проверяем, свободен ли слот
                if (targetShelf.Peek(slot) != null)
                {
                    Console.WriteLine($"Слот {slot} на полке {shelfChoice} уже занят!");
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
                    return;
                }

                Console.Write("Номер слота (1-5): ");
                if (!int.TryParse(Console.ReadLine(), out int slot) || slot < 1 || slot > SLOT_COUNT)
                {
                    Console.WriteLine("❌ Неверный номер слота!");
                    return;
                }

                // Проверяем, есть ли товар в слоте
                string? product = targetShelf.Peek(slot);
                if (product == null)
                {
                    Console.WriteLine($"Слот {slot} на полке {shelfChoice} пуст!");
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
                }
            }
        }
    }
}