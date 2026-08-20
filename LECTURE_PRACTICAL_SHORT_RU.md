# Короткий сценарий практической части

Во всех примерах использую один порядок:

1. какой компонент или flow сейчас рассматриваем;
2. что важно показать в production-коде;
3. что важно показать в fixture;
4. что важно показать в тестах;
5. как запустить пример.

---

## 1. SchedulingEngine — component tests без внешней инфраструктуры

### Компонент и граница

Открываю `src/Engines/SchedulingEngine/Program.cs`.

Говорю:

> SchedulingEngine отвечает за две операции: проверку пересечения сессий и
> проверку свободных мест. У него нет базы данных, очереди и downstream-сервисов.
> Поэтому граница этого component test — весь HTTP-сервис, а подменять внешние
> зависимости здесь не нужно.

Показываю:

- `/scheduling/check-conflict`;
- `/scheduling/check-capacity`;
- `public partial class Program` в конце файла.

Коротко объясняю:

> `public partial class Program` делает entry point доступным тестовому проекту.
> Production-поведение от этого не меняется.

### Fixture

Открываю `.csproj` тестового проекта и показываю:

```xml
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" ... />
```

Говорю:

> Это NuGet-пакет, который предоставляет `WebApplicationFactory`.
> `WebApplicationFactory<Program>` запускает настоящий ASP.NET Core pipeline
> внутри test process. Реальный TCP-порт и Docker здесь не нужны.

В этом примере отдельного класса fixture нет. Сам
`WebApplicationFactory<Program>` передаётся тестовому классу как xUnit fixture:

```csharp
IClassFixture<WebApplicationFactory<Program>>
```

Через `factory.CreateClient()` получаем `HttpClient` и отправляем обычные HTTP
запросы к приложению.

### Тесты

Открываю `SchedulingEngineComponentTests.cs`.

Показываю `Theory` для проверки конфликтов:

> Здесь Arrange, Act и Assert одинаковые. Меняются только интервалы, комнаты и
> ожидаемый результат. Поэтому несколько случаев удобно представить как
> `Theory`, а не копировать одинаковые `Fact`.

Затем показываю отдельные `Fact`:

- неправильный временной диапазон возвращает `400`;
- capacity endpoint возвращает количество свободных мест;
- отрицательная вместимость возвращает validation problem.

Говорю:

> Тест входит через HTTP, поэтому проверяется не только алгоритм, но и routing,
> model binding, validation и JSON contract.

### Запуск

```bash
dotnet test \
  MeetingFlow.Microservices/tests/MeetingFlow.SchedulingEngine.ComponentTests/MeetingFlow.SchedulingEngine.ComponentTests.csproj
```

---

## 2. DataAccessor — component tests с PostgreSQL Testcontainer

### Компонент и граница

Открываю `src/Accessors/DataAccessor/Program.cs`.

Говорю:

> DataAccessor отвечает за HTTP API доступа к данным, EF Core queries, mapping
> и сохранение в PostgreSQL. Если заменить PostgreSQL in-memory коллекцией, мы
> не проверим важную ответственность компонента. Поэтому DataAccessor запускаем
> через `WebApplicationFactory`, а PostgreSQL оставляем настоящим.

### Fixture

Открываю `DataAccessorFixture.cs` и `.csproj`.

Показываю три пакета:

```xml
Microsoft.AspNetCore.Mvc.Testing
Testcontainers.PostgreSql
Respawn
```

Говорю:

> `WebApplicationFactory` запускает DataAccessor. Testcontainers создаёт
> одноразовый контейнер из настоящего образа `postgres:16`. Respawn очищает
> данные между тестами.

Показываю создание контейнера и подмену connection string:

```csharp
new PostgreSqlBuilder("postgres:16").Build();
builder.UseSetting("POSTGRES_CONN", _postgres.GetConnectionString());
```

Коротко объясняю lifecycle:

> Fixture создаётся один раз на test class, поэтому контейнер запускается один
> раз для всего класса. Перед каждым тестом `ResetDatabaseAsync()` удаляет строки
> из application tables. Схемы и таблицы не пересоздаются.

Показываю:

```csharp
public Task InitializeAsync() => fixture.ResetDatabaseAsync();
```

Далее показываю `SeedAsync<TEntity>`:

> После очистки каждый тест создаёт собственные данные. Production seed может
> существовать, но тесты от него не зависят. `params` позволяет передать одну или
> несколько сущностей одного типа. Если передаётся корневая EF entity со
> связанными navigation properties, EF сохраняет весь граф.

Во время запуска открываю Docker Desktop:

> Здесь видны PostgreSQL и служебный контейнер Ryuk. Ryuk не является частью
> MeetingFlow — Testcontainers использует его для очистки Docker-ресурсов.

### Тесты

Открываю `DataAccessorComponentTests.cs`.

Показываю три сценария:

1. GET meeting загружает связанный graph из настоящего PostgreSQL;
2. POST registration действительно сохраняет запись, что подтверждается
   отдельным GET;
3. случайный неизвестный ID возвращает `404`.

Говорю:

> В Arrange тесты создают только свои данные. Первый тест также проверяет, что
> внутренние EF-поля `InternalNotes` и `AdminOnlyCode` не попадают в HTTP JSON.
> Это одновременно проверка EF query и границы DTO.

Альтернатива:

> Если suite сильно вырастет, контейнер можно разделить между несколькими
> классами. Но тогда нужно отдельно продумать базы, схемы и параллельность. Пока
> запуск не медленный, один контейнер на класс проще и нагляднее.

### Запуск

```bash
dotnet test \
  MeetingFlow.Microservices/tests/MeetingFlow.DataAccessor.ComponentTests/MeetingFlow.DataAccessor.ComponentTests.csproj
```

### Короткий вывод

> Компонентными тестами мы проверили DataAccessor целиком через его HTTP-границу:
> настоящий startup, routing, DTO mapping, EF Core queries и сохранение в
> настоящем PostgreSQL. Другие микросервисы при этом не запускались — системой
> под тестом оставался только DataAccessor вместе с необходимой ему базой данных.

---

## 3. RegistrationsManager — component tests со stubs и spy

### Компонент и граница

Открываю `src/Managers/RegistrationsManager/Program.cs` и endpoint
`POST /registrations`.

Говорю:

> RegistrationsManager — оркестратор registration flow. Он получает meeting и
> attendee, проверяет duplicate registration, спрашивает SchedulingEngine о
> capacity, рассчитывает цену, сохраняет registration и публикует событие.

> В этом тесте настоящий только Manager. DataAccessor и SchedulingEngine
> заменены HTTP stubs, RabbitMQ publisher заменён spy, а системное время —
> фиксированным `TimeProvider`.

### Fixture

Открываю `RegistrationsManagerFixture.cs`.

Показываю два WireMock-сервера:

```csharp
WireMockServer DataAccessorStub
WireMockServer SchedulingEngineStub
```

Говорю:

> WireMock — это настоящий локальный HTTP stub server. Production typed clients
> всё ещё формируют URL, сериализуют request и читают HTTP response, но реальные
> downstream-сервисы не запускаются.

Показываю передачу адресов stub-серверов:

```csharp
builder.UseSetting("DATA_ACCESSOR_URL", DataAccessorStub.Url!);
builder.UseSetting("SCHEDULING_ENGINE_URL", SchedulingEngineStub.Url!);
```

Показываю замену DI-регистраций:

```csharp
services.RemoveAll<IEventPublisher>();
services.AddSingleton<IEventPublisher>(EventPublisher);
services.RemoveAll<TimeProvider>();
services.AddSingleton<TimeProvider>(fixedTime);
```

Говорю:

> Production startup уже зарегистрировал настоящий RabbitMQ publisher и
> системное время. В test host мы удаляем эти регистрации и добавляем точные
> тестовые экземпляры. Singleton нужен, чтобы Manager и тест работали с одним и
> тем же spy и одним временем.

Показываю `Reset()`:

> Fixture общая для класса, поэтому перед каждым тестом очищаем WireMock stubs,
> request logs и сохранённые spy-события. Иначе один тест мог бы увидеть вызовы
> предыдущего.

### Тесты

Открываю `RegistrationsManagerComponentTests.cs`.

Показываю три сценария:

1. успешная регистрация — Manager вызывает зависимости с правильными данными и
   публикует `RegistrationCreatedV1`;
2. attendee уже зарегистрирован — flow возвращает `409` и останавливается до
   capacity check;
3. meeting заполнен — registration не сохраняется и событие не публикуется.

Говорю:

> В этих тестах важно проверять не только HTTP response, но и side effects:
> какие downstream-вызовы были выполнены и какие не должны были выполниться.
> Это основная ответственность оркестратора.

### Запуск

```bash
dotnet test \
  MeetingFlow.Microservices/tests/MeetingFlow.RegistrationsManager.ComponentTests/MeetingFlow.RegistrationsManager.ComponentTests.csproj
```

---

## 4. Registration notifications — targeted integration test

### Компоненты и граница

Открываю папку `IntegrationTests/RegistrationNotifications`.

Рисую короткую цепочку:

```text
real EventPublisher
  → RabbitMQ
    → real NotificationsAccessor consumer
      → PostgreSQL
```

Говорю:

> Здесь проверяется не бизнес-решение RegistrationsManager, а конкретная
> интеграция producer и consumer через RabbitMQ. Gateway, Managers и
> SchedulingEngine в эту границу не входят.

### Fixture

Открываю `RegistrationNotificationsFixture.cs`.

Показываю:

- RabbitMQ Testcontainer;
- PostgreSQL Testcontainer;
- `WebApplicationFactory` для NotificationsAccessor;
- production `EventPublisher`.

Говорю:

> RabbitMQ является транспортом проверяемой интеграции. PostgreSQL нужен
> настоящему consumer для сохранения результата. NotificationsAccessor
> запускается через `WebApplicationFactory`, и вместе с HTTP host запускается
> его hosted consumer.

Показываю `WaitForConsumerAsync()`:

> Запущенный RabbitMQ ещё не означает, что consumer успел создать queue и
> подписаться. Поэтому ждём конкретное условие с timeout, а не используем
> фиксированный `Task.Delay`.

### Тест

Открываю `RegistrationNotificationsIntegrationTests.cs`.

Говорю:

> Тест публикует настоящий `RegistrationCreatedV1`. Затем polling-ом ждёт
> notification через HTTP API и проверяет attendee, channel, subject, body и
> `SentAt`. Polling нужен потому, что обработка RabbitMQ асинхронна.

> Retry, dead-letter и idempotency тоже можно проверять на этом уровне, но лучше
> делать их отдельными сфокусированными сценариями.

### Запуск

```bash
dotnet test \
  MeetingFlow.Microservices/tests/MeetingFlow.Microservices.IntegrationTests/MeetingFlow.Microservices.IntegrationTests.csproj \
  --filter Category=Integration
```

---

## 5. Registration flow — system test всей системы

### Компоненты и граница

Открываю `System/SystemIntegrationTests.cs` и показываю полную цепочку:

```text
test → Gateway → RegistrationsManager → DataAccessor → PostgreSQL
                              ├→ SchedulingEngine
                              └→ RabbitMQ → NotificationsAccessor → PostgreSQL
```

Говорю:

> Это один критический backend flow через публичную границу Gateway. Браузер для
> такого system test не обязателен. В отличие от предыдущих примеров, тест не
> запускает приложение через `WebApplicationFactory` и не создаёт Testcontainers.
> Он подключается к реальной системе, заранее поднятой через Docker Compose.

### Fixture

Открываю `SystemIntegrationFixture.cs`.

Говорю:

> Fixture не управляет жизненным циклом Compose. Она создаёт `HttpClient` для
> Gateway и двух Accessors, проверяет health endpoints, наличие test-support
> routes и готовность RabbitMQ consumer. Если окружение запущено неправильно,
> тест падает сразу с понятной ошибкой.

Показываю запуск окружения:

```bash
docker compose \
  -f docker-compose.yml \
  -f docker-compose.system-tests.yml \
  up --build
```

Говорю:

> Второй Compose-файл не создаёт отдельную систему. Он добавляет
> `TestSupport__Enabled=true` для Accessors. Благодаря этому технические cleanup
> endpoints регистрируются только в тестовой конфигурации.

### Тест

Показываю Arrange:

> Через публичный Gateway тест создаёт собственные venue, meeting и attendee.
> Все значения содержат уникальный scenario ID, поэтому тест не зависит от seed
> data и не конфликтует с локальными данными.

Показываю Act и Assert:

> Registration создаётся через Gateway. Затем отдельный GET подтверждает
> сохранение registration, а polling ждёт notification для ID этого сценария.

Показываю `finally`:

> Cleanup выполняется даже после падения assertions. Сначала удаляются
> notification и registration, затем attendee, meeting и venue. Публичные CRUD
> endpoints используются там, где они существуют. Для технических записей без
> продуктового delete используются opt-in test-support endpoints владельцев
> данных.

Коротко называю альтернативу:

> В другом проекте можно использовать одноразовое окружение, реальные admin
> endpoints или прямой cleanup тестовой базы. Здесь выбран вариант, который
> позволяет многократно запускать тест на общей локальной системе и не затрагивать
> чужие данные.

### Запуск

```bash
dotnet test \
  MeetingFlow.Microservices/tests/MeetingFlow.Microservices.IntegrationTests/MeetingFlow.Microservices.IntegrationTests.csproj \
  --filter Category=System
```

Также после запуска Compose можно запустить конкретный system test кнопкой Play
в VS Code.

---

## Короткое завершение

> Мы увидели пять практических конфигураций: HTTP component без инфраструктуры,
> component с настоящей базой, orchestrator со stubs и spy, интеграцию через
> настоящий broker и system test полностью запущенного backend. В каждом случае
> fixture поднимает или подключает только то окружение, которое нужно выбранной
> границе теста.
