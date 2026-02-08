# Ofertomator - Instalacja i Aktualizacja

## 📦 Wersje do pobrania

Dostępne są dwie wersje aplikacji:

### 1. **Self-Contained** (Rekomendowana)
- ✅ Nie wymaga instalacji .NET 8
- ✅ Działa "od razu po rozpakowaniu"
- ❌ Większy rozmiar (~80-100 MB)
- 📁 Plik: `Ofertomator-vX.X.X-SelfContained-win-x64.zip`

### 2. **Framework-Dependent** (Dla zaawansowanych)
- ✅ Mniejszy rozmiar (~10-15 MB)
- ❌ Wymaga zainstalowanego .NET 8 Desktop Runtime
- 📁 Plik: `Ofertomator-vX.X.X-FrameworkDependent-win-x64.zip`
- 🔗 Pobierz .NET 8: https://dotnet.microsoft.com/download/dotnet/8.0

---

## 🆕 Pierwsza instalacja

1. **Pobierz** odpowiednią wersję (rekomendujemy Self-Contained)
2. **Rozpakuj** archiwum ZIP do wybranego folderu, np.:
   ```
   C:\Program Files\Ofertomator
   ```
   lub
   ```
   C:\Apps\Ofertomator
   ```
3. **Uruchom** aplikację klikając na `Ofertomator.exe`
4. **(Opcjonalnie)** Utwórz skrót na pulpicie

### 📊 Gdzie przechowywane są dane?

Wszystkie Twoje dane (produkty, kategorie, oferty, wizytówki) są **bezpiecznie przechowywane** w:

```
%APPDATA%\Ofertomator\ofertomator.db
```

Pełna ścieżka to zazwyczaj:
```
C:\Users\TwojaNazwaUżytkownika\AppData\Roaming\Ofertomator\ofertomator.db
```

**Ważne:** Ten folder NIE znajduje się w folderze zainstalowanej aplikacji!

---

## 🔄 Aktualizacja do nowszej wersji

### ✅ Bezpieczna aktualizacja - Twoje dane nie zostaną usunięte!

Dzięki temu, że baza danych jest przechowywana osobno w folderze `%APPDATA%`, możesz **bezpiecznie** aktualizować aplikację:

### Kroki aktualizacji:

1. **Zamknij** aplikację Ofertomator (jeśli jest uruchomiona)

2. **Pobierz** nową wersję

3. **Rozpakuj** nową wersję i **nadpisz** stare pliki w folderze instalacji

4. **Uruchom** aplikację - wszystkie Twoje dane będą na swoim miejscu! 🎉

### Alternatywnie (zalecane dla pewności):

1. **Zamknij** aplikację
2. **Usuń** cały stary folder z aplikacją (np. `C:\Program Files\Ofertomator`)
3. **Rozpakuj** nową wersję w to samo lub nowe miejsce
4. **Uruchom** - Twoje dane są bezpieczne w `%APPDATA%`!

---

## 💾 Tworzenie kopii zapasowej danych

Mimo że aktualizacja jest bezpieczna, dobrą praktyką jest tworzenie kopii zapasowych:

### Ręczna kopia zapasowa:

1. Otwórz folder:
   ```
   %APPDATA%\Ofertomator
   ```
   (Możesz wpisać to w pasku adresu Eksploratora Windows)

2. Skopiuj plik `ofertomator.db` w bezpieczne miejsce

### Przywracanie z kopii zapasowej:

1. **Zamknij aplikację Ofertomator całkowicie**
   - Sprawdź Menedżer zadań (Ctrl+Shift+Esc) czy proces `Ofertomator.exe` nie jest uruchomiony

2. **Otwórz folder z bazą danych:**
   - Naciśnij `Win + R`
   - Wpisz: `%APPDATA%\Ofertomator`
   - Kliknij OK

3. **Zmień nazwę starego pliku:**
   - Kliknij prawym na `ofertomator.db`
   - Wybierz "Zmień nazwę"
   - Zmień na: `ofertomator_STARY.db`

4. **Skopiuj kopię zapasową:**
   - Skopiuj swoją kopię zapasową do tego folderu
   - Upewnij się że nazywa się dokładnie: `ofertomator.db`

5. **Uruchom aplikację**

**WAŻNE:** Jeśli system nie pozwala zmienić nazwy pliku, sprawdź w Menedżerze zadań czy Ofertomator jest całkowicie zamknięty lub uruchom komputer ponownie.

---

## ❓ Często zadawane pytania

### Czy stracę dane podczas aktualizacji?
**NIE!** Twoje dane są przechowywane osobno w folderze `%APPDATA%\Ofertomator`, który nie jest modyfikowany podczas aktualizacji aplikacji.

### Jak sprawdzić wersję aplikacji?
Informacja o wersji jest widoczna w oknie "O programie" w aplikacji (będzie dodana w przyszłej aktualizacji).

### Co zrobić jeśli aplikacja nie uruchamia się po aktualizacji?
1. Upewnij się, że stara wersja aplikacji została całkowicie zamknięta
2. Sprawdź czy posiadasz wszystkie pliki z archiwum
3. Jeśli używasz wersji Framework-Dependent, upewnij się że masz zainstalowany .NET 8 Desktop Runtime

### Czy mogę przenieść aplikację na inny komputer?
**TAK!** Wystarczy skopiować:
1. Folder z aplikacją
2. Plik bazy danych z `%APPDATA%\Ofertomator\ofertomator.db` i umieścić go w tym samym folderze na nowym komputerze

---

## 🛠️ Wymagania systemowe

- **System operacyjny:** Windows 10/11 (64-bit)
- **.NET 8 Desktop Runtime:** Tylko dla wersji Framework-Dependent
- **Miejsce na dysku:** ~100-150 MB (Self-Contained) lub ~20-30 MB (Framework-Dependent)
- **RAM:** Minimum 2 GB

---

## 📞 Wsparcie

W razie problemów z instalacją lub aktualizacją, skontaktuj się z nami.

---

**Wersja dokumentu:** 1.0  
**Data aktualizacji:** Luty 2026
