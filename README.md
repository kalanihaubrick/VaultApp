# VaultApp — Cofre de Senhas Offline

Aplicativo desktop WPF (.NET 8) para gerenciamento seguro de senhas, 100% offline.

---

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11
- Visual Studio 2022 (ou `dotnet CLI`)

---

## Build & execução

```bash
# Clonar / abrir a pasta do projeto
cd VaultApp

# Restaurar dependências e compilar
dotnet build

# Executar
dotnet run --project VaultApp/VaultApp.csproj
```

Ou abra `VaultApp.sln` no Visual Studio e pressione **F5**.

---

## Estrutura do projeto

```
VaultApp.sln
├── VaultApp/                        ← Projeto WPF (UI)
│   ├── App.xaml / App.xaml.cs       ← Composição de dependências
│   ├── Converters/
│   │   └── Converters.cs            ← StringToVisibility, InverseBool
│   ├── ViewModels/
│   │   ├── BaseViewModel.cs         ← INotifyPropertyChanged + RelayCommand
│   │   ├── LoginViewModel.cs
│   │   ├── MainViewModel.cs
│   │   └── EntryEditorViewModel.cs
│   └── Views/
│       ├── LoginWindow.xaml(.cs)
│       ├── MainWindow.xaml(.cs)
│       └── EntryEditorWindow.xaml(.cs)
│
└── VaultApp.Core/                   ← Lógica pura (sem dependência de UI)
    ├── Crypto/
    │   └── CryptoService.cs         ← AES-256-GCM + PBKDF2
    ├── Models/
    │   ├── VaultEntry.cs
    │   └── VaultData.cs
    └── Services/
        ├── StorageService.cs        ← Leitura/escrita do vault.dat
        ├── VaultService.cs          ← CRUD + auto-lock
        ├── ClipboardService.cs      ← Copiar com timer
        └── PasswordGeneratorService.cs
```

---

## Segurança

| Aspecto | Decisão |
|---|---|
| Algoritmo de criptografia | AES-256-GCM (autenticado) |
| Derivação de chave | PBKDF2-SHA256 — 600.000 iterações (OWASP 2023) |
| Salt | 32 bytes aleatórios por arquivo (RandomNumberGenerator) |
| Nonce | 12 bytes aleatórios por gravação |
| Formato do arquivo | `VLT1` + salt + nonce + tag (128-bit) + ciphertext |
| Escrita atômica | Grava em `.tmp`, depois `File.Move` com overwrite |
| Auto-lock | Timer configurável (padrão: 5 min de inatividade) |
| Clipboard | Limpa automaticamente após 30 segundos |
| Senha errada | `CryptographicException` → `UnauthorizedAccessException` (sem leak de info) |

---

## Localização do vault

O arquivo `vault.dat` é salvo em:

```
%LOCALAPPDATA%\VaultApp\vault.dat
```

---

## Roadmap

- [x] V1 — arquivo criptografado local
- [ ] V2 — migração para SQLite (manter `StorageService` como interface)
- [ ] V2 — busca avançada (tags, favoritos)
- [ ] V2 — exportação/importação CSV
- [ ] V3 — múltiplos perfis
