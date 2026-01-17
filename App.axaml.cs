using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Ofertomator.Services;
using Ofertomator.ViewModels;
using Ofertomator.Views;
using QuestPDF.Infrastructure;
using System;

namespace Ofertomator;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        // Ustaw licencję QuestPDF (Community dla użytku niekomercyjnego)
        QuestPDF.Settings.License = LicenseType.Community;
        
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            // Konfiguracja Dependency Injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Rozwiązanie MainWindow z DI
                var mainWindow = new MainWindow();
                
                // Func<Window?> dla przekazywania MainWindow do ViewModeli (potrzebne dla dialogów)
                Func<Avalonia.Controls.Window?> getMainWindow = () => mainWindow;
                
                var databaseService = _serviceProvider.GetRequiredService<DatabaseService>();
                var pdfService = _serviceProvider.GetRequiredService<IPdfService>();
                mainWindow.DataContext = new MainViewModel(databaseService, pdfService, getMainWindow);

                desktop.MainWindow = mainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Błąd inicjalizacji: {ex.Message}");
            System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            throw;
        }
    }

    /// <summary>
    /// Konfiguracja serwisów w kontenerze DI
    /// </summary>
    private void ConfigureServices(IServiceCollection services)
    {
        // Singleton - jedna instancja dla całej aplikacji
        services.AddSingleton<DatabaseService>(provider => 
            new DatabaseService("ofertomator.db"));
        services.AddSingleton<IPdfService, PdfGeneratorService>();

        // ViewModels jako Transient (nowa instancja przy każdym żądaniu)
        services.AddTransient<MainViewModel>();
        services.AddTransient<ProductsViewModel>();
        services.AddTransient<OfferGeneratorViewModel>();
        services.AddTransient<CategoriesViewModel>();
        services.AddTransient<BusinessCardViewModel>();
    }
}
