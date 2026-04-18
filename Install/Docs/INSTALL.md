# Сборка и запуск установщика VPNExclude

## 1) Подготовка prerequisite-файлов

Скачайте и положите в `Install/Prerequisites`:

1. `wireguard-installer-x64.exe`
2. `windowsdesktop-runtime-8.0.x-win-x64.exe`

> Для WinForms нужен именно **Windows Desktop Runtime 8 x64**.

## 2) Подготовка publish приложения

Из корня репозитория:

### Вариант A: framework-dependent

```bash
dotnet publish App/VPNExclude/VPNExclude.csproj -c Release -f net8.0-windows -o Install/Output/App/framework-dependent
```

### Вариант B: self-contained (win-x64)

```bash
dotnet publish App/VPNExclude/VPNExclude.csproj -c Release -f net8.0-windows -r win-x64 --self-contained true -o Install/Output/App/self-contained-win-x64
```

## 3) Сборка Inno Setup

### Framework-dependent (с шагом .NET Desktop Runtime)

```bash
iscc /DBuildFlavor=framework Install\VPNExclude.Setup.iss
```

### Self-contained (без обязательного шага .NET Desktop Runtime)

```bash
iscc /DBuildFlavor=selfcontained Install\VPNExclude.Setup.iss
```

## 4) Что делает установщик

- Устанавливает VPNExclude в `Program Files`.
- Создаёт ярлык в меню Пуск и (опционально) на рабочем столе.
- Создаёт локальную папку данных `%LOCALAPPDATA%\VPNExclude`.
- Может запустить WireGuard installer, если файл присутствует в prerequisites.
- В framework-dependent варианте может запустить .NET Desktop Runtime installer.

## 5) По silent-ключам prerequisites

В текущей заготовке prerequisite-инсталляторы запускаются интерактивно.

Если позже потребуется полностью silent-сценарий, сначала подтвердите актуальные тихие аргументы у официальной документации WireGuard/.NET, и только затем обновляйте `.iss`.
