using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.IO;

// интерфейс
public interface IJournalEntry
{
    string ToLogLine();
    string ToScreenLine();
}

public class Shelf
{
    private string?[] _slots;
    public string Name { get; }

    public Shelf(string name, int size)
    {
        Name = name;
        _slots = new string?[size];
    }

    // положить товар в слот от 1 до S 
    public bool Place(int slotNumber, string product)
    {
        int index = slotNumber - 1;

        if (_slots[index] != null) return false;

        _slots[index] = product;
        return true;
    }

    // забрать товар 
    public string? Take(int slotNumber)
    {
        int index = slotNumber - 1;

        if (_slots[index] == null) return null;

        string product = _slots[index];
        _slots[index] = null;
        return product;
    }

    // посмотреть что в слоте не забирая 
    public string? Peek(int slotNumber)
    {
        return _slots[slotNumber - 1];
    }

    // показать полку на экране 
    public void Display()
    {
        Console.WriteLine($"Полка {Name}: ");
        for (int i = 0; i < _slots.Length; i++)
        {
            string content = _slots[i] ?? "пусто";
            Console.WriteLine($"[{i + 1}] {content} ");
        }
        Console.WriteLine();
    }
}

// событие  
public class PlacedEvent : IJournalEntry
{
    // свойтсва
    public string Shelf { get; }
    public int Slot { get; }
    public string ProductName { get; }

    // конструктор 
    public PlacedEvent(string shelf, int slot, string productName)
    {
        Shelf = shelf;
        Slot = slot;
        ProductName = productName;
    }

    public string ToLogLine()
    {
        return $"{Shelf}|{Slot}|{ProductName}";
    }

    public string ToScreenLine()
    {
        return $"Размещeние | полка {Shelf} | слот {Slot} | товар «{ProductName}» ";
    }
}


public class TakenEvent : IJournalEntry
{
    public string Shelf { get; }
    public int Slot { get; }
    public string ProductName { get; }

    public TakenEvent(string shelf, int slot, string productName)
    {
        Shelf = shelf;
        Slot = slot;
        ProductName = productName;
    }

    public string ToLogLine()
    {
        return $"{Shelf}|{Slot}|{ProductName}";
    }

    public string ToScreenLine()
    {
        return $"Изъятие | полка {Shelf} | слот {Slot} | товар «{ProductName}»";
    }
}

public class MovedEvent : IJournalEntry
{
    public string FromShelf { get; }
    public int FromSlot { get; }
    public string ToShelf { get; }
    public int ToSlot { get; }
    public string ProductName { get; }

    public MovedEvent(string fromShelf, int fromSlot, string toShelf, int toSlot, string productName)
    {
        FromShelf = fromShelf;
        FromSlot = fromSlot;
        ToShelf = toShelf;
        ToSlot = toSlot;
        ProductName = productName;
    }

    public string ToLogLine()
    {
        return $"{FromShelf}|{FromSlot}|{ToShelf}|{ToSlot}|{ProductName}";
    }

    public string ToScreenLine()
    {
        return $"Перенос | с {FromShelf}:{FromSlot} на {ToShelf}:{ToSlot} | товар «{ProductName}»";
    }
}


// создание журнала Journal<T>
public class Journal<T> where T : IJournalEntry
{
    private List<T> _entries = new List<T>();

    public void Add(T entry)
    {
        _entries.Add(entry);
    }

    public List<T> GetAll()
    {
        return _entries;
    }

    public void SaveToFile(string path)
    {
        using (StreamWriter writer = new StreamWriter(path))
        {
            foreach (T entry in _entries)
            {
                writer.WriteLine(entry.ToLogLine());
            }
        }
    }
}

public class FailedAttemptEvent : IJournalEntry
{
    public string OperationType { get; }
    public string? Shelf { get; }
    public int? Slot { get; }
    public string Reason { get; }

    public FailedAttemptEvent(string operationType, string? shelf, int? slot, string reason)
    {
        OperationType = operationType;
        Shelf = shelf;
        Slot = slot;
        Reason = reason;

    }

    public string ToLogLine()
    {
        return $"{OperationType}|{Shelf ?? ""}|{Slot?.ToString() ?? ""}|{Reason}";
    }

    public string ToScreenLine()
    {
        return $"Неудача | {OperationType} | полка {Shelf ?? "?"} слот {Slot?.ToString() ?? "?"} | причина: {Reason}";
    }
}

// класс программ
class Program
{
    const int S = 5;

    static void Main()
    {
        // журналы 
        Journal<PlacedEvent> placedJournal = new Journal<PlacedEvent>();
        Journal<TakenEvent> takenJournal = new Journal<TakenEvent>();
        Journal<MovedEvent> movedJournal = new Journal<MovedEvent>();

        Journal<FailedAttemptEvent> failedJournal = new Journal<FailedAttemptEvent>();

        if (File.Exists("placed.log"))
        {
            string[] lines = File.ReadAllLines("placed.log");
            foreach (string line in lines)
            {
                string[] parts = line.Split('|');
                if (parts.Length == 3)
                {
                    PlacedEvent e = new PlacedEvent(parts[0], int.Parse(parts[1]), parts[2]);
                    placedJournal.Add(e);
                }
            }
        }

        if (File.Exists("taken.log"))
        {
            string[] lines = File.ReadAllLines("taken.log");
            foreach (string line in lines)
            {
                string[] parts = line.Split('|');
                if (parts.Length == 3)
                {
                    TakenEvent e = new TakenEvent(parts[0], int.Parse(parts[1]), parts[2]);
                    takenJournal.Add(e);
                }
            }
        }
        
        if (File.Exists("moved.log"))
        {
            string[] lines = File.ReadAllLines("moved.log");
            foreach (string line in lines)
            {
                string[] parts = line.Split('|');
                if (parts.Length == 5)
                {
                    MovedEvent e = new MovedEvent(parts[0], int.Parse(parts[1]), parts[2], int.Parse(parts[3]), parts[4]);
                    movedJournal.Add(e);
                }
            }
        }

        if (File.Exists("failed.log"))
        {
            string[] lines = File.ReadAllLines("failed.log");
            foreach (string line in lines)
            {
                string[] parts = line.Split('|');
                if (parts.Length == 4)
                {
                    FailedAttemptEvent e = new FailedAttemptEvent(parts[0], parts[1] == "" ? null : parts[1],
                        parts[2] == "" ? null : int.Parse(parts[2]), parts[3]);
                    failedJournal.Add(e);
                }
            }
        }

        // создаем две полки 
        Shelf shelfA = new Shelf("A", S);
        Shelf shelfB = new Shelf("B", S);

        bool exit = false;

        while (!exit) // бесконечный цикл пока экзит не станет тру
        {
            Console.Clear();
            Console.WriteLine("   СКЛАД   \n");

            // показываем полки 
            shelfA.Display();
            shelfB.Display();

            Console.WriteLine("\n Меню:");
            Console.WriteLine("1 - Положить товар");
            Console.WriteLine("2 - Забрать товар");
            Console.WriteLine("3 - Перенести товар");
            Console.WriteLine("4 - Показать журналы");
            Console.WriteLine("5 - Выход");
            Console.Write("\nВаш выбор  ");

            string? input = Console.ReadLine();

            // обрабокта выбора            
            if (input == "5")
            {
                // Сохраняем журналы в файлы
                placedJournal.SaveToFile("placed.log");
                takenJournal.SaveToFile("taken.log");
                movedJournal.SaveToFile("moved.log");
                failedJournal.SaveToFile("failed.log");

                exit = true;
                Console.WriteLine("Журналы сохранены в файлы. Выход из программы...");
            }
            else if (input == "1")
            {
                // Положить товар 
                Console.WriteLine("Полка (А или В): ");
                string? shelfName = Console.ReadLine()?.ToUpper();

                Console.WriteLine($"Номер слота (1-{S}): ");
                string? slotInput = Console.ReadLine();
                bool slotOk = int.TryParse(slotInput, out int slot);

                Console.Write("Название товара: ");
                string? product = Console.ReadLine();

                // Проверка корректности ввода 
                if (!slotOk || slot < 1 || slot > S)
                {
                    Console.WriteLine("Ошибка: неверный номер слота!");
                    FailedAttemptEvent failed = new FailedAttemptEvent("Положить", shelfName, 0, "неверный номер слота");
                    failedJournal.Add(failed);
                    Console.ReadKey();
                }
                else if (string.IsNullOrWhiteSpace(product))
                {
                    Console.WriteLine("Ошибка: название товара не может быть пустым!");
                    FailedAttemptEvent failed = new FailedAttemptEvent("Положить", shelfName, slot, "пустое название товара");
                    failedJournal.Add(failed);
                    Console.ReadKey();
                }
                else if (shelfName != "A" && shelfName != "B")
                {
                    Console.WriteLine("Ошибка: полка должна быть A или B!");
                    FailedAttemptEvent failed = new FailedAttemptEvent("Положить", shelfName, slot, "неверная полка");
                    failedJournal.Add(failed);
                    Console.ReadKey();
                }
                else
                {
                    // выбираем нужную полку 
                    Shelf selectedShelf = (shelfName == "A") ? shelfA : shelfB;

                    // пробуем положить
                    bool succes = selectedShelf.Place(slot, product);

                    if (succes)
                    {
                        // создаем событие кладем в журнал 
                        PlacedEvent placedEvent = new PlacedEvent(shelfName, slot, product);
                        placedJournal.Add(placedEvent);
                        Console.WriteLine($"Товар {product} успешно положен на полку {shelfName} в слот {slot}");
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка: слот {slot} на полке {shelfName} уже занят!");
                        FailedAttemptEvent failed = new FailedAttemptEvent("Положить", shelfName, slot, "слот занят");
                        failedJournal.Add(failed);
                    }
                    Console.ReadKey();
                }
            }
            else if (input == "2")
            {
                // Забрать товар 
                Console.WriteLine("Полка (А или В): ");
                string? shelfName = Console.ReadLine()?.ToUpper();

                Console.Write($"Номер слота (1-{S}): ");
                string? slotInput = Console.ReadLine();
                bool slotOk = int.TryParse(slotInput, out int slot);

                // проверка корректности ввода 
                if (!slotOk || slot < 1 || slot > S)
                {
                    Console.WriteLine("Ошибка: неверный номер слота!");
                    FailedAttemptEvent failed = new FailedAttemptEvent("Забрать", shelfName, 0, "неверный номер слота");
                    failedJournal.Add(failed);
                    Console.ReadKey();
                }
                else if (shelfName != "A" && shelfName != "B")
                {
                    Console.WriteLine("Ошибка: полка должна быть А или В!");
                    FailedAttemptEvent failed = new FailedAttemptEvent("Забрать", shelfName, slot, "неверная полка");
                    failedJournal.Add(failed);
                    Console.ReadKey();
                }
                else
                {
                    // Выбираем нужную полку 
                    Shelf selectedShelf = (shelfName == "A") ? shelfA : shelfB;

                    // Смотрим, что лежит в слоте (не забирая)
                    string? product = selectedShelf.Peek(slot);

                    if (product == null)
                    {
                        Console.WriteLine($"Ошибка: слот {slot} на полке {shelfName} пуст! Нечего забирать.");
                        FailedAttemptEvent failed = new FailedAttemptEvent("Забрать", shelfName, slot, "слот пуст");
                        failedJournal.Add(failed);
                        Console.ReadKey();
                    }
                    else
                    {
                        string takenProduct = selectedShelf.Take(slot);

                        // создаю событие 
                        TakenEvent takenEvent = new TakenEvent(shelfName, slot, takenProduct);
                        // документирую это событие 
                        takenJournal.Add(takenEvent);

                        Console.WriteLine($"Товар {takenProduct} забран с полки {shelfName} из слота {slot}");
                        Console.ReadKey();
                    }
                }
            }
            else if (input == "3")
            {
                // Ввод источника 
                Console.Write("Полка-источник (A или B): ");
                string? fromShelfName = Console.ReadLine()?.ToUpper();

                Console.Write($"Слот-источник (1-{S}): ");
                string? fromSlotInput = Console.ReadLine();
                bool fromSlotOk = int.TryParse(fromSlotInput, out int fromSlot);


                // Ввод назначения
                Console.Write("Полка-назначение (A или B): ");
                string? toShelfName = Console.ReadLine()?.ToUpper();

                Console.Write($"Слот-назначение (1-{S}): ");
                string? toSlotInput = Console.ReadLine();
                bool toSlotOk = int.TryParse(toSlotInput, out int toSlot);

                // проверка корректности 
                if (!fromSlotOk || !toSlotOk || fromSlot < 1 || fromSlot > S || toSlot < 1 || toSlot > S)
                {
                    Console.WriteLine("Ошибка: неверный номер слота!");
                    FailedAttemptEvent failed = new FailedAttemptEvent("Перенести", fromShelfName, fromSlot, "неверный номер слота");
                    failedJournal.Add(failed);
                    Console.ReadKey();
                }
                else if ((fromShelfName != "A" && fromShelfName != "B") || (toShelfName != "A" && toShelfName != "B"))
                {
                    Console.WriteLine("Ошибка: полка должна быть A или B!");
                    FailedAttemptEvent failed = new FailedAttemptEvent("Перенести", fromShelfName, fromSlot, "неверная полка");
                    failedJournal.Add(failed);
                    Console.ReadKey();
                }
                else
                {
                    Shelf fromShelf = (fromShelfName == "A") ? shelfA : shelfB;
                    Shelf toShelf = (toShelfName == "A") ? shelfA : shelfB;

                    // Смотрим, что лежит в источнике 
                    string? product = fromShelf.Peek(fromSlot);

                    if (product == null)
                    {
                        Console.WriteLine($"Ошибка: слот {fromSlot} на полке {fromShelfName} пуст! Нечего переносить.");
                        FailedAttemptEvent failed = new FailedAttemptEvent("Перенести", fromShelfName, fromSlot, "слот источника пуст");
                        failedJournal.Add(failed);
                        Console.ReadKey();
                    }
                    else
                    {
                        // Проверка свободен ли слот назначения?
                        if (toShelf.Peek(toSlot) != null)
                        {
                            Console.WriteLine($"Ошибка: слот {toSlot} на полке {toShelfName} уже занят!");
                            FailedAttemptEvent failed = new FailedAttemptEvent("Перенести", toShelfName, toSlot, "слот назначения занят");
                            failedJournal.Add(failed);
                            Console.ReadKey();
                        }
                        else
                        {
                            // все ок, выполяню перенос
                            // забираем товар из источника 
                            string movedProduct = fromShelf.Take(fromSlot);
                            // кладем в назначение 
                            toShelf.Place(toSlot, movedProduct);

                            // создаю событие 
                            MovedEvent movedEvent = new MovedEvent(fromShelfName, fromSlot, toShelfName, toSlot, movedProduct);

                            // добавляю в журнал 
                            movedJournal.Add(movedEvent);
                            Console.WriteLine($"Товар «{movedProduct}» перенесён с {fromShelfName}:{fromSlot} на {toShelfName}:{toSlot}");
                            Console.ReadKey();
                        }
                    }
                }
            }
            else if (input == "4")
            {
                Console.Clear();
                Console.WriteLine("=== ЖУРНАЛЫ СОБЫТИЙ ===\n");

                Console.WriteLine("--- Размещения ---");
                foreach (var e in placedJournal.GetAll())
                {
                    Console.WriteLine(e.ToScreenLine());
                }

                Console.WriteLine("\n--- Изъятия ---");
                foreach (var e in takenJournal.GetAll())
                {
                    Console.WriteLine(e.ToScreenLine());
                }

                Console.WriteLine("\n--- Переносы ---");
                foreach (var e in movedJournal.GetAll())
                {
                    Console.WriteLine(e.ToScreenLine());
                }

                Console.WriteLine("\n--- Неудачные попытки ---");
                foreach (var e in failedJournal.GetAll())
                {
                    Console.WriteLine(e.ToScreenLine());
                }

                Console.WriteLine("\nНажмите любую клавишу...");
                Console.ReadKey();
            }
        }
    }
}