using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Ofertomator.Models;

namespace Ofertomator.Services;

/// <summary>
/// Serwis do zarządzania bazą danych SQLite
/// Wszystkie operacje są asynchroniczne dla zachowania responsywności UI
/// </summary>
public class DatabaseService : IDisposable
{
    private readonly string _connectionString;
    private const int CommandTimeout = 10; // 10 sekund timeout

    public DatabaseService(string databasePath = "ofertomator.db")
    {
        _connectionString = $"Data Source={databasePath};";
        
        // Inicjalizacja bazy danych przy pierwszym użyciu
        InitializeDatabaseAsync().Wait();
    }

    /// <summary>
    /// Tworzy połączenie z bazą danych
    /// </summary>
    private IDbConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        // Dodaj custom funkcję dla polskich znaków
        CreatePolishLowerFunction(connection);
        
        return connection;
    }
    
    /// <summary>
    /// Tworzy funkcję POLISH_LOWER() do konwersji tekstu na małe litery z obsługą polskich znaków
    /// </summary>
    private void CreatePolishLowerFunction(SqliteConnection connection)
    {
        connection.CreateFunction("POLISH_LOWER", (string text) =>
        {
            if (string.IsNullOrEmpty(text))
                return text;
            
            // Używamy kultury polskiej do konwersji na małe litery
            return text.ToLower(System.Globalization.CultureInfo.GetCultureInfo("pl-PL"));
        });
    }

    /// <summary>
    /// Inicjalizacja struktury bazy danych z optymalizacjami
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        using var connection = CreateConnection();

        // Tabela Categories
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS Categories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT UNIQUE NOT NULL COLLATE NOCASE,
                DefaultMargin REAL DEFAULT 0.0,
                DisplayOrder INTEGER DEFAULT 0
            );
        ");

        // Tabela Products z indeksami
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS Products (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Code TEXT COLLATE NOCASE,
                Name TEXT NOT NULL COLLATE NOCASE,
                Unit TEXT DEFAULT 'szt.',
                PurchasePriceNet REAL DEFAULT 0.0,
                PriceUpdateDate TEXT,
                VatRate REAL DEFAULT 23.0,
                CategoryId INTEGER NOT NULL,
                FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
            );
        ");

        // Indeksy dla optymalizacji wyszukiwania
        await connection.ExecuteAsync(@"
            CREATE INDEX IF NOT EXISTS idx_products_category 
            ON Products(CategoryId);
        ");

        await connection.ExecuteAsync(@"
            CREATE INDEX IF NOT EXISTS idx_products_code 
            ON Products(Code) WHERE Code IS NOT NULL;
        ");

        await connection.ExecuteAsync(@"
            CREATE INDEX IF NOT EXISTS idx_products_name 
            ON Products(Name COLLATE NOCASE);
        ");

        // Tabela BusinessCard (pojedynczy rekord)
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS BusinessCard (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                Company TEXT,
                FullName TEXT,
                Phone TEXT,
                Email TEXT
            );
        ");

        // Tabela SavedOffers
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS SavedOffers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                CreatedDate TEXT NOT NULL,
                ModifiedDate TEXT NOT NULL,
                CategoryOrder TEXT
            );
        ");

        // Tabela SavedOfferItems z kaskadowym usuwaniem
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS SavedOfferItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OfferId INTEGER NOT NULL,
                ProductId INTEGER,
                Name TEXT NOT NULL,
                CategoryName TEXT,
                Unit TEXT DEFAULT 'szt.',
                PurchasePriceNet REAL DEFAULT 0.0,
                VatRate REAL DEFAULT 23.0,
                Margin REAL DEFAULT 0.0,
                Quantity REAL DEFAULT 1.0,
                FOREIGN KEY (OfferId) REFERENCES SavedOffers(Id) ON DELETE CASCADE,
                FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE SET NULL
            );
        ");

        // Indeks dla optymalizacji zapytań o pozycje oferty
        await connection.ExecuteAsync(@"
            CREATE INDEX IF NOT EXISTS idx_offer_items_offer 
            ON SavedOfferItems(OfferId);
        ");

        // === MIGRACJE ===
        // Migracja: Dodanie kolumny DisplayOrder do Categories (jeśli nie istnieje)
        var hasDisplayOrder = await connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) 
            FROM pragma_table_info('Categories') 
            WHERE name='DisplayOrder'
        ");

        if (hasDisplayOrder == 0)
        {
            await connection.ExecuteAsync(@"
                ALTER TABLE Categories 
                ADD COLUMN DisplayOrder INTEGER DEFAULT 0
            ");
            
            // Ustaw "Bez kategorii" na koniec (9999)
            await connection.ExecuteAsync(@"
                UPDATE Categories 
                SET DisplayOrder = 9999 
                WHERE Name = 'Bez kategorii'
            ");
        }

        // Migracja: Dodanie kolumny CustomName do SavedOfferItems (jeśli nie istnieje)
        var hasCustomName = await connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) 
            FROM pragma_table_info('SavedOfferItems') 
            WHERE name='CustomName'
        ");

        if (hasCustomName == 0)
        {
            await connection.ExecuteAsync(@"
                ALTER TABLE SavedOfferItems 
                ADD COLUMN CustomName TEXT
            ");
        }

        // Dodanie domyślnej kategorii "Bez kategorii" jeśli nie istnieje
        var categoryExists = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Categories WHERE Name = @Name",
            new { Name = "Bez kategorii" }
        );

        if (categoryExists == 0)
        {
            await connection.ExecuteAsync(
                "INSERT INTO Categories (Name, DefaultMargin, DisplayOrder) VALUES (@Name, @DefaultMargin, @DisplayOrder)",
                new { Name = "Bez kategorii", DefaultMargin = 0.0m, DisplayOrder = 9999 }
            );
        }

        // Inicjalizacja pustej wizytówki jeśli nie istnieje
        var businessCardExists = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM BusinessCard WHERE Id = 1"
        );

        if (businessCardExists == 0)
        {
            await connection.ExecuteAsync(
                "INSERT INTO BusinessCard (Id, Company, FullName, Phone, Email) VALUES (1, '', '', '', '')"
            );
        }
    }

    #region Category Operations

    /// <summary>
    /// Pobiera wszystkie kategorie
    /// </summary>
    public async Task<IEnumerable<Category>> GetCategoriesAsync()
    {
        try
        {
            using var connection = CreateConnection();
            return await connection.QueryAsync<Category>(
                "SELECT * FROM Categories ORDER BY DisplayOrder, Name"
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas pobierania kategorii: {ex.Message}");
            return new List<Category>();
        }
    }

    /// <summary>
    /// Pobiera kategorię po ID
    /// </summary>
    public async Task<Category?> GetCategoryByIdAsync(int id)
    {
        try
        {
            using var connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Category>(
                "SELECT * FROM Categories WHERE Id = @Id",
                new { Id = id }
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas pobierania kategorii: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Dodaje nową kategorię
    /// </summary>
    public async Task<int> AddCategoryAsync(Category category)
    {
        try
        {
            using var connection = CreateConnection();
            return await connection.ExecuteScalarAsync<int>(@"
                INSERT INTO Categories (Name, DefaultMargin, DisplayOrder) 
                VALUES (@Name, @DefaultMargin, @DisplayOrder);
                SELECT last_insert_rowid();",
                category
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas dodawania kategorii: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Aktualizuje kategorię
    /// </summary>
    public async Task<bool> UpdateCategoryAsync(Category category)
    {
        try
        {
            using var connection = CreateConnection();
            var result = await connection.ExecuteAsync(@"
                UPDATE Categories 
                SET Name = @Name, DefaultMargin = @DefaultMargin, DisplayOrder = @DisplayOrder 
                WHERE Id = @Id",
                category
            );
            return result > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas aktualizacji kategorii: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Usuwa kategorię (tylko jeśli nie ma przypisanych produktów)
    /// </summary>
    public async Task<bool> DeleteCategoryAsync(int id)
    {
        try
        {
            // Nie można usunąć kategorii "Bez kategorii"
            var category = await GetCategoryByIdAsync(id);
            if (category?.Name == "Bez kategorii")
            {
                return false;
            }

            using var connection = CreateConnection();
            
            // Sprawdź czy są produkty w tej kategorii
            var productCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Products WHERE CategoryId = @Id",
                new { Id = id }
            );

            if (productCount > 0)
            {
                return false; // Nie można usunąć kategorii z produktami
            }

            var result = await connection.ExecuteAsync(
                "DELETE FROM Categories WHERE Id = @Id",
                new { Id = id }
            );
            
            return result > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas usuwania kategorii: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Zwraca liczbę produktów w kategorii
    /// </summary>
    public async Task<int> GetProductsCountByCategoryAsync(int categoryId)
    {
        try
        {
            using var connection = CreateConnection();
            return await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Products WHERE CategoryId = @CategoryId",
                new { CategoryId = categoryId }
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas pobierania liczby produktów: {ex.Message}");
            return 0;
        }
    }

    #endregion

    #region Produkty

    /// <summary>
    /// Pobiera produkty z paginacją i wyszukiwaniem
    /// </summary>
    public async Task<(IEnumerable<Product> Products, int TotalCount)> GetProductsAsync(int pageNumber, int pageSize, string? searchQuery = null)
    {
        try
        {
            using var connection = CreateConnection();
            
            var offset = (pageNumber - 1) * pageSize;
            var whereClause = string.IsNullOrWhiteSpace(searchQuery) 
                ? "" 
                : "WHERE POLISH_LOWER(p.Name) LIKE POLISH_LOWER(@Search) OR POLISH_LOWER(p.Code) LIKE POLISH_LOWER(@Search)";
            
            var searchParam = $"%{searchQuery}%";

            // Pobierz łączną liczbę produktów
            var totalCount = await connection.ExecuteScalarAsync<int>(
                $"SELECT COUNT(*) FROM Products p {whereClause}",
                new { Search = searchParam }
            );

            // Pobierz produkty z kategorią
            var products = await connection.QueryAsync<Product, Category, Product>(
                $@"SELECT p.*, c.* FROM Products p
                   INNER JOIN Categories c ON p.CategoryId = c.Id
                   {whereClause}
                   ORDER BY p.Name
                   LIMIT @PageSize OFFSET @Offset",
                (product, category) =>
                {
                    product.Category = category;
                    return product;
                },
                new { PageSize = pageSize, Offset = offset, Search = searchParam },
                splitOn: "Id"
            );

            return (products, totalCount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas pobierania produktów: {ex.Message}");
            return (new List<Product>(), 0);
        }
    }

    /// <summary>
    /// Pobiera produkty z danej kategorii (opcjonalnie z limitem dla wydajności)
    /// </summary>
    public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId, int? limit = null)
    {
        try
        {
            using var connection = CreateConnection();
            var sql = @"SELECT p.*, c.* FROM Products p
                  INNER JOIN Categories c ON p.CategoryId = c.Id
                  WHERE p.CategoryId = @CategoryId
                  ORDER BY p.Name";
            
            if (limit.HasValue)
            {
                sql += " LIMIT @Limit";
            }
            
            return await connection.QueryAsync<Product, Category, Product>(
                sql,
                (product, category) =>
                {
                    product.Category = category;
                    return product;
                },
                new { CategoryId = categoryId, Limit = limit },
                splitOn: "Id"
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas pobierania produktów kategorii: {ex.Message}");
            return new List<Product>();
        }
    }

    /// <summary>
    /// Pobiera liczbę produktów w kategorii
    /// </summary>
    public async Task<int> GetProductCountByCategoryAsync(int categoryId)
    {
        try
        {
            using var connection = CreateConnection();
            return await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Products WHERE CategoryId = @CategoryId",
                new { CategoryId = categoryId }
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas zliczania produktów: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Pobiera produkt po ID
    /// </summary>
    public async Task<Product?> GetProductByIdAsync(int id)
    {
        try
        {
            using var connection = CreateConnection();
            var products = await connection.QueryAsync<Product, Category, Product>(
                @"SELECT p.*, c.* FROM Products p
                  INNER JOIN Categories c ON p.CategoryId = c.Id
                  WHERE p.Id = @Id",
                (product, category) =>
                {
                    product.Category = category;
                    return product;
                },
                new { Id = id },
                splitOn: "Id"
            );
            
            return products.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas pobierania produktu: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Pobiera produkt po kodzie
    /// </summary>
    public async Task<Product?> GetProductByCodeAsync(string code)
    {
        try
        {
            using var connection = CreateConnection();
            var products = await connection.QueryAsync<Product, Category, Product>(
                @"SELECT p.*, c.* FROM Products p
                  INNER JOIN Categories c ON p.CategoryId = c.Id
                  WHERE p.Code = @Code COLLATE NOCASE",
                (product, category) =>
                {
                    product.Category = category;
                    return product;
                },
                new { Code = code },
                splitOn: "Id"
            );
            
            return products.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas pobierania produktu po kodzie: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Dodaje nowy produkt
    /// </summary>
    public async Task<int> AddProductAsync(Product product)
    {
        try
        {
            using var connection = CreateConnection();
            product.PriceUpdateDate = DateTime.Now;
            
            return await connection.ExecuteScalarAsync<int>(@"
                INSERT INTO Products (Code, Name, Unit, PurchasePriceNet, PriceUpdateDate, VatRate, CategoryId)
                VALUES (@Code, @Name, @Unit, @PurchasePriceNet, @PriceUpdateDate, @VatRate, @CategoryId);
                SELECT last_insert_rowid();",
                product
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas dodawania produktu: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Aktualizuje produkt
    /// </summary>
    public async Task<bool> UpdateProductAsync(Product product)
    {
        try
        {
            using var connection = CreateConnection();
            product.PriceUpdateDate = DateTime.Now;
            
            var result = await connection.ExecuteAsync(@"
                UPDATE Products 
                SET Code = @Code, 
                    Name = @Name, 
                    Unit = @Unit, 
                    PurchasePriceNet = @PurchasePriceNet,
                    PriceUpdateDate = @PriceUpdateDate,
                    VatRate = @VatRate,
                    CategoryId = @CategoryId
                WHERE Id = @Id",
                product
            );
            
            return result > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas aktualizacji produktu: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Usuwa produkt
    /// </summary>
    public async Task<bool> DeleteProductAsync(int id)
    {
        try
        {
            using var connection = CreateConnection();
            var result = await connection.ExecuteAsync(
                "DELETE FROM Products WHERE Id = @Id",
                new { Id = id }
            );
            return result > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas usuwania produktu: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Usuwa wiele produktów jednocześnie (batch delete)
    /// </summary>
    public async Task<int> DeleteProductsAsync(IEnumerable<int> ids)
    {
        try
        {
            using var connection = CreateConnection();
            var idList = string.Join(",", ids);
            return await connection.ExecuteAsync(
                $"DELETE FROM Products WHERE Id IN ({idList})"
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas usuwania produktów: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Import batch'owy produktów (optymalizacja wydajności)
    /// </summary>
    public async Task<(int Added, int Updated)> ImportProductsBatchAsync(
        IEnumerable<Product> products, 
        bool updateExisting = true)
    {
        int added = 0;
        int updated = 0;

        try
        {
            using var connection = CreateConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var product in products)
                {
                    product.PriceUpdateDate = DateTime.Now;

                    // Sprawdź czy produkt istnieje (po kodzie jeśli jest podany)
                    Product? existing = null;
                    if (!string.IsNullOrWhiteSpace(product.Code))
                    {
                        existing = await GetProductByCodeAsync(product.Code);
                    }

                    if (existing != null && updateExisting)
                    {
                        // Aktualizuj istniejący
                        product.Id = existing.Id;
                        await connection.ExecuteAsync(@"
                            UPDATE Products 
                            SET Name = @Name, 
                                Unit = @Unit, 
                                PurchasePriceNet = @PurchasePriceNet,
                                PriceUpdateDate = @PriceUpdateDate,
                                VatRate = @VatRate,
                                CategoryId = @CategoryId
                            WHERE Id = @Id",
                            product,
                            transaction
                        );
                        updated++;
                    }
                    else
                    {
                        // Dodaj nowy
                        await connection.ExecuteAsync(@"
                            INSERT INTO Products (Code, Name, Unit, PurchasePriceNet, PriceUpdateDate, VatRate, CategoryId)
                            VALUES (@Code, @Name, @Unit, @PurchasePriceNet, @PriceUpdateDate, @VatRate, @CategoryId)",
                            product,
                            transaction
                        );
                        added++;
                    }
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas importu batch'owego: {ex.Message}");
            throw;
        }

        return (added, updated);
    }

    #endregion

    #region BusinessCard Operations

    /// <summary>
    /// Pobiera wizytówkę użytkownika
    /// </summary>
    public async Task<BusinessCard> GetBusinessCardAsync()
    {
        try
        {
            using var connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<BusinessCard>(
                "SELECT * FROM BusinessCard WHERE Id = 1"
            ) ?? new BusinessCard();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas pobierania wizytówki: {ex.Message}");
            return new BusinessCard();
        }
    }

    /// <summary>
    /// Aktualizuje wizytówkę użytkownika
    /// </summary>
    public async Task<bool> UpdateBusinessCardAsync(BusinessCard card)
    {
        try
        {
            using var connection = CreateConnection();
            var result = await connection.ExecuteAsync(@"
                UPDATE BusinessCard 
                SET Company = @Company, 
                    FullName = @FullName, 
                    Phone = @Phone, 
                    Email = @Email 
                WHERE Id = 1",
                card
            );
            return result > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas aktualizacji wizytówki: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Zapisuje wizytówkę użytkownika (alias do UpdateBusinessCardAsync)
    /// </summary>
    public Task<bool> SaveBusinessCardAsync(BusinessCard card) => UpdateBusinessCardAsync(card);

    #endregion

    #region Zapisane Oferty

    /// <summary>
    /// Zapisuje ofertę do bazy danych (z transakcją)
    /// </summary>
    public async Task<int> SaveOfferAsync(SavedOffer offer, IEnumerable<SavedOfferItem> items)
    {
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            int offerId;

            // Jeśli oferta ma ID, to edytujemy istniejącą
            if (offer.Id > 0)
            {
                // Usuń stare pozycje
                await connection.ExecuteAsync(
                    "DELETE FROM SavedOfferItems WHERE OfferId = @OfferId",
                    new { OfferId = offer.Id },
                    transaction
                );

                // Aktualizuj nagłówek
                await connection.ExecuteAsync(@"
                    UPDATE SavedOffers 
                    SET Title = @Title, 
                        CreatedDate = @CreatedDate,
                        ModifiedDate = @ModifiedDate 
                    WHERE Id = @Id",
                    offer,
                    transaction
                );

                offerId = offer.Id;
            }
            else
            {
                // Wstaw nowy nagłówek
                offerId = await connection.ExecuteScalarAsync<int>(@"
                    INSERT INTO SavedOffers (Title, CreatedDate, ModifiedDate) 
                    VALUES (@Title, @CreatedDate, @ModifiedDate);
                    SELECT last_insert_rowid();",
                    offer,
                    transaction
                );
            }

            // Wstaw pozycje
            foreach (var item in items)
            {
                await connection.ExecuteAsync(@"
                    INSERT INTO SavedOfferItems (
                        OfferId, ProductId, Name, CategoryName, Unit,
                        PurchasePriceNet, VatRate, Margin, Quantity
                    ) VALUES (
                        @OfferId, @ProductId, @Name, @CategoryName, @Unit,
                        @PurchasePriceNet, @VatRate, @Margin, @Quantity
                    )",
                    new
                    {
                        OfferId = offerId,
                        item.ProductId,
                        item.Name,
                        item.CategoryName,
                        item.Unit,
                        item.PurchasePriceNet,
                        item.VatRate,
                        item.Margin,
                        item.Quantity
                    },
                    transaction
                );
            }

            transaction.Commit();
            return offerId;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine($"Błąd podczas zapisywania oferty: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Pobiera listę zapisanych ofert (posortowaną malejąco po dacie)
    /// </summary>
    public async Task<IEnumerable<SavedOffer>> GetSavedOffersAsync()
    {
        try
        {
            using var connection = CreateConnection();
            return await connection.QueryAsync<SavedOffer>(@"
                SELECT * FROM SavedOffers 
                ORDER BY CreatedDate DESC"
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas pobierania ofert: {ex.Message}");
            return Enumerable.Empty<SavedOffer>();
        }
    }

    /// <summary>
    /// Pobiera pozycje dla danej oferty
    /// </summary>
    public async Task<IEnumerable<SavedOfferItem>> LoadOfferItemsAsync(int offerId)
    {
        try
        {
            using var connection = CreateConnection();
            return await connection.QueryAsync<SavedOfferItem>(@"
                SELECT * FROM SavedOfferItems 
                WHERE OfferId = @OfferId 
                ORDER BY Id",
                new { OfferId = offerId }
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas wczytywania pozycji oferty: {ex.Message}");
            return Enumerable.Empty<SavedOfferItem>();
        }
    }

    /// <summary>
    /// Usuwa ofertę i kaskadowo jej pozycje
    /// </summary>
    public async Task<bool> DeleteOfferAsync(int offerId)
    {
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Usuń pozycje
            await connection.ExecuteAsync(
                "DELETE FROM SavedOfferItems WHERE OfferId = @OfferId",
                new { OfferId = offerId },
                transaction
            );

            // Usuń nagłówek
            var result = await connection.ExecuteAsync(
                "DELETE FROM SavedOffers WHERE Id = @Id",
                new { Id = offerId },
                transaction
            );

            transaction.Commit();
            return result > 0;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine($"Błąd podczas usuwania oferty: {ex.Message}");
            return false;
        }
    }

    #endregion

    public void Dispose()
    {
        // Cleanup jeśli potrzebny
    }
}
