// часть 1 Интерфейс и события 

using System.Timers;

public interface IJournalEntry
{
    string ToLogLine();
    string ToScreenLine();
}

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