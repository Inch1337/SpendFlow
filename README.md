# SpendFlow

SpendFlow - небольшое веб-приложение для учёта личных расходов, выполненное как тестовое задание на позицию стажёра/junior C# backend developer. Можно добавлять и удалять расходы, просматривать список, фильтровать его по периоду и категории, искать по описанию. Статистика показывает общую сумму и диаграмму расходов по категориям за выбранный период.

## Возможности

### Основная функциональность

- Добавление расхода: описание, сумма, дата и категория.
- Просмотр списка и отдельного расхода, удаление.
- Фильтрация по датам, включая обе границы периода.
- Общая сумма и суммы по категориям за период.
- Проверка входных данных и сообщения об ошибках.
- Сохранение данных в PostgreSQL.

### Дополнительные возможности

- Фильтр по категории и поиск по описанию без учёта регистра.
- Диаграмма по категориям, выбор текущего месяца и сброс фильтров.
- Обновление списка и статистики без перезагрузки страницы.
- Swagger/OpenAPI, Docker Compose, тесты валидации и интеграционные тесты.

## Стек

- C#, .NET 8, ASP.NET Core Web API.
- EF Core 8, Npgsql, PostgreSQL 16.
- HTML/CSS/JavaScript, Fetch API; диаграмма на HTML/CSS.
- Swagger/OpenAPI (Swashbuckle).
- Docker и Docker Compose.
- xUnit, ASP.NET Core `WebApplicationFactory` для HTTP-тестов.

## Структура проекта

```text
SpendFlow/
├── SpendFlow.Api/
│   ├── Controllers/       HTTP endpoints
│   ├── Services/          Расходы, фильтры и статистика
│   ├── DTOs/              Запросы, ответы и валидация
│   ├── Entities/          Expense и категории
│   ├── Data/              AppDbContext
│   ├── Migrations/        Схема PostgreSQL
│   ├── Properties/        Профили локального запуска
│   ├── wwwroot/           Статический frontend
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Dockerfile
│   └── SpendFlow.Api.csproj
├── SpendFlow.Tests/       Тесты DTO, сервиса и HTTP API
├── docker-compose.yml
├── docker-compose.tests.yml
└── SpendFlow.slnx
```

Запрос проходит через **Controller -> ExpenseService -> AppDbContext -> PostgreSQL**. Контроллер принимает DTO, сервис выполняет операцию, EF Core обращается к БД. Frontend отдаётся тем же ASP.NET Core приложением; отдельный сервер и Node.js не нужны.

## Быстрый запуск

Рекомендуемый способ проверить проект - **Docker Compose**: он поднимает API и PostgreSQL. В текущей конфигурации миграции выполняются вручную с компьютера, поэтому для первого запуска нужны также .NET SDK и `dotnet-ef`.

1. Установите Git.
2. Установите и запустите Docker Desktop для Windows/macOS (Linux-контейнеры). На Linux нужны Docker Engine и Docker Compose Plugin.
3. Установите **.NET SDK 8.0**: он включает среды выполнения .NET 8 и ASP.NET Core 8. Одного Runtime недостаточно для команд ниже.
4. Откройте PowerShell или терминал Linux/macOS. Проверьте инструменты:

   ```sh
   git --version
   docker version
   docker compose version
   dotnet --list-sdks
   ```

5. Клонируйте проект и установите EF CLI, если он ещё не установлен:

   ```sh
   git clone https://github.com/Inch1337/SpendFlow.git
   cd SpendFlow
   dotnet tool install --global dotnet-ef --version 8.0.31
   dotnet ef --version
   ```

6. Из корня репозитория выполните по порядку; переходите к следующей команде после успешного завершения предыдущей:

   ```sh
   dotnet restore SpendFlow.Api/SpendFlow.Api.csproj
   docker compose up -d --wait postgres
   dotnet ef database update --project SpendFlow.Api/SpendFlow.Api.csproj
   docker compose up -d --build
   docker compose ps
   ```

   Порты `5432` и `8080` должны быть свободны. Миграция использует локальные демонстрационные настройки из `appsettings.json`; если вы ранее задали `ConnectionStrings__DefaultConnection`, проверьте, что переменная указывает на нужную БД.

7. Откройте:

   | Что | Адрес |
   | --- | --- |
   | Frontend | [http://localhost:8080/](http://localhost:8080/) |
   | Swagger | [http://localhost:8080/swagger](http://localhost:8080/swagger) |
   | API расходов | [http://localhost:8080/api/expenses](http://localhost:8080/api/expenses) |

Все дальнейшие команды проекта также выполняются из корня `SpendFlow`, если явно не указано другое. Команды `git`, `dotnet` и `docker` подходят для PowerShell и Bash; присваивание переменных окружения показано отдельно для каждой оболочки.

## Вариант 1 - запуск через Docker Compose

Для первого запуска выполните раздел «Быстрый запуск», включая установку инструментов и применение миграций. PostgreSQL отдельно устанавливать на компьютер не нужно.

| Сервис Compose | Назначение | Порт на компьютере -> в контейнере |
| --- | --- | --- |
| `api` | ASP.NET Core API, frontend и Swagger | `8080` -> `8080` |
| `postgres` | PostgreSQL 16, контейнер `spendflow-postgres` | `5432` -> `5432` |

База `spendflow` и пользователь `postgres` создаются образом PostgreSQL при первом запуске с пустым volume. Таблицы создаёт команда `dotnet ef database update`. API **не применяет миграции автоматически**; проверка здоровья PostgreSQL проверяет доступность сервера, а не наличие таблиц. В контейнере API установлен Runtime, поэтому запускать в нём `dotnet ef` нельзя.

После первого запуска, если новых миграций нет:

```sh
docker compose up -d --build
```

Состояние и логи:

```sh
docker compose ps
docker compose logs --tail=100 postgres
docker compose logs -f api
```

`Ctrl+C` завершает просмотр логов, контейнеры продолжают работать.

Остановить контейнеры, сохранив их:

```sh
docker compose stop
```

Остановить и удалить контейнеры и сеть проекта:

```sh
docker compose down
```

Обе команды сохраняют рабочую БД. Данные находятся в именованном Docker volume с ключом `spendflow_pgdata`, смонтированном в `/var/lib/postgresql/data`. При стандартном имени проекта `spendflow` фактическое имя volume - `spendflow_spendflow_pgdata`; префикс зависит от имени Compose-проекта. Данные хранятся у Docker, а не в каталоге репозитория. Посмотреть volume и его расположение:

```sh
docker volume ls --filter label=com.docker.compose.volume=spendflow_pgdata
docker volume inspect spendflow_spendflow_pgdata
```

Для нестандартного имени проекта подставьте имя из первой команды. В Docker Desktop расположение относится к Linux-среде Docker.

Только если требуется полностью сбросить рабочую БД:

> **ЭТА КОМАНДА УДАЛИТ ДАННЫЕ БАЗЫ.** После неё при следующем запуске нужно заново применить миграции.

```sh
docker compose down -v
```

## Вариант 2 - локальная разработка

Нужны Git, **.NET SDK 8.0**, `dotnet-ef` **8.0.31** и Docker для PostgreSQL. API запускается на компьютере, БД - в контейнере. Вместо контейнера можно использовать установленный PostgreSQL 16, подготовив БД по следующему разделу.

Если репозиторий ещё не склонирован:

```sh
git clone https://github.com/Inch1337/SpendFlow.git
cd SpendFlow
```

Установите `dotnet-ef`, как в быстром запуске, затем выполните:

```sh
dotnet restore SpendFlow.Api/SpendFlow.Api.csproj
docker compose up -d --wait postgres
dotnet ef database update --project SpendFlow.Api/SpendFlow.Api.csproj
dotnet run --project SpendFlow.Api/SpendFlow.Api.csproj --launch-profile http
```

Последняя команда работает в текущем терминале. Остановка API - `Ctrl+C`; остановка БД - `docker compose stop postgres` в другом терминале.

- Frontend: [http://localhost:5110/](http://localhost:5110/).
- Swagger: [http://localhost:5110/swagger](http://localhost:5110/swagger).

Профиль `http` задаёт порт `5110` и окружение `Development`. Профиль `https` дополнительно использует `https://localhost:7177`; для приведённых HTTP-команд сертификат не нужен.

### .NET 8 и формат решения .slnx

Приложение и тесты нацелены на **`net8.0`**. Все основные команды здесь используют `.csproj` и работают с SDK 8. Формат решения `SpendFlow.slnx` требует **SDK 9.0.200 или новее** ([поддержка SLNX в .NET CLI](https://devblogs.microsoft.com/dotnet/introducing-slnx-support-dotnet-cli/)); версия SDK не меняет целевую платформу приложения.

Если установлен более новый SDK, для запуска приложения, тестов и EF CLI всё равно нужны среды выполнения .NET 8 и ASP.NET Core 8; проще оставить SDK 8 установленным рядом. Сборка всего решения при наличии подходящего SDK:

```sh
dotnet build SpendFlow.slnx
```

Сборка API с SDK 8:

```sh
dotnet build SpendFlow.Api/SpendFlow.Api.csproj
```

## База данных и migrations

EF Core migrations создают и обновляют схему PostgreSQL. В проекте две миграции:

- `20260915115903_InitialCreate` - таблица `Expenses`.
- `20260915132027_UseDateOnlyAndUtcTimestamps` - перевод даты расхода в тип `date` с сохранением календарного дня UTC.

Для БД из основного Compose достаточно `docker compose up -d --wait postgres`: база `spendflow` будет создана при первой инициализации. После первого клонирования примените миграции; повторяйте обновление после получения новых миграций:

```sh
dotnet ef migrations list --project SpendFlow.Api/SpendFlow.Api.csproj
dotnet ef database update --project SpendFlow.Api/SpendFlow.Api.csproj
```

Первая команда показывает миграции и, при доступной БД, какие ещё не применены. Посмотреть только список из кода без подключения:

```sh
dotnet ef migrations list --project SpendFlow.Api/SpendFlow.Api.csproj --no-connect
```

При обычном повторном запуске обновление схемы не требуется. Повторная команда `database update` применяет только отсутствующие миграции. Создавать новую миграцию командой `migrations add` для запуска проекта не нужно.

### Если PostgreSQL установлен отдельно

Установите и запустите PostgreSQL 16 с клиентом `psql`. Войдите под администратором (ниже - локальная установка с администратором `postgres`):

```sh
psql -h localhost -U postgres -d postgres
```

Создайте БД `spendflow`, если её ещё нет. Для локальной проверки используем существующего пользователя `postgres`, как в настройках проекта:

```sql
CREATE DATABASE spendflow OWNER postgres;
\q
```

Перед миграцией и запуском API задайте подключение в том же терминале, заменив `<your-password>` паролем пользователя `postgres` из вашей установки.

PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection = 'Host=localhost;Port=5432;Database=spendflow;Username=postgres;Password=<your-password>'
```

Bash (Linux/macOS):

```bash
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=spendflow;Username=postgres;Password=<your-password>'
```

Затем выполните `dotnet ef database update --project SpendFlow.Api/SpendFlow.Api.csproj` и команду локального запуска. Не поднимайте одновременно контейнер PostgreSQL и отдельный сервер на одном порту `5432`. Не сохраняйте реальные пароли в Git; примеры с паролем в командной строке также могут попасть в историю терминала.

## Развёртывание на VPS

Ниже - запуск для демонстрации на **чистом Ubuntu 24.04 LTS** с пользователем, имеющим `sudo`. API и PostgreSQL работают в Docker; .NET SDK на сервере нужен для ручных миграций. Для Debian используйте инструкции установки Docker и .NET именно для вашей версии ОС; команды запуска проекта останутся теми же.

### 1. Подключитесь и установите инструменты

На своём компьютере:

```sh
ssh <user>@<server-ip>
```

На сервере установите Git и SDK 8 из репозиториев Ubuntu 24.04 ([установка .NET на Ubuntu](https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-install)):

```bash
sudo apt-get update
sudo apt-get install -y git ca-certificates curl dotnet-sdk-8.0
```

Установите **Docker Engine и Docker Compose Plugin** по официальному разделу **Install using the apt repository** для [Ubuntu](https://docs.docker.com/engine/install/ubuntu/#install-using-the-repository) или [Debian](https://docs.docker.com/engine/install/debian/#install-using-the-repository). Нужно сначала добавить репозиторий Docker, затем установить пакеты Engine и Compose Plugin; установка только команды Compose недостаточна.

После установки проверьте:

```bash
sudo systemctl start docker
sudo docker version
sudo docker compose version
dotnet --list-sdks
dotnet tool install --global dotnet-ef --version 8.0.31
export PATH="$PATH:$HOME/.dotnet/tools"
dotnet ef --version
```

В этом разделе Docker-команды выполняются через `sudo`; `git` и `dotnet` - от обычного пользователя. Если EF CLI уже установлен нужной версии, повторную установку пропустите.

### 2. Склонируйте проект и проверьте настройки

```bash
git clone https://github.com/Inch1337/SpendFlow.git
cd SpendFlow
sudo docker compose config --quiet
```

Текущий Compose задаёт демонстрационные реквизиты БД прямо в YAML. Для запуска без изменения конфигурации дополнительные переменные окружения **не требуются**. Создание `.env` или `export POSTGRES_PASSWORD=...` не заменит эти значения: в Compose нет подстановок `${...}` и `env_file`.

Перед запуском ограничьте входящий доступ к `5432` средствами сетевого экрана провайдера VPS. Порт `8080` разрешите только тем, кто будет проверять приложение. Публикация Docker-портов может обходить обычные правила UFW; учитывайте [ограничения Docker и firewall](https://docs.docker.com/engine/install/ubuntu/#firewall-limitations).

### 3. Поднимите БД, примените миграции и запустите API

```bash
dotnet restore SpendFlow.Api/SpendFlow.Api.csproj
sudo docker compose up -d --wait postgres
dotnet ef database update --project SpendFlow.Api/SpendFlow.Api.csproj
sudo docker compose up -d --build
sudo docker compose ps
sudo docker compose logs --tail=100 postgres api
```

Миграции выполняются с VPS через `localhost:5432`, а контейнер API подключается к `postgres:5432`. Как и при локальном запуске, проверьте, что ранее заданная `ConnectionStrings__DefaultConnection` не перенаправляет EF CLI в другую БД.

### 4. Проверьте доступ и остановку

Сначала на сервере:

```bash
curl --fail http://localhost:8080/api/expenses
curl --fail --output /dev/null http://localhost:8080/swagger/index.html
```

В новой БД список расходов вернёт `[]`. В браузере своего компьютера откройте `http://<server-ip>:8080/` и `http://<server-ip>:8080/swagger`, подставив IP VPS. Для домена с настроенной DNS-записью используются те же пути и порт.

Остановить и удалить контейнеры, сохранив БД:

```bash
sudo docker compose down
```

Повторный запуск - `sudo docker compose up -d --build`. При появлении новых миграций сначала поднимите PostgreSQL и повторите `dotnet ef database update`. Автоматический запуск контейнеров после перезагрузки VPS в текущем Compose не настроен; после перезагрузки запустите их этой командой вручную.

### Важно для VPS

- Это конфигурация для демонстрации: приложение доступно по HTTP, авторизации нет, Swagger включён и в `Production`.
- Не храните реальные пароли в Git. Для публичного развёртывания нужна отдельная настройка передачи секретов в Compose и согласованная строка подключения API; текущий YAML этого через `.env` не поддерживает. Сам ASP.NET Core поддерживает `ConnectionStrings__DefaultConnection`.
- PostgreSQL сейчас опубликован на всех интерфейсах хоста (`5432:5432`). Не оставляйте его доступным из интернета; для связи контейнеров публикация порта не нужна, но приведённые ручные миграции используют его с хоста.
- Для публичного production-развёртывания рекомендуется поставить reverse proxy с HTTPS перед приложением; настройка reverse proxy выходит за рамки тестового задания.
- Изменение `POSTGRES_PASSWORD` не меняет пароль в уже инициализированном volume: пароль существующей роли нужно менять в самой PostgreSQL. Не удаляйте рабочие данные ради смены реквизитов.

## Конфигурация

API читает `SpendFlow.Api/appsettings.json`, затем файл для выбранного окружения и переменные окружения. `appsettings.Development.json` меняет только логирование. Переменная окружения с двойным подчёркиванием переопределяет соответствующий вложенный параметр JSON.

Все пароли ниже - демонстрационные значения из репозитория, не реальные серверные секреты.

| Параметр / источник | Назначение | Текущее значение |
| --- | --- | --- |
| `ConnectionStrings:DefaultConnection` в `appsettings.json` | Подключение при локальном запуске и миграциях | `Host=localhost;Port=5432;Database=spendflow;Username=postgres;Password=postgres` |
| `ConnectionStrings__DefaultConnection` у `api` в Compose | Подключение API внутри Docker-сети | `Host=postgres;Port=5432;Database=spendflow;Username=postgres;Password=postgres` |
| `ASPNETCORE_ENVIRONMENT` | Окружение ASP.NET Core | `Development` в локальных профилях, `Production` в Compose |
| `ASPNETCORE_HTTP_PORTS` | HTTP-порт внутри контейнера API | `8080` в Dockerfile и Compose |
| `ports` у `api` | Публикация контейнера на хосте | `8080:8080` |
| `applicationUrl` в `launchSettings.json` | Локальные профили `http` / `https` | `http://localhost:5110` / дополнительно `https://localhost:7177` |
| `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` у `postgres` | Начальная настройка PostgreSQL с пустым volume | `spendflow`, `postgres`, `postgres` |
| `ports` у `postgres` | Доступ к БД с хоста | `5432:5432` |

Для своего PostgreSQL используйте пример переменной окружения из раздела о БД. `localhost` подходит для API/EF CLI на хосте; внутри контейнера API он означал бы сам контейнер, поэтому там используется имя сервиса `postgres`.

Порты и реквизиты Compose сейчас заданы явно. Переменные оболочки хоста не заменяют их автоматически, а `.env` не загружается самим ASP.NET Core. Изменение внутреннего порта API также требует согласовать публикацию порта Docker. Профили `launchSettings.json` действуют при локальном `dotnet run`, но не при запуске контейнера.

## API

| Метод | Путь | Назначение | Успешный ответ |
| --- | --- | --- | --- |
| `GET` | `/api/expenses` | Список с фильтрами | `200` |
| `GET` | `/api/expenses/{id}` | Расход по ID | `200` |
| `POST` | `/api/expenses` | Добавление | `201` и `Location` |
| `DELETE` | `/api/expenses/{id}` | Удаление | `204` |
| `GET` | `/api/expenses/summary` | Статистика за период | `200` |

Пример фильтра:

```http
GET /api/expenses?dateFrom=2026-01-01&dateTo=2026-01-31&category=Food&search=coffee
```

Все фильтры необязательны и применяются совместно. Список отсортирован по дате, затем по ID, от новых к старым. Поиск проверяет вхождение в описание без учёта регистра, обрезает пробелы по краям; `%`, `_` и `\` трактуются буквально.

Пример тела `POST /api/expenses` с `Content-Type: application/json`:

```json
{
  "description": "Кофе",
  "amount": 150.50,
  "date": "2026-01-15",
  "category": "Food"
}
```

Категории: `Food`, `Transport`, `Housing`, `Entertainment`, `Health`, `Other`. В JSON они передаются строками. Описание обязательно, до 500 символов; сумма - от `0.01` до `999999999.99`, без дробной части меньше копейки. Дата обязательна, формат `yyyy-MM-dd`; будущие даты допустимы. Обратный период, неверная категория и другие ошибки ввода возвращают `400`, отсутствующий расход - `404`.

Полная интерактивная документация доступна в Swagger по адресам из разделов запуска. Готовые запросы также есть в `SpendFlow.Api/SpendFlow.Api.http`: используйте `http://localhost:5110` для локального API или `http://localhost:8080` для Docker.

## Как работает статистика

```http
GET /api/expenses/summary?dateFrom=2026-01-01&dateTo=2026-01-31
```

- Учитываются обе границы периода. Можно указать одну дату или не указывать даты вовсе - тогда статистика считается за всё время.
- EF Core выполняет группировку и суммирование по категориям в PostgreSQL. Сервис добавляет отсутствующие категории с нулевой суммой и складывает результаты в общий итог.
- Ответ содержит `totalAmount` и `byCategory` с полями `category` и `totalAmount`. Всегда возвращаются все шесть категорий; для пустого периода все суммы равны нулю.
- **Категория и поиск влияют только на список. Статистика и диаграмма учитывают только выбранные даты.**
- Frontend строит диаграмму из `byCategory`. На сервере суммы считаются через `decimal`; в браузере используется JavaScript `Number`, поэтому точность копеек для произвольно больших итогов не гарантируется.

## Тесты

Для тестов нужны .NET SDK 8.0 и зависимости проекта. Из корня репозитория:

```sh
dotnet restore SpendFlow.Tests/SpendFlow.Tests.csproj
```

### Без базы данных

Проверяются DTO: обязательные поля, описание, диапазон и точность суммы, категории и границы периода. Docker и PostgreSQL для этого набора не нужны:

```sh
dotnet test SpendFlow.Tests/SpendFlow.Tests.csproj --filter "Category!=Integration"
```

### Полный набор и интеграционные тесты

**До запуска полного набора требуется отдельная PostgreSQL из `docker-compose.tests.yml`.** Рабочую БД использовать не нужно. Запустите Docker и выполните:

```sh
docker compose -f docker-compose.tests.yml up -d --wait
dotnet test SpendFlow.Tests/SpendFlow.Tests.csproj
```

Только интеграционные тесты:

```sh
dotnet test SpendFlow.Tests/SpendFlow.Tests.csproj --filter "Category=Integration"
```

Тестовый Compose использует проект `spendflow-tests`, сервис `postgres-tests`, порт `127.0.0.1:55432` и БД `spendflow_tests`. Пользователь и демонстрационный пароль - `spendflow_tests`. Подключение задано в `PostgresFixture.cs`, переменная подключения основного API его не меняет.

Контейнер создаёт тестовую БД, fixture автоматически применяет миграции. Тесты сервиса проверяют фильтры, поиск, сортировку и статистику; данные создаются в транзакциях и откатываются. HTTP-тесты запускают приложение через `WebApplicationFactory`, проверяют создание, чтение, удаление и `404`, затем удаляют свои записи. Отдельно запускать API не нужно.

HTTP-тесты не выполняются параллельно с другими коллекциями. Не запускайте несколько процессов интеграционных тестов одновременно против этой БД. Без доступной PostgreSQL тесты завершатся ошибкой, а не будут пропущены.

После тестов:

```sh
docker compose -f docker-compose.tests.yml down
```

Тестовые данные находятся в `tmpfs` и исчезают при остановке контейнера; volume рабочей БД не используется. Frontend проверяется вручную: добавьте расходы, проверьте фильтры, диаграмму и удаление, затем обновите страницу и проверьте сохранность данных.

## Краткое описание принятых решений

### Почему выбрана такая структура

Для небольшого трекера достаточно одного API-проекта с разделением по ответственности: контроллеры работают с HTTP, `ExpenseService` содержит операции с расходами, а `AppDbContext` обращается к PostgreSQL через EF Core. Цепочка `Controller -> Service -> DbContext` позволяет проверять логику сервиса отдельно от HTTP; тесты вынесены в `SpendFlow.Tests`. Отдельные Repository и Unit of Work не добавлены: для текущих операций достаточно возможностей EF Core.

DTO отделены от сущностей, чтобы задавать формат запросов, ответов и валидацию независимо от модели хранения. Frontend лежит в `wwwroot` и отдаётся самим ASP.NET Core, поэтому для запуска не нужны отдельный frontend-сервер и сборка JavaScript.

### Как считается статистика

Сервис отбирает расходы по датам с включёнными границами периода, затем EF Core выполняет `GroupBy` и `Sum` в PostgreSQL. В приложение загружаются суммы по категориям; сервис добавляет нулевые значения для отсутствующих категорий и складывает суммы в `totalAmount`. Это позволяет считать итог без загрузки всех расходов в память. Категории заданы enum; фильтр категории и поиск относятся только к списку и не влияют на статистику.

Для денежных сумм используется `decimal`, для даты расхода - `DateOnly` / PostgreSQL `date`, чтобы календарный день не зависел от часового пояса. Время создания хранится отдельно в UTC. Интеграционные тесты проверяют расчёты на отдельной PostgreSQL, включая границы периода и пустой результат.

### Что не успели сделать

Основная функциональность учёта расходов реализована. Из возможных дополнительных улучшений отсутствуют:

- Редактирование расходов (`PUT`/`PATCH`).
- Пагинация списка.
- Импорт и экспорт CSV.
- Специальная обработка конфликтов одновременного изменения данных.

Также нет авторизации и разделения расходов по пользователям, управления категориями и автоматизированных frontend-тестов. Это один общий набор расходов, а не многопользовательский сервис.

## Troubleshooting

### 1. Docker недоступен или команда compose не найдена

На Windows/macOS запустите Docker Desktop и дождитесь готовности Linux Engine. На Linux проверьте службу и наличие Compose Plugin. `docker version` должен показывать и Client, и Server; используйте `docker compose`, а не старую команду `docker-compose`.

### 2. Port already in use

Основному Compose нужны свободные `8080` и `5432`, локальному API - `5110`, тестовой БД - `55432`. Посмотрите занятые контейнерами порты:

```sh
docker ps --format "table {{.Names}}\t{{.Ports}}"
```

Остановите известное вам конфликтующее приложение. Частая причина - уже установленный PostgreSQL на `5432`. Другой порт требует согласованного изменения публикации и строки подключения; произвольная переменная `PORT` в проекте не предусмотрена.

### 3. PostgreSQL container is not healthy

```sh
docker compose ps
docker compose logs --tail=100 postgres
```

Проверьте сообщения об инициализации, доступное место и ошибки volume. Для тестовой БД используйте `docker compose -f docker-compose.tests.yml logs --tail=100 postgres-tests`. Не удаляйте volume рабочей БД для устранения ошибки без сохранения нужных данных.

### 4. Migration cannot connect to PostgreSQL

Дождитесь завершения `docker compose up -d --wait postgres`. EF CLI на хосте должен подключаться к `localhost:5432`, контейнер API - к `postgres:5432`. Проверьте `ConnectionStrings__DefaultConnection`, имя БД и пароль. Изменение реквизитов контейнера не обновляет уже существующего пользователя PostgreSQL.

### 5. dotnet ef не найден или просит другую среду выполнения

Установите SDK 8 и EF CLI:

```sh
dotnet tool install --global dotnet-ef --version 8.0.31
```

Если инструмент уже установлен другой версии, используйте:

```sh
dotnet tool update --global dotnet-ef --version 8.0.31 --allow-downgrade
```

Перезапустите терминал и проверьте `dotnet ef --version`. На Linux/macOS каталог `$HOME/.dotnet/tools` должен быть в `PATH`; для текущего Bash-сеанса: `export PATH="$PATH:$HOME/.dotnet/tools"`.

### 6. Интерфейс открывается, но таблицы Expenses нет

Статические файлы и Swagger могут работать даже без готовой БД. Примените схему к нужной БД:

```sh
dotnet ef database update --project SpendFlow.Api/SpendFlow.Api.csproj
```

В HTTP-профиле и Docker возможно предупреждение `Failed to determine the https port for redirect`: в коде включён `UseHttpsRedirection`, но HTTPS-порт для этих запусков не настроен. Используйте указанные HTTP-адреса; само предупреждение не означает ошибку подключения к БД.

### 7. Изменения не попали в Docker-образ

Пересоберите только API без кеша и пересоздайте его контейнер:

```sh
docker compose build --no-cache api
docker compose up -d api
```

Данные PostgreSQL при этом сохраняются.

### 8. Ошибка .slnx или NuGet

Если SDK 8 не распознаёт `SpendFlow.slnx`, используйте команды с `.csproj` из этого README либо установите SDK с поддержкой `.slnx`. `NU1900` означает, что NuGet не смог получить данные об уязвимостях пакетов: проверьте доступ к `https://api.nuget.org/v3/index.json` и повторите `dotnet restore`. О результате тестов судите по отдельной итоговой сводке запуска.
