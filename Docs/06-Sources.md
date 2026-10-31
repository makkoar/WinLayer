# Источники по виртуализации приложений Windows

Дата обращения: 31 октября 2026 года. Ниже приведены первичные материалы Microsoft и проекта Sandboxie. Предлагаемые правила WinLayer описаны в остальных документах; они не являются заявлением о наличии готовой реализации у Microsoft.

## Аналоги

| Источник | Для чего полезен |
| --- | --- |
| [Sandboxie Sandbox Hierarchy](https://sandboxie-plus.github.io/sandboxie-docs/Content/SandboxHierarchy.html) | Локальные файлы, чтение оригиналов, хранение реестра и пространства IPC |
| [Sandboxie Service Programs](https://sandboxie-plus.github.io/sandboxie-docs/Content/ServicePrograms/) | Обработка служб, COM, фоновых загрузок и MSI внутри песочницы |
| [Исходники Sandboxie](https://github.com/sandboxie-plus/Sandboxie) | Архитектура существующего продукта и лицензии его компонентов |
| [Windows Sandbox](https://learn.microsoft.com/en-us/windows/security/application-security/application-isolation/windows-sandbox/) | Отдельное ядро, временная среда и её жизненный цикл |
| [Настройка Windows Sandbox](https://learn.microsoft.com/en-us/windows/security/application-security/application-isolation/windows-sandbox/windows-sandbox-configure-using-wsb-file) | Общие папки и внешние эффекты разрешённой записи |
| [Ограничения Windows Containers](https://learn.microsoft.com/en-us/virtualization/windowscontainers/quick-start/lift-shift-to-containers) | Отличие серверных контейнеров от среды обычных настольных приложений |
| [Поведение приложений MSIX](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-behind-the-scenes) | Объединённые представления и ограничения области виртуализации |

## Файлы и реестр

| Источник | Для чего полезен |
| --- | --- |
| [File System Filter Drivers](https://learn.microsoft.com/en-us/windows-hardware/drivers/ifs/about-file-system-filter-drivers) | Назначение minifilter и особенности файлового ввода-вывода |
| [Filtering Registry Calls](https://learn.microsoft.com/en-us/windows-hardware/drivers/kernel/filtering-registry-calls) | Отдельный механизм обработки обращений к реестру |
| [CmRegisterCallbackEx](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/nf-wdm-cmregistercallbackex) | Регистрация обработчика и порядок фильтров |
| [RegLoadAppKeyW](https://learn.microsoft.com/en-us/windows/win32/api/winreg/nf-winreg-regloadappkeyw) | Частный куст реестра и ограничения его использования |
| [RegOverridePredefKey](https://learn.microsoft.com/en-us/windows/win32/api/winreg/nf-winreg-regoverridepredefkey) | Переназначение корней в пределах одного процесса |
| [Registry Redirector](https://learn.microsoft.com/en-us/windows/win32/winprog64/registry-redirector) | Раздельные и общие представления реестра при WOW64 |
| [Merged View of HKEY_CLASSES_ROOT](https://learn.microsoft.com/en-us/windows/win32/sysinfo/merged-view-of-hkey-classes-root) | Правила объединения пользовательских и машинных классов |
| [Registry Virtualization](https://learn.microsoft.com/en-us/windows/win32/sysinfo/registry-virtualization) | Почему UAC-виртуализация не является универсальным решением |
| [Windows Projected File System](https://learn.microsoft.com/en-us/windows/win32/projfs/projected-file-system) | Назначение проекции файловых данных |

## Процессы и системная интеграция

| Источник | Для чего полезен |
| --- | --- |
| [Microsoft Detours](https://github.com/microsoft/Detours) | Основа для экспериментального инструментирования API |
| [Job Objects](https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects) | Управление группами процессов и ограничения наследования |
| [Создание процессов](https://learn.microsoft.com/en-us/windows/win32/procthread/creating-processes) | Запуск процессов и управление наследуемыми дескрипторами |
| [Service control manager](https://learn.microsoft.com/en-us/windows/win32/services/service-control-manager) | RPC-интерфейс и общая база служб |
| [Windows Installer и UAC](https://learn.microsoft.com/en-us/windows/win32/msi/using-windows-installer-with-uac) | Установка и обслуживание с повышенными правами |
| [AppContainer isolation](https://learn.microsoft.com/en-us/windows/win32/secauthz/appcontainer-isolation) | Изоляция ресурсов как отдельная задача |
| [Требования подписи драйверов](https://learn.microsoft.com/en-us/windows-hardware/drivers/install/kernel-mode-code-signing-requirements--windows-vista-and-later-) | Условия загрузки и распространения системного компонента |
| [CryptProtectData](https://learn.microsoft.com/en-us/windows/win32/api/dpapi/nf-dpapi-cryptprotectdata) | Ограничения переноса данных, защищённых DPAPI |

## Visual Studio

| Источник | Для чего полезен |
| --- | --- |
| [Выбор расположения установки](https://learn.microsoft.com/en-us/visualstudio/install/change-installation-locations?view=visualstudio) | Компоненты, которые сохраняют размещение на системном диске |
| [Установка Visual Studio](https://learn.microsoft.com/en-us/visualstudio/install/install-visual-studio?view=visualstudio) | Загрузчик, отдельный Installer и административные права |
| [Локальная установка Visual Studio](https://learn.microsoft.com/en-us/visualstudio/install/create-an-offline-installation-of-visual-studio?view=visualstudio) | Наборы пакетов и сохранённые пути источников установки |

## Ярлыки и список программ

| Источник | Для чего полезен |
| --- | --- |
| [Shell Links](https://learn.microsoft.com/en-us/windows/win32/shell/links) | Содержимое ярлыков, цели в пространстве оболочки и разрешение ссылок |
| [Internet Shortcuts](https://learn.microsoft.com/en-us/windows/win32/lwef/internet-shortcuts) | Модель URL-ярлыка и запуск обработчика |
| [SHELLEXECUTEINFO](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/ns-shellapi-shellexecuteinfow) | Активация через оболочку, DDE и отсутствие гарантии нового процесса |
| [Shortcut Table](https://learn.microsoft.com/en-us/windows/win32/msi/shortcut-table) | MSI-ярлыки с проверкой установленных компонентов при запуске |
| [KNOWNFOLDERID](https://learn.microsoft.com/en-us/windows/win32/shell/knownfolderid) | Системные идентификаторы пользовательских и общих папок |
| [GetDiskFreeSpaceExW](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getdiskfreespaceexw) | Сведения о доступном месте для каталога и учёт пользовательских квот |
| [Uninstall Registry Key](https://learn.microsoft.com/en-us/windows/win32/msi/uninstall-registry-key) | Поля представления установленного приложения |
| [Configuring Add/Remove Programs](https://learn.microsoft.com/en-us/windows/win32/msi/configuring-add-remove-programs-with-windows-installer) | Область видимости записи и доступность действий обслуживания |
| [Chromium ProcessSingleton](https://chromium.googlesource.com/chromium/src/%2B/b26c92da0d902cf774bdb7c429beb129f6ec1f4d/chrome/browser/process_singleton.h) | Пример передачи запуска существующему экземпляру, привязанному к профилю; приведена конкретная ревизия |
| [Профили Chromium](https://www.chromium.org/developers/creating-and-using-profiles/) | Раздельные каталоги данных для параллельных экземпляров |
| [Перенос данных Firefox](https://support.mozilla.org/en-US/kb/restore-bookmarks-passwords-and-data-from-an-old-firefox-profile) | Требование закрытия браузера при копировании профиля |
| [Защита cookies Chrome](https://security.googleblog.com/2024/07/improving-security-of-chrome-cookies-on.html) | Привязка защищённых данных к приложению и компьютеру |
| [Device Bound Session Credentials](https://developer.chrome.com/docs/web-platform/device-bound-session-credentials) | Продление сеансов с использованием ключа устройства |
| [SQLite Write-Ahead Logging](https://sqlite.org/wal.html#the_wal_file) | Необходимость согласованного состояния базы и её WAL при копировании |
| [RFC 9700, раздел 4.14.2](https://www.rfc-editor.org/rfc/rfc9700.html#section-4.14.2) | Ротация refresh token и последствия использования старого токена |

## Внешний браузер и специальные ссылки

| Источник | Для чего полезен |
| --- | --- |
| [Обработка URI-активации в Windows](https://learn.microsoft.com/en-us/windows/apps/develop/launch/handle-uri-activation) | URI-схемы, выбор обработчика и различия моделей приложений |
| [URI-активация в .NET](https://learn.microsoft.com/en-us/windows/apps/develop/launch/handle-uri-activation-dotnet) | Регистрация настольного обработчика и доставка активации экземпляру приложения |
| [Приложения по умолчанию в Windows](https://support.microsoft.com/en-us/windows/apps/change-default-apps-in-windows) | Пользовательский выбор приложения для протокола |
| [RFC 8252: OAuth 2.0 for Native Apps](https://www.rfc-editor.org/rfc/rfc8252.html) | Внешний браузер, варианты возврата авторизации и сопоставление ответа с запросом |

## Стек разработки

| Источник | Для чего полезен |
| --- | --- |
| [.NET 10](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview) | Документация выбранной платформы |
| [Avalonia 12.1.4](https://github.com/AvaloniaUI/Avalonia/releases/tag/12.1.4) | Последний стабильный релиз, проверенный 31 октября 2026 года |
| [Пакет Avalonia](https://www.nuget.org/packages/Avalonia/12.1.4) | Опубликованная версия и сведения о целевых платформах .NET |
| [Пакет Avalonia.Desktop](https://www.nuget.org/packages/Avalonia.Desktop/12.1.4) | Согласованный выпуск настольного пакета |
| [Нативное взаимодействие .NET](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/) | Вызов Windows API и подключение нативных компонентов из управляемого кода |
| [Пример драйвера WDK](https://learn.microsoft.com/en-us/windows-hardware/drivers/gettingstarted/writing-a-very-small-kmdf--driver) | Отдельный нативный путь разработки системного компонента, если он потребуется |
| [Single-file deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview) | Публикация для конкретной архитектуры, runtime в поставке и отдельные нативные файлы |
| [WOW64 и x86-приложения](https://learn.microsoft.com/en-us/windows/win32/winprog64/running-32-bit-applications) | Поддержка x86 на x64 и ограничения загрузки DLL разных разрядностей |
| [Поддерживаемые ОС .NET 10](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md) | Различие целевой совместимости проекта и официального сопровождения runtime |
| [Поддерживаемые платформы Avalonia](https://docs.avaloniaui.net/docs/supported-platforms) | Уровни поддержки Windows 10 и Windows 11 |
| [Меню Проводника Windows 11](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/integrate-packaged-app-with-file-explorer) | IExplorerCommand и регистрация команды с идентичностью приложения |
| [Установщики 7-Zip](https://www.7-zip.org/download.html) | Кандидат для простого опыта: EXE/MSI и варианты x86/x64 |

Наличие этих механизмов подтверждает осуществимость отдельных частей архитектуры. Маршрутизация между контейнерами — проектное предложение WinLayer, а не готовая функция Windows. Работоспособность конкретной конфигурации Visual Studio в выбранном движке устанавливается сквозной проверкой установки и обслуживания.
