# Podsumowanie przygotowania do aktualizacji v1.0.0

## ✅ Wykonane kroki

### 1. Bezpieczna lokalizacja bazy danych
**Zmieniono:** `App.axaml.cs`
- Baza danych teraz jest przechowywana w: `%APPDATA%\Ofertomator\ofertomator.db`
- Folder automatycznie tworzony przy pierwszym uruchomieniu
- **WAŻNE:** To oznacza że dane użytkownika są bezpieczne podczas aktualizacji!

### 2. Konfiguracja projektu dla Release
**Zmieniono:** `Ofertomator.csproj`
- Dodano informacje o wersji (Version, AssemblyVersion, etc.)
- Skonfigurowano optymalizacje dla buildu Release
- Możliwość łatwego zwiększania numeru wersji

### 3. Skrypt automatycznego budowania
**Utworzono:** `build-release.ps1`
- Automatycznie buduje wersję Self-Contained lub Framework-Dependent
- Tworzy archiwa ZIP gotowe do dystrybucji
- Dodaje plik CZYTAJ_MNIE.txt do każdego archiwum

### 4. Dokumentacja
**Utworzono:**
- `INSTALACJA.md` - szczegółowa instrukcja dla użytkowników końcowych
- `BUILD.md` - instrukcje dla deweloperów
- `CHANGELOG.md` - historia zmian aplikacji
- `CZYTAJ_MNIE.txt` - prosty plik do archiwum ZIP dla użytkownika

---

## 🚀 Jak zbudować wersję Release do dystrybucji?

### Opcja 1: Użyj skryptu (ZALECANE)

```powershell
.\build-release.ps1 -Version "1.0.0"
```

To utworzy:
- `Release\Ofertomator-v1.0.0-SelfContained-win-x64.zip` (~52 MB)
- `Release\Ofertomator-v1.0.0-FrameworkDependent-win-x64.zip` (~10 MB)

### Opcja 2: Tylko Self-Contained (rekomendowana dla użytkowników)

```powershell
.\build-release.ps1 -Version "1.0.0" -SelfContained
```

### Opcja 3: Ręcznie

```powershell
dotnet publish -c Release --self-contained true --runtime win-x64 -p:Version=1.0.0
```

---

## 📦 Co dostanie użytkownik?

Po rozpakowaniu archiwum ZIP użytkownik znajdzie:

```
Ofertomator-v1.0.0-SelfContained/
├── Ofertomator.exe          ← Uruchamia aplikację
├── CZYTAJ_MNIE.txt          ← Instrukcja instalacji
├── Assets/                  ← Zasoby (ikony, logo)
├── LatoFont/               ← Czcionki
└── [pozostałe DLL i pliki runtime]
```

**BRAK pliku ofertomator.db** - baza jest tworzona automatycznie w %APPDATA%!

---

## 🔄 Jak użytkownik zainstaluje aktualizację?

### Dla nowego użytkownika (pierwsza instalacja):
1. Rozpakuj ZIP do wybranego folderu
2. Uruchom Ofertomator.exe
3. Gotowe!

### Dla istniejącego użytkownika (aktualizacja):
1. Zamknij Ofertomator
2. Usuń stary folder z aplikacją
3. Rozpakuj nową wersję (może być w tym samym miejscu)
4. Uruchom - wszystkie dane pozostają nienaruszone!

**Dlaczego to działa?**
Bo baza danych jest w `%APPDATA%\Ofertomator\`, nie w folderze aplikacji!

---

## 📊 Gdzie są dane użytkownika?

### Lokalizacja:
```
C:\Users\[NazwaUżytkownika]\AppData\Roaming\Ofertomator\ofertomator.db
```

Można też wpisać w Eksploratorze Windows:
```
%APPDATA%\Ofertomator
```

### Co jest w bazie danych?
- Wszystkie produkty
- Kategorie i grupy kategorii
- Zapisane oferty
- Wizytówka firmowa

---

## ✅ Checklist przed udostępnieniem aktualizacji

- [ ] Przetestuj aplikację lokalnie
- [ ] Zbuduj Release: `.\build-release.ps1 -Version "1.0.0"`
- [ ] Przetestuj build Release (rozpakuj ZIP i uruchom)
- [ ] Sprawdź czy baza danych tworzy się w %APPDATA%
- [ ] Zaktualizuj CHANGELOG.md o nowe funkcje
- [ ] Prześlij archiwum ZIP użytkownikowi
- [ ] Dołącz instrukcję aktualizacji (INSTALACJA.md)

---

## 🎉 Gotowe!

Aplikacja jest przygotowana do bezpiecznej dystrybucji i aktualizacji!

**Kluczowe zalety:**
✅ Dane użytkownika bezpieczne w %APPDATA%
✅ Prosty proces aktualizacji (usuń stare → rozpakuj nowe)
✅ Automatyczne skrypty budowania
✅ Kompletna dokumentacja dla użytkowników
✅ Wersjonowanie aplikacji

**Data przygotowania:** 8 lutego 2026
