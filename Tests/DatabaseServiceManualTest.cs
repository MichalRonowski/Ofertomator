using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ofertomator.Models;
using Ofertomator.Services;

namespace Ofertomator.Tests;

/// <summary>
/// Prosty test manualny funkcjonalności DatabaseService
/// Uruchom: dotnet run --project Ofertomator.Tests
/// </summary>
public class DatabaseServiceManualTest
{
    // WYŁĄCZONE: Nieużywane jako punkt wejścia
    /*
    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("=== Test DatabaseService ===\n");

        var db = new DatabaseService("test_ofertomator.db");

        try
        {
            // Test 1: Inicjalizacja
            Console.WriteLine("✓ Test 1: Inicjalizacja bazy danych");
            await db.InitializeDatabaseAsync();
            Console.WriteLine("  Baza zainicjalizowana pomyślnie\n");

            // Test 2: Kategorie
            Console.WriteLine("✓ Test 2: Operacje na kategoriach");
            var categories = await db.GetCategoriesAsync();
            Console.WriteLine($"  Liczba kategorii: {System.Linq.Enumerable.Count(categories)}");

            var newCategory = new Category
            {
                Name = "Elektronika",
                DefaultMargin = 25m
            };
            var categoryId = await db.AddCategoryAsync(newCategory);
            Console.WriteLine($"  Dodano kategorię: {newCategory.Name} (ID: {categoryId})\n");

            // Test 3: Produkty
            Console.WriteLine("✓ Test 3: Dodawanie produktów");
            var product1 = new Product
            {
                Code = "TEST001",
                Name = "Testowy Produkt 1",
                Unit = "szt.",
                PurchasePriceNet = 10.50m,
                VatRate = 23m,
                CategoryId = categoryId
            };

            var productId = await db.AddProductAsync(product1);
            Console.WriteLine($"  Dodano produkt: {product1.Name} (ID: {productId})");
            Console.WriteLine($"  Cena: {product1.PurchasePriceNet:N2} PLN\n");

            // Test 4: Paginacja
            Console.WriteLine("✓ Test 4: Paginacja produktów");
            var (products, totalCount) = await db.GetProductsAsync(1, 10);
            Console.WriteLine($"  Liczba produktów: {totalCount}");
            Console.WriteLine($"  Produktów na stronie: {System.Linq.Enumerable.Count(products)}\n");

            // Test 5: Wyszukiwanie
            Console.WriteLine("✓ Test 5: Wyszukiwanie produktów");
            var (searchResults, searchCount) = await db.GetProductsAsync(1, 10, "Testowy");
            Console.WriteLine($"  Znaleziono produktów: {searchCount}\n");

            // Test 6: BusinessCard
            Console.WriteLine("✓ Test 6: Wizytówka");
            var card = await db.GetBusinessCardAsync();
            card.Company = "Test Company";
            card.FullName = "Jan Kowalski";
            card.Phone = "+48 123 456 789";
            card.Email = "jan@example.com";
            await db.UpdateBusinessCardAsync(card);
            Console.WriteLine($"  Zapisano wizytówkę: {card.Company}\n");

            // Test 7: Batch Import
            Console.WriteLine("✓ Test 7: Batch import (10 produktów)");
            var batchProducts = new List<Product>();
            for (int i = 1; i <= 10; i++)
            {
                batchProducts.Add(new Product
                {
                    Code = $"BATCH{i:D3}",
                    Name = $"Produkt Batch {i}",
                    PurchasePriceNet = i * 5.5m,
                    VatRate = 23m,
                    CategoryId = categoryId
                });
            }

            var (added, updated) = await db.ImportProductsBatchAsync(batchProducts, false);
            Console.WriteLine($"  Dodano: {added}, Zaktualizowano: {updated}\n");

            // Podsumowanie
            Console.WriteLine("=== Wszystkie testy PASSED ✅ ===");
            Console.WriteLine("\nBaza danych działa poprawnie!");
            Console.WriteLine($"Plik bazy: test_ofertomator.db");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ BŁĄD: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        Console.WriteLine("\nNaciśnij Enter, aby zakończyć...");
        Console.ReadLine();
    }
    */
}
