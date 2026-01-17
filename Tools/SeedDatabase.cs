using System;
using System.Threading.Tasks;
using Ofertomator.Models;
using Ofertomator.Services;

namespace Ofertomator.Tools;

/// <summary>
/// Skrypt do dodania testowych danych do bazy
/// Uruchom raz aby wypełnić bazę przykładowymi produktami
/// </summary>
public class SeedDatabase
{
    public static async Task MainAsync()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("=== Wypełnianie bazy testowymi danymi ===\n");

        var db = new DatabaseService("ofertomator.db");

        try
        {
            // Inicjalizacja bazy
            await db.InitializeDatabaseAsync();

            // Pobierz kategorię "Bez kategorii"
            var categories = await db.GetCategoriesAsync();
            var defaultCategory = System.Linq.Enumerable.FirstOrDefault(
                categories, c => c.Name == "Bez kategorii");

            if (defaultCategory == null)
            {
                Console.WriteLine("Błąd: Nie znaleziono kategorii domyślnej");
                return;
            }

            // Dodaj kategorie
            Console.WriteLine("Dodawanie kategorii...");
            var elektronika = new Category { Name = "Elektronika", DefaultMargin = 25m };
            var meble = new Category { Name = "Meble", DefaultMargin = 30m };
            var narzedzia = new Category { Name = "Narzędzia", DefaultMargin = 20m };

            var elektronikId = await db.AddCategoryAsync(elektronika);
            var mebleId = await db.AddCategoryAsync(meble);
            var narzedziaId = await db.AddCategoryAsync(narzedzia);
            
            Console.WriteLine($"  ✓ Dodano {elektronika.Name}");
            Console.WriteLine($"  ✓ Dodano {meble.Name}");
            Console.WriteLine($"  ✓ Dodano {narzedzia.Name}\n");

            // Dodaj produkty - Elektronika
            Console.WriteLine("Dodawanie produktów - Elektronika...");
            var produkty = new[]
            {
                new Product { Code = "EL001", Name = "Laptop Dell XPS 15", Unit = "szt.", PurchasePriceNet = 4500m, VatRate = 23m, CategoryId = elektronikId },
                new Product { Code = "EL002", Name = "Monitor Samsung 27\"", Unit = "szt.", PurchasePriceNet = 1200m, VatRate = 23m, CategoryId = elektronikId },
                new Product { Code = "EL003", Name = "Klawiatura mechaniczna", Unit = "szt.", PurchasePriceNet = 350m, VatRate = 23m, CategoryId = elektronikId },
                new Product { Code = "EL004", Name = "Mysz bezprzewodowa", Unit = "szt.", PurchasePriceNet = 120m, VatRate = 23m, CategoryId = elektronikId },
                new Product { Code = "EL005", Name = "Kabel HDMI 2m", Unit = "szt.", PurchasePriceNet = 25m, VatRate = 23m, CategoryId = elektronikId },
            };

            foreach (var p in produkty)
            {
                await db.AddProductAsync(p);
                Console.WriteLine($"  ✓ {p.Name} - {p.PurchasePriceNet:N2} PLN");
            }

            // Dodaj produkty - Meble
            Console.WriteLine("\nDodawanie produktów - Meble...");
            produkty = new[]
            {
                new Product { Code = "ME001", Name = "Biurko regulowane", Unit = "szt.", PurchasePriceNet = 800m, VatRate = 23m, CategoryId = mebleId },
                new Product { Code = "ME002", Name = "Krzesło biurowe ergonomiczne", Unit = "szt.", PurchasePriceNet = 650m, VatRate = 23m, CategoryId = mebleId },
                new Product { Code = "ME003", Name = "Regał na książki", Unit = "szt.", PurchasePriceNet = 450m, VatRate = 23m, CategoryId = mebleId },
                new Product { Code = "ME004", Name = "Lampka biurkowa LED", Unit = "szt.", PurchasePriceNet = 85m, VatRate = 23m, CategoryId = mebleId },
                new Product { Code = "ME005", Name = "Szafka pod biurko", Unit = "szt.", PurchasePriceNet = 320m, VatRate = 23m, CategoryId = mebleId },
            };

            foreach (var p in produkty)
            {
                await db.AddProductAsync(p);
                Console.WriteLine($"  ✓ {p.Name} - {p.PurchasePriceNet:N2} PLN");
            }

            // Dodaj produkty - Narzędzia
            Console.WriteLine("\nDodawanie produktów - Narzędzia...");
            produkty = new[]
            {
                new Product { Code = "NZ001", Name = "Wiertarka udarowa", Unit = "szt.", PurchasePriceNet = 380m, VatRate = 23m, CategoryId = narzedziaId },
                new Product { Code = "NZ002", Name = "Zestaw wkrętaków", Unit = "zestaw", PurchasePriceNet = 65m, VatRate = 23m, CategoryId = narzedziaId },
                new Product { Code = "NZ003", Name = "Młotek stalowy", Unit = "szt.", PurchasePriceNet = 35m, VatRate = 23m, CategoryId = narzedziaId },
                new Product { Code = "NZ004", Name = "Taśma miernicza 5m", Unit = "szt.", PurchasePriceNet = 18m, VatRate = 23m, CategoryId = narzedziaId },
                new Product { Code = "NZ005", Name = "Poziomica laserowa", Unit = "szt.", PurchasePriceNet = 220m, VatRate = 23m, CategoryId = narzedziaId },
            };

            foreach (var p in produkty)
            {
                await db.AddProductAsync(p);
                Console.WriteLine($"  ✓ {p.Name} - {p.PurchasePriceNet:N2} PLN");
            }

            // Aktualizuj wizytówkę
            Console.WriteLine("\nAktualizacja wizytówki...");
            var card = await db.GetBusinessCardAsync();
            card.Company = "ACME Solutions Sp. z o.o.";
            card.FullName = "Jan Kowalski";
            card.Phone = "+48 123 456 789";
            card.Email = "jan.kowalski@acme.pl";
            await db.UpdateBusinessCardAsync(card);
            Console.WriteLine($"  ✓ Wizytówka: {card.Company}");

            // Podsumowanie
            var (products, total) = await db.GetProductsAsync(1, 1000);
            Console.WriteLine($"\n=== SUKCES ===");
            Console.WriteLine($"Dodano {total} produktów do bazy danych!");
            Console.WriteLine("Możesz teraz uruchomić aplikację: dotnet run");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ BŁĄD: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
