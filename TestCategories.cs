using System;
using System.Linq;
using System.Threading.Tasks;
using Ofertomator.Services;

public class TestCategories
{
    // WYŁĄCZONE: Nieużywane jako punkt wejścia
    /*
    public static async Task Main()
    {
        Console.WriteLine("=== TEST START ===");
        var db = new DatabaseService("ofertomator.db");
        
        var categories = (await db.GetCategoriesAsync()).ToList();
        Console.WriteLine($"Categories count: {categories.Count}");
        
        foreach (var cat in categories)
        {
            Console.WriteLine($"- {cat.Name} (ID: {cat.Id})");
        }
        
        Console.WriteLine("=== TEST END ===");
    }
    */
}
