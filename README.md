# SpendFlow

API для учёта личных расходов: ASP.NET Core 8, EF Core 8 и PostgreSQL 16.

## Запуск через Docker Compose

Нужен Docker с поддержкой Linux-контейнеров и Docker Compose.
Команды выполняются из корня репозитория. Порты 5432 и 8080 должны быть свободны.

### Первый запуск и применение новых миграций

Миграции применяются вручную с хоста. Для этого нужны .NET SDK 8 или новее
и инструмент `dotnet-ef` версии 8.0.31. Если инструмент ещё не установлен:

```powershell
dotnet tool install --global dotnet-ef --version 8.0.31
```

Запустите БД, дождитесь её готовности и примените миграции:

```powershell
docker compose up -d --wait postgres
dotnet ef database update --project SpendFlow.Api --connection "Host=localhost;Port=5432;Database=spendflow;Username=postgres;Password=postgres"
docker compose up --build -d
```

Healthcheck проверяет готовность PostgreSQL принимать подключения, но не наличие таблиц.
API самостоятельно миграции не применяет.

### Последующие запуски

Если все миграции уже применены:

```powershell
docker compose up --build -d
```

- Swagger: [http://localhost:8080/swagger](http://localhost:8080/swagger).
- API: [http://localhost:8080/api/expenses](http://localhost:8080/api/expenses).
- Для запросов из `SpendFlow.Api/SpendFlow.Api.http` установите
  `SpendFlow.Api_HostAddress = http://localhost:8080`.

В контейнере API подключается к БД по имени сервиса `postgres`,
а команды миграции с хоста — через `localhost:5432`.
Этот Compose предназначен для локального запуска: HTTP и демонстрационные реквизиты БД.

### Логи и остановка

```powershell
docker compose logs -f api
docker compose down
```

Данные сохраняются в существующем volume `spendflow_pgdata`.
Команда `docker compose down -v` удаляет этот volume вместе с данными.

## Тесты

Обычные тесты валидации не требуют БД:

```powershell
dotnet test SpendFlow.slnx --filter "Category!=Integration"
```

Интеграционные тесты вызывают методы `ExpenseService` с настоящей PostgreSQL 16.
Они проверяют суммы, периоды, фильтры, поиск и сортировку. API запускать не нужно.
Отдельный Compose-проект `spendflow-tests` использует порт `55432` и БД
`spendflow_tests`; рабочая БД `spendflow` и её volume не используются.

```powershell
docker compose -f docker-compose.tests.yml up -d --wait
dotnet test SpendFlow.slnx --filter "Category=Integration"
```

Для запуска всех тестов с работающей тестовой БД:

```powershell
dotnet test SpendFlow.slnx
```

Fixture применяет существующие миграции один раз перед тестами класса.
Каждый тест создаёт данные в своей транзакции и откатывает её после проверки.
Тесты внутри класса выполняются последовательно; не запускайте несколько
процессов интеграционных тестов одновременно против этого контейнера.
Без доступной тестовой БД интеграционные тесты завершатся ошибкой, а не пропуском.

После проверки:

```powershell
docker compose -f docker-compose.tests.yml down
```

Тестовая БД хранится в `tmpfs`: её данные исчезают при остановке контейнера.
