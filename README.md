# DDiary

**DDiary** è un'applicazione desktop Windows 11 per la gestione di un diario alimentare quotidiano per persone diabetiche.

---

## Requisiti di sistema

| Componente | Versione |
|---|---|
| Sistema operativo | Windows 10 / 11 (x64) |
| .NET Runtime | .NET 10 Windows |
| Framework UI | WPF |

---

## Architettura

```
DDiary/
├── App.xaml / App.xaml.cs          ← Entry point & DI container
├── Commands/                        ← RelayCommand MVVM
├── Converters/                      ← IValueConverter WPF
├── Data/                            ← DbContext EF Core (SQLite)
├── Helpers/                         ← ThemeManager, extra converters
├── Models/                          ← Entità dominio
├── Repositories/                    ← Pattern Repository
├── Services/                        ← Business logic (DiaryService, ExportService...)
├── Themes/                          ← ResourceDictionary Light/Dark + Styles
├── ViewModels/                      ← MVVM ViewModels
└── Views/                           ← XAML Views + code-behind
```

**Pattern:** MVVM  
**Persistenza:** SQLite via Entity Framework Core  
**DI:** Microsoft.Extensions.DependencyInjection  

---

## Come costruire ed eseguire

### Prerequisiti

1. Installare [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
2. Aprire il terminale (PowerShell o CMD)

### Build

```powershell
cd DDiary
dotnet restore
dotnet build
```

### Esecuzione

```powershell
dotnet run --project DDiary/DDiary.csproj
```

### Pubblicazione (Release)

```powershell
dotnet publish DDiary/DDiary.csproj -c Release -r win-x64 --self-contained true
```

---

## Funzionalità principali

| Funzionalità | Stato |
|---|---|
| Diario giornaliero con sezioni pasto | ✅ |
| Inserimento rapido alimento (FAB + modale) | ✅ |
| Auto-mapping pasto per ora | ✅ |
| Storico navigabile con filtri | ✅ |
| Esportazione PNG | ✅ |
| Esportazione PDF | ✅ |
| Tema Light / Dark / System | ✅ |
| Impostazioni personalizzabili | ✅ |
| Profili locali multipli | ✅ |
| Notifiche desktop Windows | ✅ |
| Promemoria giornaliero configurabile | ✅ |

---

## Database

Il database SQLite viene creato automaticamente al primo avvio in:

```
%LOCALAPPDATA%\DDiary\ddiary.db
```

---

## Sviluppi futuri suggeriti

- Autenticazione / login online (l'architettura è già predisposta)
- Sincronizzazione cloud
- Import/export CSV
- Grafici statistici glicemia / CHO
- Integrazione con device glucometro
- Widget taskbar Windows 11

