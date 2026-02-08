# Ofertomator - Build & Release Guide

## 🚀 Budowanie wersji Release

### Szybki start

```powershell
# Zbuduj obie wersje (Self-Contained + Framework-Dependent)
.\build-release.ps1 -Version "1.0.0"

# Lub tylko Self-Contained
.\build-release.ps1 -Version "1.0.0" -SelfContained

# Lub tylko Framework-Dependent
.\build-release.ps1 -Version "1.0.0" -FrameworkDependent
```

### Wyjście

Wszystkie buildy są tworzone w folderze `.\Release\`:
```
Release/
├── Ofertomator-v1.0.0-SelfContained/
│   └── [pliki aplikacji]
├── Ofertomator-v1.0.0-FrameworkDependent/
│   └── [pliki aplikacji]
├── Ofertomator-v1.0.0-SelfContained-win-x64.zip
└── Ofertomator-v1.0.0-FrameworkDependent-win-x64.zip
```

---

## 📋 Checklist przed wydaniem

### 1. Zaktualizuj numery wersji

- [ ] `Ofertomator.csproj` - właściwość `<Version>`
- [ ] `CHANGELOG.md` - dodaj sekcję z nową wersją
- [ ] `build-release.ps1` - domyślny parametr `-Version` (opcjonalnie)

### 2. Testowanie

- [ ] Przetestuj aplikację w trybie Debug
- [ ] Sprawdź wszystkie główne funkcje:
  - [ ] Dodawanie/edycja/usuwanie produktów
  - [ ] Zarządzanie kategoriami
  - [ ] Import CSV
  - [ ] Generowanie PDF
  - [ ] Zapisywanie/wczytywanie ofert
  - [ ] Edycja wizytówki
- [ ] Uruchom build Release lokalnie
- [ ] Sprawdź czy baza danych jest w `%APPDATA%\Ofertomator`

### 3. Build Release

```powershell
.\build-release.ps1 -Version "X.Y.Z"
```

### 4. Testowanie Release Build

- [ ] Rozpakuj archiwum Self-Contained
- [ ] Uruchom `Ofertomator.exe`
- [ ] Sprawdź podstawowe funkcje
- [ ] Sprawdź lokalizację bazy danych (`%APPDATA%\Ofertomator`)

### 5. Dystrybucja

- [ ] Prześlij archiwa ZIP do użytkowników
- [ ] Dołącz plik `INSTALACJA.md` z instrukcjami
- [ ] Poinformuj o zmianach (wyślij CHANGELOG)

---

## 🔄 Proces aktualizacji dla użytkowników

### Dlaczego aktualizacja jest bezpieczna?

Od wersji 1.0.0, baza danych jest przechowywana w:
```
%APPDATA%\Ofertomator\ofertomator.db
```

To oznacza, że:
- ✅ Użytkownik może nadpisać folder aplikacji bez utraty danych
- ✅ Użytkownik może usunąć stary folder i zainstalować nowy
- ✅ Dane są oddzielone od plików aplikacji

### Instrukcje dla użytkownika

Przekaż użytkownikom plik `INSTALACJA.md` z pełnymi instrukcjami.

---

## 🧪 Testowanie migracji bazy danych

Jeśli wprowadzasz zmiany w schemacie bazy danych:

### 1. Przygotuj skrypt migracji

Dodaj kod migracji w `DatabaseService.InitializeDatabaseAsync()`:

```csharp
// Sprawdź wersję schematu
var schemaVersion = await GetSchemaVersionAsync();

if (schemaVersion < 2)
{
    await MigrateToVersion2Async();
}
```

### 2. Przetestuj migrację

1. Skopiuj starą bazę danych
2. Uruchom nową wersję aplikacji  
3. Sprawdź czy migracja przebiegła pomyślnie
4. Sprawdź czy dane są zachowane

---

## 🛠️ Ręczne budowanie (bez skryptu)

### Self-Contained

```powershell
dotnet publish -c Release `
    --self-contained true `
    --runtime win-x64 `
    -p:PublishSingleFile=false `
    -p:Version=1.0.0 `
    -o .\Release\Output
```

### Framework-Dependent

```powershell
dotnet publish -c Release `
    --self-contained false `
    --runtime win-x64 `
    -p:Version=1.0.0 `
    -o .\Release\Output
```

---

## 📊 Rozmiary buildów (przybliżone)

- **Self-Contained:** ~80-100 MB
- **Framework-Dependent:** ~10-15 MB
- **Baza danych:** ~100 KB - 10 MB (zależnie od ilości danych)

---

## 🔍 Debugowanie problemów

### Aplikacja nie uruchamia się po update

1. Sprawdź Event Viewer (Dziennik zdarzeń Windows)
2. Sprawdź czy wszystkie pliki DLL są obecne
3. Dla Framework-Dependent: sprawdź czy .NET 8 jest zainstalowany

### Baza danych nie jest widoczna

Sprawdź:
```powershell
$env:APPDATA\Ofertomator
```

---

## 📝 Notatki

- Zawsze testuj build Release przed dystrybucją
- Zachowaj stare wersje archiwów ZIP (dla rollbacku)
- Dokumentuj wszystkie zmiany w CHANGELOG.md
- Informuj użytkowników o breaking changes

---

**Ostatnia aktualizacja:** Luty 2026
