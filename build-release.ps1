# Skrypt do budowania wersji Release aplikacji Ofertomator
# 
# Tworzy dwie wersje:
# 1. Self-contained - zawiera runtime .NET (wiekszy rozmiar, nie wymaga instalacji .NET)
# 2. Framework-dependent - wymaga zainstalowanego .NET 8 (mniejszy rozmiar)

param(
    [string]$Version = "1.0.0",
    [switch]$SelfContained,
    [switch]$FrameworkDependent,
    [switch]$All
)

$ErrorActionPreference = "Stop"

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  Ofertomator - Build Release v$Version" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Funkcja do czyszczenia folderu Release
function Clean-ReleaseFolder {
    Write-Host "Czyszczenie poprzednich buildow..." -ForegroundColor Yellow
    if (Test-Path ".\Release") {
        Remove-Item -Recurse -Force ".\Release"
    }
    New-Item -ItemType Directory -Path ".\Release" | Out-Null
}

# Funkcja do budowania Self-Contained
function Build-SelfContained {
    Write-Host ""
    Write-Host "Budowanie wersji Self-Contained..." -ForegroundColor Green
    Write-Host "(Zawiera runtime .NET - nie wymaga instalacji .NET 8)" -ForegroundColor Gray
    
    dotnet publish -c Release --self-contained true --runtime win-x64 -p:PublishSingleFile=false -p:Version=$Version -o ".\Release\Ofertomator-v$Version-SelfContained"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Self-Contained build zakonczony sukcesem!" -ForegroundColor Green
        
        # Skopiuj plik CZYTAJ_MNIE.txt do folderu Release
        Copy-Item -Path ".\CZYTAJ_MNIE.txt" -Destination ".\Release\Ofertomator-v$Version-SelfContained\" -Force
        
        # Utworz archiwum ZIP
        $zipName = "Ofertomator-v$Version-SelfContained-win-x64.zip"
        Compress-Archive -Path ".\Release\Ofertomator-v$Version-SelfContained\*" -DestinationPath ".\Release\$zipName" -Force
        Write-Host "Utworzono archiwum: $zipName" -ForegroundColor Green
    } else {
        Write-Host "Build Self-Contained nie powiodl sie!" -ForegroundColor Red
        exit 1
    }
}

# Funkcja do budowania Framework-Dependent
function Build-FrameworkDependent {
    Write-Host ""
    Write-Host "Budowanie wersji Framework-Dependent..." -ForegroundColor Green
    Write-Host "(Wymaga zainstalowanego .NET 8 Desktop Runtime)" -ForegroundColor Gray
    
    dotnet publish -c Release --self-contained false --runtime win-x64 -p:Version=$Version -o ".\Release\Ofertomator-v$Version-FrameworkDependent"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Framework-Dependent build zakonczony sukcesem!" -ForegroundColor Green
        
        # Skopiuj plik CZYTAJ_MNIE.txt do folderu Release
        Copy-Item -Path ".\CZYTAJ_MNIE.txt" -Destination ".\Release\Ofertomator-v$Version-FrameworkDependent\" -Force
        
        # Utworz archiwum ZIP
        $zipName = "Ofertomator-v$Version-FrameworkDependent-win-x64.zip"
        Compress-Archive -Path ".\Release\Ofertomator-v$Version-FrameworkDependent\*" -DestinationPath ".\Release\$zipName" -Force
        Write-Host "Utworzono archiwum: $zipName" -ForegroundColor Green
    } else {
        Write-Host "Build Framework-Dependent nie powiodl sie!" -ForegroundColor Red
        exit 1
    }
}

# Glowna logika
Clean-ReleaseFolder

if ($All -or (!$SelfContained -and !$FrameworkDependent)) {
    Build-SelfContained
    Build-FrameworkDependent
} else {
    if ($SelfContained) {
        Build-SelfContained
    }
    if ($FrameworkDependent) {
        Build-FrameworkDependent
    }
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  Build zakonczony!" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Pliki znajduja sie w folderze: .\Release" -ForegroundColor Yellow
Write-Host ""
Write-Host "WAZNE: Baza danych uzytkownika jest przechowywana w:" -ForegroundColor Yellow
Write-Host "%APPDATA%\Ofertomator\ofertomator.db" -ForegroundColor White
Write-Host ""
Write-Host "Aktualizacja polega na nadpisaniu tylko plikow aplikacji." -ForegroundColor Yellow
Write-Host "Dane uzytkownika pozostana nienaruszone!" -ForegroundColor Green
