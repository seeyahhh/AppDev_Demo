using System;
using System.Collections.Generic;


bool runAgain = true;
while (runAgain)
{
    Receipt receipt = BuildReceipt();
    receipt.PrintReceipt();
    runAgain = AskRunAgain();
}

Console.WriteLine("\nThank you for using Receipt Maker!");
Console.ReadKey();

static Receipt BuildReceipt()
{
    PrintHeader();

    Receipt receipt = new Receipt();
    receipt.StoreName = PromptString("Enter Store Name   ");
    receipt.CashierName = PromptString("Enter Cashier Name ");
    receipt.TransactionDate = DateTime.Now;

    CollectItems(receipt);
    receipt.DiscountPercent = PromptDiscount();
    receipt.PaymentMethod = PromptPaymentMethod();

    if (receipt.PaymentMethod == "Cash")
        receipt.AmountTendered = PromptAmountTendered(receipt.GetTotal());

    return receipt;
}

static void PrintHeader()
{
    Console.Clear();
    Console.WriteLine("==========================================");
    Console.WriteLine("               RECEIPT MAKER              ");
    Console.WriteLine("==========================================");
}

static string PromptString(string label)
{
    Console.Write($"\n{label}: ");
    return Console.ReadLine();
}

static void CollectItems(Receipt receipt)
{
    Console.WriteLine("\n------------------------------------------");
    Console.WriteLine("  ADD ITEMS  (type 'done' to finish)");
    Console.WriteLine("------------------------------------------\n");

    while (true)
    {
        Console.Write("Item Name   : ");
        string itemName = Console.ReadLine();

        if (itemName.Trim().ToLower() == "done") break;

        if (string.IsNullOrWhiteSpace(itemName))
        {
            Console.WriteLine("[!] Item name cannot be empty.\n");
            continue;
        }

        int qty = PromptInt("Quantity    ", "[!] Enter a valid quantity (e.g. 1, 2, 3).");
        double price = PromptDouble("Unit Price  ", "[!] Enter a valid price (e.g. 99.50).");

        receipt.AddItem(new Item(itemName, qty, price));
        Console.WriteLine($"  [+] '{itemName}' added!\n");
    }

    if (receipt.IsEmpty())
    {
        Console.WriteLine("\n[!] No items added. Exiting...");
        Console.ReadKey();
        Environment.Exit(0);
    }
}

static int PromptInt(string label, string errorMsg)
{
    while (true)
    {
        Console.Write($"{label}: ");
        if (int.TryParse(Console.ReadLine(), out int value) && value > 0)
            return value;
        Console.WriteLine(errorMsg);
    }
}

static double PromptDouble(string label, string errorMsg)
{
    while (true)
    {
        Console.Write($"{label}: ");
        if (double.TryParse(Console.ReadLine(), out double value) && value >= 0)
            return value;
        Console.WriteLine(errorMsg);
    }
}

static double PromptDiscount()
{
    Console.Write("\nDiscount (% -- enter 0 if none): ");
    double.TryParse(Console.ReadLine(), out double discount);
    return Math.Clamp(discount, 0, 100);
}

static string PromptPaymentMethod()
{
    Console.WriteLine("\nPayment Method:");
    Console.WriteLine("  [1] Cash");
    Console.WriteLine("  [2] Credit Card");
    Console.WriteLine("  [3] GCash");
    Console.Write("Choose (1-3): ");
    string choice = Console.ReadLine();

    return choice switch
    {
        "2" => "Credit Card",
        "3" => "GCash",
        _ => "Cash"
    };
}

static double PromptAmountTendered(double total)
{
    while (true)
    {
        Console.Write("Amount Tendered (PHP): ");
        if (double.TryParse(Console.ReadLine(), out double tendered) && tendered >= total)
            return tendered;
        Console.WriteLine($"[!] Must be at least PHP {total:F2}");
    }
}

static bool AskRunAgain()
{
    Console.WriteLine("\n------------------------------------------");
    Console.WriteLine("  [1] Make another transaction");
    Console.WriteLine("  [2] Exit");
    Console.Write("Choose (1-2): ");
    return Console.ReadLine().Trim() == "1";
}

class Item
{
    public string Name { get; set; }
    public int Quantity { get; set; }
    public double UnitPrice { get; set; }
    public double GetSubtotal() => Quantity * UnitPrice; 

    public Item(string name, int quantity, double unitPrice)
    {
        Name = name;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}

class Receipt
{
    public string StoreName { get; set; }
    public string CashierName { get; set; }
    public DateTime TransactionDate { get; set; }
    public string PaymentMethod { get; set; }
    public double DiscountPercent { get; set; }
    public double AmountTendered { get; set; }

    private List<Item> _items = new List<Item>();

    // Item management
    public void AddItem(Item item) => _items.Add(item);
    public bool IsEmpty() => _items.Count == 0;
    public IReadOnlyList<Item> GetItems() => _items;

    // Calculations
    public double GetSubtotal()
    {
        double total = 0;
        foreach (var item in _items)
            total += item.GetSubtotal();
        return total;
    }

    public double GetDiscountAmount() => GetSubtotal() * (DiscountPercent / 100);
    public double GetVAT() => (GetSubtotal() - GetDiscountAmount()) * 0.12;
    public double GetTotal() => GetSubtotal() - GetDiscountAmount() + GetVAT();
    public double GetChange() => AmountTendered - GetTotal();

    public void PrintReceipt()
    {
        ReceiptPrinter printer = new ReceiptPrinter();
        printer.Print(this);
    }
}

class ReceiptPrinter
{
    private const int Width = 42;

    public void Print(Receipt r)
    {
        string line = new string('-', Width);
        string dline = new string('=', Width);

        Console.Clear();
        Console.WriteLine();
        Console.WriteLine(dline);
        Console.WriteLine(Center(r.StoreName.ToUpper(), Width));
        Console.WriteLine(Center("*** Official Receipt ***", Width));
        Console.WriteLine(dline);
        Console.WriteLine($" Date    : {r.TransactionDate:MM/dd/yyyy hh:mm tt}");
        Console.WriteLine($" Cashier : {r.CashierName}");
        Console.WriteLine($" OR No.  : {GenerateORNumber(r.TransactionDate)}");
        Console.WriteLine(line);

        PrintItemsTable(r, line);
        PrintTotals(r, dline);
        PrintPayment(r, line);
    }

    private void PrintItemsTable(Receipt r, string line)
    {
        Console.WriteLine($" {"ITEM",-18} {"QTY",4} {"PRICE",8} {"TOTAL",8}");
        Console.WriteLine(line);

        foreach (var item in r.GetItems())
        {
            string name = item.Name.Length > 18 ? item.Name.Substring(0, 18) : item.Name;
            Console.WriteLine($" {name,-18} {item.Quantity,4} {item.UnitPrice,8:F2} {item.GetSubtotal(),8:F2}");
        }

        Console.WriteLine(line);
    }

    private void PrintTotals(Receipt r, string dline)
    {
        Console.WriteLine($" {"Subtotal:",-30} PHP {r.GetSubtotal(),6:F2}");

        if (r.DiscountPercent > 0)
            Console.WriteLine($" {"Discount (" + r.DiscountPercent + "%):",-30} PHP {-r.GetDiscountAmount(),6:F2}");

        Console.WriteLine($" {"VAT (12%):",-30} PHP {r.GetVAT(),6:F2}");
        Console.WriteLine(dline);
        Console.WriteLine($" {"TOTAL:",-30} PHP {r.GetTotal(),6:F2}");
        Console.WriteLine(dline);
    }

    private void PrintPayment(Receipt r, string line)
    {
        Console.WriteLine($" Payment  : {r.PaymentMethod}");

        if (r.PaymentMethod == "Cash")
        {
            Console.WriteLine($" {"Amount Tendered:",-30} PHP {r.AmountTendered,6:F2}");
            Console.WriteLine($" {"Change:",-30} PHP {r.GetChange(),6:F2}");
        }

        Console.WriteLine(line);
        Console.WriteLine(Center("Thank you for shopping!", Width));
        Console.WriteLine(Center("Please come again! :)", Width));
        Console.WriteLine(line);
    }

    private string Center(string text, int width)
    {
        if (text.Length >= width) return text;
        int spaces = (width - text.Length) / 2;
        return new string(' ', spaces) + text;
    }

    private string GenerateORNumber(DateTime date)
        => $"OR-{date:yyyyMMdd}-{new Random().Next(1000, 9999)}";
}