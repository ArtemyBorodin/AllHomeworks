using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using static System.Net.Mime.MediaTypeNames;

// контекст урона 
public class DamageContext
{
    public int Round { get; }
    public int EnemyHpBeforeHit { get; }
    public int PlayerHp { get; }
    public int Strength { get; }

    public DamageContext(int round, int enemyHpBeforeHit, int playerHp, int strength)
    {
        Round = round;
        EnemyHpBeforeHit = enemyHpBeforeHit;
        PlayerHp = playerHp;
        Strength = strength;
    }
// конец класса DamageContext
}

// Делегат для модификаторов
public delegate void DamageModifier(ref int damage, DamageContext context);

//класс пайплайна
public class PlayerDamagePipeline
{
    public event DamageModifier? Modifiers;

    public void Apply(ref int damage, DamageContext context)
    {
        if (Modifiers != null)
        {
            //вызываем всех подписанных модификаторов
            Delegate[] delegates = Modifiers.GetInvocationList();
            foreach (DamageModifier modifier in delegates)
            {
                modifier(ref damage, context);
                if (damage < 1) damage = 1; // минимум 1
            }
        }
    }

    //статические методы- модификаторы 
    public static void ApplyBonusIfEnemyLow(ref int damage, DamageContext context)
    {
        if (context.EnemyHpBeforeHit < 30)
        {
            damage += 5;
        }
    }

    public static void ApplyBonusIfenemyLow(ref int damage, DamageContext context)
    {
        if (context.EnemyHpBeforeHit < 30)
        {
            damage += 5;
        }
    }

    public static void ApplyPenaltyIfPlayerLow(ref int damage, DamageContext context)
    {
        if (context.PlayerHp < 40)
        {
            damage -= 3;
            if (damage < 1) damage = 1;
        }
    }
}

// Интерфейсы стратегий
public interface IPlayerAttackStrategy
{
    int GetPlayerDamage(int roundIndex, int playerHp, int enemyHp);
}

public interface IEnemyAttackStrategy
{
    int GetEnemyDamage(int roundIndex, int enemyHp, int playerHp);
}

//----------------------стратегия игрока ------------------------------

public class LightPlayerAttackStrategy : IPlayerAttackStrategy
{
    private const int DAMAGE = 10;

    public int GetPlayerDamage(int roundIndex, int playerHp, int enemyHp)
    {
        return DAMAGE;
    }
}

public class HeavyPlayerAttackStrategy : IPlayerAttackStrategy
{
    private readonly int _baseDamage;
    private readonly int _strength;
    private readonly int _step;

    public HeavyPlayerAttackStrategy(int strength, int step)
    {
        _baseDamage = 12;
        _strength = strength;   
        _step = step;   
    }

    public int GetPlayerDamage(int roundIndex, int playerHp, int enemyHp)
    {
        return _baseDamage + _strength  * _step;
    }
}

// ---------------------стратегия противника --------------------------

public class AggressiveEnemyStrategy : IEnemyAttackStrategy
{
    private const int DAMAGE = 15;

    public int GetEnemyDamage(int roundIndex, int enemyHp, int playerHp)
    {
        return DAMAGE;
    }
}

public class CarefulEnemyStrategy : IEnemyAttackStrategy
{
    private const int DAMAGE = 8;

    public int GetEnemyDamage(int roundIndex, int enemyHp, int playerHp)
    {
        return DAMAGE;
    }
}

public class RandomEnemyStrategy : IEnemyAttackStrategy
{
    private static readonly Random _random = new Random(12345);
    private readonly int[] _damages = { 10, 12, 14 };

    public int GetEnemyDamage(int roundIndex, int enemyHp, int playerHp)
    {
        int index = _random.Next(0, 3);
        return _damages[index];
    }
}

class Program
{
    // константы 
    const int MAX_HP = 100;
    const int START_HP = 100;
    const int STRENGTH = 5;
    const int HEAVY_BASE = 12;
    const int HEAVY_STEP = 2;
    const int REST_HEAL = 5;

    static void Main()
    {
        //начальные значения 
        int playerHp = START_HP;
        int enemyHp = START_HP;
        int round = 1;

        // стратегии игрока 
        IPlayerAttackStrategy lightStrategy = new LightPlayerAttackStrategy();
        IPlayerAttackStrategy heavyStrategy = new HeavyPlayerAttackStrategy(STRENGTH, HEAVY_STEP);

        // стратегия противника 
        IEnemyAttackStrategy enemyStrategy = new AggressiveEnemyStrategy();
        string enemyStrategyName = "Aggressive";

        //Флаги для смены стратегии 
        bool hasSwitchedToCareful = false;
        bool hasSwitchedToRandom = false;

        // Создаём пайплайн и подписываем модификаторы
        PlayerDamagePipeline damagePipeline = new PlayerDamagePipeline();
        damagePipeline.Modifiers += PlayerDamagePipeline.ApplyBonusIfEnemyLow;
        damagePipeline.Modifiers += PlayerDamagePipeline.ApplyPenaltyIfPlayerLow;

        while (true)
        {
            // Отображение состояния и меню
            Console.WriteLine($"--- Раунд {round} | Игрок {playerHp} HP | Сила {STRENGTH} | Противник {enemyHp} HP | Стратегия: {enemyStrategyName} ---");
            Console.WriteLine("1 - Лёгкий удар");
            Console.WriteLine("2 - Тяжёлый удар");
            Console.WriteLine("3 - Отдых");
            Console.WriteLine("4 - Показать состояние");
            Console.WriteLine("0 - Выход");
            Console.Write("Ваш выбор: ");

            string input = Console.ReadLine();
            if (!int.TryParse(input, out int choice))
            {
                Console.WriteLine("Неверный ввод");
                continue;
            }

            // Выход
            if (choice == 0)
            {
                Console.WriteLine("Выход из игры");
                continue;
            }

            // показать состояние раунд не меняется 
            if (choice == 4)
            {
                Console.WriteLine($"Игрок: {playerHp} HP | Противник: {enemyHp} " +
                    $"HP | Сила: {STRENGTH} | Раунд: {round} | Стратегия: {enemyStrategyName}");
                continue;
            }

            // ход игрока 
            int playerDamage = 0;
            if (choice == 1) // лёгкий удар 
            {
                // Получаем базовый урон от стратегии
                int baseDamage = lightStrategy.GetPlayerDamage(round, playerHp, enemyHp);

                // Сохраняем HP противника ДО удара для контекста
                int enemyHpBeforeHit = enemyHp;

                // Создаём контекст
                DamageContext context = new DamageContext(round, enemyHpBeforeHit, playerHp, STRENGTH);

                // Применяем модификаторы (урон может измениться)
                int finalDamage = baseDamage;
                damagePipeline.Apply(ref finalDamage, context);

                // Применяем урон
                enemyHp -= finalDamage;

                // Выводим информацию
                if (choice == 1)
                {
                    if (finalDamage != baseDamage)
                        Console.WriteLine($"Игрок наносит лёгкий удар (база {baseDamage} + модификаторы = {finalDamage}).");
                    else
                        Console.WriteLine($"Игрок наносит лёгкий удар.");
                }
                else if (choice == 2)
                {
                    if (finalDamage != baseDamage)
                        Console.WriteLine($"Игрок наносит тяжёлый удар (база {HEAVY_BASE} + Сила×{HEAVY_STEP} = {baseDamage}, + модификаторы = {finalDamage}).");
                    else
                        Console.WriteLine($"Игрок наносит тяжёлый удар (база {HEAVY_BASE} + Сила×{HEAVY_STEP} = {baseDamage}).");
                }
            }
            else if (choice == 2)
            {
                // Получаем базовый урон от стратегии
                int baseDamage = lightStrategy.GetPlayerDamage(round, playerHp, enemyHp);

                // Сохраняем HP противника ДО удара для контекста
                int enemyHpBeforeHit = enemyHp;

                // Создаём контекст
                DamageContext context = new DamageContext(round, enemyHpBeforeHit, playerHp, STRENGTH);

                // Применяем модификаторы (урон может измениться)
                int finalDamage = baseDamage;
                damagePipeline.Apply(ref finalDamage, context);

                // Применяем урон
                enemyHp -= finalDamage;

                // Выводим информацию
                if (choice == 1)
                {
                    if (finalDamage != baseDamage)
                        Console.WriteLine($"Игрок наносит лёгкий удар (база {baseDamage} + модификаторы = {finalDamage}).");
                    else
                        Console.WriteLine($"Игрок наносит лёгкий удар.");
                }
                else if (choice == 2)
                {
                    if (finalDamage != baseDamage)
                        Console.WriteLine($"Игрок наносит тяжёлый удар (база {HEAVY_BASE} + Сила×{HEAVY_STEP} = {baseDamage}, + модификаторы = {finalDamage}).");
                    else
                        Console.WriteLine($"Игрок наносит тяжёлый удар (база {HEAVY_BASE} + Сила×{HEAVY_STEP} = {baseDamage}).");
                }
            }
            else if (choice == 3) // отдых
            {
                playerHp += REST_HEAL;
                if (playerHp > MAX_HP) playerHp = MAX_HP;
                Console.WriteLine($"Игрок отдыхает и восстанавливает {REST_HEAL} HP.");
            }

            if (enemyHp < 0) enemyHp = 0;

            // Проверка победы игрока 
            if (enemyHp <= 0)
            {
                Console.WriteLine($"ПОБЕДА ИГРОКА!");
                break;
            }

            // -----------------------------------смена стратегии противника-------------------------------------
            if (!hasSwitchedToRandom && enemyHp < 25)
            {
                enemyStrategy = new RandomEnemyStrategy();
                enemyStrategyName = "Random";
                hasSwitchedToRandom = true;
                hasSwitchedToCareful = true;
            }
            else if (!hasSwitchedToCareful && enemyHp < 50)
            {
                enemyStrategy = new CarefulEnemyStrategy();
                enemyStrategyName = "Careful";
                hasSwitchedToCareful = true;
            }

            // ----------------------------ход противника------------------------------
            int enemyDamage = enemyStrategy.GetEnemyDamage(round, enemyHp, playerHp);
            playerHp -= enemyDamage;
            if (playerHp < 0) playerHp = 0;
            Console.WriteLine($"Противник наносит {enemyDamage} урона.");

            // Проверка победы противника 
            if (playerHp <= 0)
            {
                Console.WriteLine($"ПОБЕДА ПРОТИВНИКА!");
                break;
            }

            // Следующий раунд 
            round++;

        // конец цикла 
        }
    // конец Main 
    }

// end class program 
}