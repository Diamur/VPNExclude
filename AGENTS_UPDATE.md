## Журнал изменений AGENTS (append-only)
2026-04-16 — Каркас Form1 WinForms
Files: App/VPNExclude/Form1.Designer.cs
- Добавлен масштабируемый UI-каркас: верхняя панель действий, таблица правил, блок деталей, лог и StatusStrip.
- Настроены обязательные контролы и параметры формы (заголовок, стартовый/минимальный размер, центрирование).
2026-04-16 — Полировка компоновки Form1 (второй проход)
Files: App/VPNExclude/Form1.Designer.cs
- Выравнены отступы и пропорции верхней панели, таблицы и нижнего блока для более спокойного системного вида.
- Улучшено визуальное оформление dgvRules и сетки полей в "Детали записи", скорректированы привязки при ресайзе.
2026-04-16 — MVP логика данных исключений в Form1
Files: App/VPNExclude/Form1.cs, App/VPNExclude/ExclusionRule.cs, App/VPNExclude/Form1.Designer.cs
- Добавлены загрузка/сохранение JSON, CRUD через UI, выбор записи и обновление таблицы с логом и статусом.
- Реализованы DNS-проверка для Domain и безопасная заглушка для кнопки применения маршрутов.
2026-04-16 — Исправление проверки/сохранения Domain IP
Files: App/VPNExclude/Form1.cs
- Проверка домена теперь логирует полный список IPv4 и сразу обновляет текущую запись/строку таблицы.
- При сохранении Domain с пустым IP добавлен авто-DNS-резолв; без найденных IP запись не сохраняется.
2026-04-16 — MVP применения host-маршрутов Windows
Files: App/VPNExclude/Form1.cs
- Для выбранной записи реализовано применение маршрутов route -p add <IP> mask 255.255.255.255 <Gateway> с проверками прав, шлюза и IP.
- Добавлена проверка существующих маршрутов через route print, подробный лог по каждому IP и итоговый статус (добавлено/уже было/ошибок).
2026-04-16 — Слияние старых и новых IP для Domain
Files: App/VPNExclude/Form1.cs
- Проверка домена теперь объединяет текущие IP и DNS-результат без затирания старых адресов, с подробным логом по старым/новым/добавленным/итогу.
- Автоподстановка IP при сохранении Domain также использует объединение списков и сохраняет объединённый результат в JSON.
2026-04-16 — Вкладка системных маршрутов и сравнение с JSON
Files: App/VPNExclude/Form1.Designer.cs, App/VPNExclude/Form1.cs
- Добавлены вкладки "Записи" и "Системные маршруты", кнопки загрузки/сравнения маршрутов и отчётная таблица маршрутов.
- Добавлен флажок "В системе" в основной таблице записей с пересчётом по системным host-маршрутам после загрузки и сравнения.
2026-04-16 — Исправление парсера route print и colInSystem
Files: App/VPNExclude/Form1.cs
- Парсер route print -4 сделан устойчивее к русской/английской локали, добавлен debug-дамп сырого stdout в route_print_debug.log.
- Исправлен пересчёт признака "В системе" после загрузки/сравнения/применения маршрутов с обновлением таблиц и сводным логом.
2026-04-17 — Переход загрузки системных маршрутов на Get-NetRoute JSON
Files: App/VPNExclude/Form1.cs
- Основной источник системных маршрутов переведён с текстового route print на PowerShell Get-NetRoute + ConvertTo-Json (Active/Persistent + fallback).
- Добавлены debug-дампы raw JSON и сохранён пересчёт colInSystem после загрузки/сравнения/применения.
2026-04-17 — Синхронное удаление записи и host-маршрутов
Files: App/VPNExclude/Form1.cs
- btnDelete теперь удаляет системные host-маршруты записи (с проверкой admin, shared IP и откатом удаления JSON при ошибках).
- Исправлен запуск PowerShell-команд Get-NetRoute через -EncodedCommand, чтобы исключить ошибки экранирования кавычек.
2026-04-18 — Стартовая форма-меню перед Form1
Files: App/VPNExclude/StartForm.cs, App/VPNExclude/StartForm.Designer.cs, App/VPNExclude/Program.cs
- Добавлена новая первичная форма StartForm (500x300, центр, фиксированный размер) с заголовком и 2 кнопками.
- Реализован сценарий открытия Form1 из стартовой формы с возвратом обратно после закрытия Form1.
- Добавлен запуск WireGuard через ProcessStartInfo с проверкой наличия файла и безопасной обработкой ошибок.
2026-04-18 — Импорт и экспорт JSON из верхней панели Form1
Files: App/VPNExclude/Form1.Designer.cs, App/VPNExclude/Form1.cs
- Добавлены кнопки "Загрузить JSON" и "Сохранить JSON" на верхнюю панель справа от текущих действий.
- Реализованы OpenFileDialog/SaveFileDialog, подтверждение перед импортом, замена набора записей с сохранением в основной JSON и обновлением таблицы/лога/статуса.
2026-04-18 — Автопереключение на вкладку "Записи" из верхней панели
Files: App/VPNExclude/Form1.cs
- Добавлен helper SwitchToRecordsTab() и вызов в обработчиках верхних кнопок (Add/Check/Delete/Refresh/Apply/Load JSON/Save JSON).
- Кнопки вкладки "Системные маршруты" оставлены без автопереключения.
2026-04-18 — Инфраструктура Install и сценарий Inno Setup
Files: Install/VPNExclude.Setup.iss, Install/README.md, Install/Docs/INSTALL.md, Install/Prerequisites/README.txt, Install/Output/.gitkeep
- Добавлена папка Install с подпапками Prerequisites/Output/Docs и документацией по сборке установщика.
- Подготовлен .iss сценарий с двумя режимами сборки: framework-dependent и self-contained, плюс шаги для WireGuard и .NET Desktop Runtime 8 x64.
2026-05-04 — Привязка host-route к физическому IF для full-tunnel WG
Files: App/VPNExclude/Form1.cs
- Добавлен авто-выбор физического IPv4-интерфейса (Up + IPv4DefaultGateway) с фильтрацией WireGuard/VPN/TAP/virtual.
- Применение маршрутов переведено на route.exe -p add <ip> mask 255.255.255.255 <gw> metric 1 IF <ifIndex> с предварительным route delete <ip>.
- Усилена проверка маршрута через route print -4 с контролем gateway + interface index и расширен лог выполнения.
2026-05-04 — Фикс удаления маршрутов после удаления DefaultGateway
Files: App/VPNExclude/Form1.cs
- Исправлен CS0103 в BtnDelete_Click: удалена оставшаяся ссылка на DefaultGateway.
- Для старых записей без валидного Gateway удаление host-route теперь делает route delete <ip> (без gateway), с сохранением логики ошибок.
2026-05-04 — Анти-зависание при "Применить маршруты"
Files: App/VPNExclude/Form1.cs
- RunProcess переведён на чтение stdout/stderr через async-задачи с таймаутом 15000 мс.
- При превышении таймаута процесс принудительно завершается, возвращается ошибка с командой, чтобы UI не зависал бесконечно.
2026-05-04 — Добавлены настройки gateway/interface для обхода VPN
Files: App/VPNExclude/Form1.cs, App/VPNExclude/Form1.Designer.cs, App/VPNExclude/SettingsForm.cs
- Добавлена кнопка "Настройки" и диалог с полями Gateway, InterfaceAlias, InterfaceIndex, LocalIPv4 + кнопкой "Автоопределить".
- Настройки сохраняются в vpnexclude.settings.json рядом с vpnexclude.json; при отсутствии файла используется автоопределение.
- Применение маршрутов теперь учитывает source=manual/auto и пишет расширенный лог источника/интерфейса/шлюза.
