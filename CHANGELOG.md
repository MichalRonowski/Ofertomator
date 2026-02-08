# Historia zmian - Ofertomator

## [1.0.0] - 2026-02-08

### ✨ Funkcje
- Zarządzanie produktami (dodawanie, edycja, usuwanie, wyszukiwanie)
- Zarządzanie kategoriami produktów z hierarchią grup
- Import produktów z plików CSV/Excel
- Generowanie ofert handlowych w formacie PDF
- Możliwość zmiany kolejności produktów w ofercie (drag & drop)
- Wizytówka firmowa z edycją danych
- Zapis i wczytywanie ofert
- Zaznaczanie wielu produktów (checkboxy)
- Filtrowanie produktów po kategoriach
- Wyszukiwanie produktów w czasie rzeczywistym
- Obsługa polskich znaków w wyszukiwaniu

### 🔧 Techniczne
- Framework: .NET 8 + Avalonia UI
- Architektura: MVVM z CommunityToolkit.Mvvm
- Baza danych: SQLite (przechowywana w %APPDATA%\Ofertomator)
- Generowanie PDF: QuestPDF
- Obsługa polskich znaków w sortowaniu i wyszukiwaniu

### 📦 Dostępne wersje
- Self-Contained (zawiera runtime .NET)
- Framework-Dependent (wymaga .NET 8 Desktop Runtime)

---

## Format wersji

Format wersjonowania: `MAJOR.MINOR.PATCH`

- **MAJOR** - Poważne zmiany, mogące łamać kompatybilność
- **MINOR** - Nowe funkcje, zachowanie kompatybilności wstecznej
- **PATCH** - Poprawki błędów i małe usprawnienia

---

## Planowane funkcje (backlog)

- [ ] Export produktów do CSV/Excel
- [ ] Szablony ofert
- [ ] Raporty sprzedaży
- [ ] Import/export bazy danych
- [ ] Auto-backup bazy danych
- [ ] Okno "O programie" z numerem wersji
- [ ] Automatyczne sprawdzanie aktualizacji
