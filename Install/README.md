# Install infrastructure for VPNExclude

Эта папка содержит заготовку инфраструктуры установщика на базе **Inno Setup**.

## Структура

- `Install/VPNExclude.Setup.iss` — основной сценарий Inno Setup.
- `Install/Prerequisites/` — внешние prerequisite-инсталляторы (WireGuard, .NET Desktop Runtime).
- `Install/Output/` — выходные файлы сборки установщика и промежуточные публикации приложения.
- `Install/Docs/INSTALL.md` — пошаговая инструкция по подготовке и сборке.

## Важный нюанс по .NET

Для WinForms в framework-dependent варианте требуется именно **.NET 8 Windows Desktop Runtime x64**.

Если приложение публикуется как self-contained (`win-x64`), отдельная установка .NET Desktop Runtime не требуется, но инфраструктура prerequisite оставлена в репозитории специально.
