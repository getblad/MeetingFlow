# Практическая часть лекции: component, integration и system tests на примере MeetingFlow

Этот файл — не конспект для участников, а закадровый текст для живой демонстрации.
Фразы в блоках **«Говорю»** можно использовать почти дословно. Блоки
**«Показываю»** подсказывают, какой файл или фрагмент кода открыть.

---

## 1. Переход от теории к практике

### Показываю

- `MeetingFlow.Microservices/README.md`;
- схему взаимодействия сервисов;
- папку `MeetingFlow.Microservices/tests`.

### Говорю

> Мы разобрали определения и пирамиду тестирования. Теперь посмотрим, как эти
> идеи выглядят в микросервисном проекте. Важно, что я не буду определять тип
> теста только по используемой библиотеке. Testcontainers сам по себе не делает
> тест интеграционным, а `WebApplicationFactory` сам по себе не делает тест
> компонентным. Тип теста определяется выбранной границей: что мы считаем
> системой под тестом, через какую границу в неё входим и какие части оставляем
> реальными.

> В MeetingFlow есть Gateway, Managers, Engines, Accessors, PostgreSQL и
> RabbitMQ. Один и тот же registration flow можно проверить на разных уровнях.
> Но цель пирамиды — не повторить весь набор сценариев на каждом уровне. Мы
> хотим на каждом уровне получить уникальную уверенность за разумную цену.

### Главная мысль перед демонстрацией

```text
Component test
    проверяет один сервис через его внешнюю границу;
    внутренности сервиса реальные;
    внешние зависимости либо реальные и изолированные,
    либо заменены управляемыми двойниками.

Targeted integration test
    проверяет конкретный стык нескольких реальных компонентов.

System test
    проверяет критический сценарий через публичную границу
    полностью развёрнутой backend-системы.
```

> Терминология в разных компаниях может отличаться. Где-то тест Accessor плюс
> PostgreSQL назовут integration test. Это не проблема, если команда явно
> договорилась о границе. В этой лекции я называю его component test, потому что
> продуктовая система под тестом — один DataAccessor, а PostgreSQL является его
> инфраструктурной зависимостью.

---

## 2. Сначала показываю общую стратегию, а не код

### Показываю

Архитектурный flow регистрации:

```text
Client
  → Gateway
    → RegistrationsManager
      → DataAccessor → PostgreSQL
      → SchedulingEngine
      → RabbitMQ
        → NotificationsAccessor → PostgreSQL
```

### Говорю

> Если начать сразу писать системный тест на каждый возможный случай, мы
> получим медленный и хрупкий набор. Поэтому сначала раскладываем риски по
> уровням.

- Алгоритм пересечения временных интервалов и расчёт свободных мест удобно
  подробно проверять на уровне `SchedulingEngine`.
- Реальные EF-запросы, маппинг и сохранение в PostgreSQL проверяем на границе
  `DataAccessor`.
- Ветвление registration use case и порядок downstream-вызовов проверяем на
  уровне `RegistrationsManager` с управляемыми зависимостями.
- Совместимость publisher, RabbitMQ, event contract и consumer проверяем одним
  сфокусированным integration test.
- Полную сборку подтверждаем одним критическим happy path через Gateway.

> Обратите внимание: сценарии «неверный интервал», «участник уже
> зарегистрирован» и «зал заполнен» не обязаны ещё раз подробно повторяться в
> system suite. Их дешевле и точнее диагностировать ниже в пирамиде.

---

## 3. Пример №1 — SchedulingEngine как stateless component

### Показываю

1. `src/Engines/SchedulingEngine/Program.cs`;
2. `tests/MeetingFlow.SchedulingEngine.ComponentTests/MeetingFlow.SchedulingEngine.ComponentTests.csproj`;
3. `tests/MeetingFlow.SchedulingEngine.ComponentTests/SchedulingEngineComponentTests.cs`.

### Что делает компонент

`SchedulingEngine` предоставляет две HTTP-операции:

- `/scheduling/check-conflict` проверяет пересечение сессий в одной комнате;
- `/scheduling/check-capacity` считает оставшиеся места.

У него нет базы данных, очереди и downstream HTTP-сервисов.

### Говорю о границе

> Здесь системой под тестом является весь SchedulingEngine. Я не вызываю
> приватную функцию с алгоритмом напрямую. Тест входит через настоящий HTTP
> endpoint: проходят routing, JSON serialization, model binding, validation и
> response serialization. Именно поэтому это не unit test, хотя сервис очень
> маленький.

> `WebApplicationFactory<Program>` поднимает приложение внутри процесса
> testhost. Настоящий TCP-порт и Docker-контейнер для Engine не нужны. Клиент,
> созданный через `CreateClient`, отправляет запрос в in-memory test server.

> По смыслу мы имитируем запрос, который в production пришёл бы от
> MeetingsManager или RegistrationsManager. Дальше SchedulingEngine никуда не
> ходит, поэтому подменять нечего.

### Почему нужен `public partial class Program`

Показываю конец production `Program.cs`:

```csharp
public partial class Program { }
```

### Говорю

> В minimal API компилятор генерирует entry point. Эта пустая partial-декларация
> делает тип `Program` доступным тестовому проекту, чтобы
> `WebApplicationFactory<Program>` мог найти и запустить приложение. Она не
> содержит тестовой логики и не меняет поведение production-сервиса.

### Показываю `Theory`

Открываю:

```csharp
[Theory]
[InlineData(..., true)]
[InlineData(..., false)]
```

### Говорю

> Эти строки проверяют одно правило, но с разными наборами входных данных.
> Поэтому здесь логичен `Theory`, а не четыре почти одинаковых `Fact`.

Поясняю случаи:

- та же комната и пересечение времени — конфликт;
- граничное касание `11:00–12:00` после `10:00–11:00` — не конфликт;
- пересекающееся время, но другая комната — не конфликт;
- интервал до существующей сессии — не конфликт;
- сравнение имени комнаты регистронезависимое.

> `Theory` подходит, когда Arrange, Act и Assert остаются концептуально одними и
> теми же, а меняются только данные и ожидаемый результат. Не надо превращать в
> одну огромную theory принципиально разные поведения с разными причинами
> падения — такие тесты сложнее читать.

### Показываю отдельные `Fact`

- неправильный диапазон времени возвращает validation problem;
- корректный capacity request возвращает число свободных мест;
- отрицательная вместимость возвращает `400`.

### Говорю

> Некорректный диапазон — уже другое поведение HTTP-контракта, поэтому он вынесен
> в отдельный `Fact`. Здесь мы проверяем не только булев результат алгоритма, но
> и форму ошибки: статус `400` и ключ `candidate` в validation problem.

### Запускаю

```bash
dotnet test \
  MeetingFlow.Microservices/tests/MeetingFlow.SchedulingEngine.ComponentTests/MeetingFlow.SchedulingEngine.ComponentTests.csproj
```

Или нажимаю Play возле конкретного теста в VS Code.

### Что этот пример учит

- component test может быть очень быстрым и не использовать Docker;
- проверять сервис лучше через его публичную границу;
- `WebApplicationFactory` запускает реальный ASP.NET Core pipeline;
- похожие табличные случаи удобно выражать через `Theory`;
- отсутствие инфраструктуры у компонента не превращает HTTP component test в
  unit test.

### Альтернативы

> Чистый алгоритм также можно покрыть unit tests, если вынести его в отдельный
> класс. Такие тесты будут ещё быстрее. Но они не подтвердят routing, binding и
> HTTP contract. Можно иметь оба уровня, если алгоритм сложный, но не стоит
> механически дублировать каждый случай. Например, много граничных комбинаций
> оставить unit-тестам, а несколько репрезентативных контрактных случаев —
> component suite.

---

## 4. Пример №2 — DataAccessor и настоящий PostgreSQL

### Показываю

1. `tests/MeetingFlow.DataAccessor.ComponentTests/MeetingFlow.DataAccessor.ComponentTests.csproj`;
2. `DataAccessorFixture.cs`;
3. `DataAccessorComponentTests.cs`.

### Установленные библиотеки

```xml
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" ... />
<PackageReference Include="Testcontainers.PostgreSql" ... />
<PackageReference Include="Respawn" ... />
```

### Говорю

> DataAccessor нельзя содержательно проверить с in-memory коллекцией. Его
> ответственность — EF Core запросы, relations, PostgreSQL mapping и
> persistence. Поэтому сам DataAccessor запускается через
> `WebApplicationFactory`, а PostgreSQL — в одноразовом Testcontainer.

> Testcontainers использует настоящий образ `postgres:16`, запускает контейнер
> на динамическом host-порту и отдаёт тесту connection string. Тесту не важно,
> занят ли локальный `5432`, и он не подключается случайно к базе разработчика.

### Сначала объясняю, что такое fixture

Показываю класс `DataAccessorFixture` целиком, а затем его основные поля:

```csharp
private readonly PostgreSqlContainer _postgres = ...;
private WebApplicationFactory<Program>? _application;
private HttpClient? _client;
private Respawner? _respawner;
```

### Говорю

> Fixture — это объект, который хранит общее тестовое окружение и управляет его
> жизненным циклом. В данном случае она знает, как запустить PostgreSQL, поднять
> DataAccessor, создать HTTP client, подготовить Respawn, а в конце всё корректно
> освободить.

> Благодаря fixture эта инфраструктурная логика не копируется в каждом тесте.
> Сами тесты получают уже готовый `HttpClient` и методы для подготовки и очистки
> данных, поэтому могут сосредоточиться на сценарии Arrange, Act и Assert.

> Fixture — это не mock и не отдельный вид теста. Это способ организовать
> повторно используемый setup, состояние и cleanup тестового окружения.

### Разбираю lifecycle fixture

Показываю:

```csharp
public sealed class DataAccessorComponentTests(DataAccessorFixture fixture)
    : IClassFixture<DataAccessorFixture>, IAsyncLifetime
```

и:

```csharp
public Task InitializeAsync() => fixture.ResetDatabaseAsync();
```

### Говорю

> `IClassFixture<DataAccessorFixture>` означает: один экземпляр fixture на этот
> test class. Следовательно, PostgreSQL-контейнер запускается один раз для
> класса, а не перед каждым `Fact`. При этом xUnit создаёт новый экземпляр
> самого test class для каждого тестового случая.

> Пока suite небольшой и запуск контейнера не стал узким местом, fixture на
> класс — понятный и безопасный вариант. Если классов станет много и startup
> начнёт доминировать, можно поднять контейнер на collection или assembly и
> разделить тесты по отдельным базам/схемам. Но тогда придётся особенно аккуратно
> решать параллельность и изоляцию. Общий контейнер без стратегии разделения
> данных быстро создаёт flaky tests.

### Показываю конфигурацию приложения

```csharp
builder.UseSetting("POSTGRES_CONN", _postgres.GetConnectionString());
```

### Говорю

> Мы не переписываем production DI. Мы заменяем только конфигурацию connection
> string. Поэтому DataAccessor проходит обычный startup и использует обычный
> `MeetingFlowDbContext` и Npgsql provider.

### Объясняю seed data и Respawn

> Production startup создаёт schema и запускает обычный seed. Но тесты не
> должны полагаться на эти записи. Seed может измениться, исчезнуть или прийти в
> другом порядке. Поэтому после создания приложения fixture настраивает Respawn,
> а перед каждым тестом очищает application schemas.

> Respawn не пересоздаёт контейнер и не выполняет полный набор migrations заново.
> Он очищает таблицы с учётом зависимостей. В результате schema остаётся, а
> данные каждого теста начинаются с известного состояния.

> Seed в production-коде по-прежнему существует, но для этих тестов он не имеет
> значения. Это хороший признак самодостаточности: удаление или изменение seed
> не должно менять результат теста.

### Почему Arrange создаёт данные напрямую через EF

Показываю `fixture.SeedAsync(...)`.

> Для component test самого DataAccessor прямое создание EF entities допустимо:
> база и EF model находятся внутри выбранной границы этого компонента. Так мы
> можем точно подготовить граф, необходимый для проверки read endpoint.

> Это не означает, что прямой SQL или EF setup подходит каждому system test.
> На уровне всей системы такой setup связывает тест с внутренней схемой чужого
> сервиса и обходит реальные business API. У разных уровней разные допустимые
> инструменты.

> Ещё один вариант для Accessor component tests — создавать prerequisites через
> HTTP самого Accessor. Это уменьшает прямую связь теста с EF entities, но может
> сделать Arrange длиннее и одновременно начать проверять несколько write
> endpoints, которые не относятся к текущей цели. Выбор зависит от того, какой
> риск мы хотим проверить.

### Тест 1: чтение полного meeting graph

Показываю `GetMeeting_WhenMeetingExists_ReturnsGraphLoadedFromPostgreSql`.

### Говорю

> Тест сам создаёт venue, meeting, session, speaker, registration, attendee и
> feedback. После этого вызывает HTTP GET и проверяет, что реальный EF query
> загрузил связи и DataAccessor собрал правильный DTO.

> Дополнительно мы кладём в entity внутренние поля `InternalNotes` и
> `AdminOnlyCode`, а затем проверяем сырой JSON. Это проверка границы контракта:
> внутренние поля EF entity не должны утечь наружу.

### Тест 2: запись и отдельное чтение регистрации

Показываю `CreateRegistration_WhenReferencesExist_PersistsItInPostgreSql`.

> Сначала тест создаёт только необходимые foreign-key prerequisites. Затем
> запись выполняется через HTTP. После POST мы не ограничиваемся проверкой тела
> ответа: отдельным GET читаем список регистраций. Это доказывает, что строка
> действительно committed в PostgreSQL, а не просто сформирована в памяти.

### Тест 3: отсутствующая запись

> Отдельно проверяем `404` для случайного ID. Здесь Arrange не нужен именно
> потому, что Respawn гарантирует отсутствие чужих данных, а новый GUID делает
> сценарий независимым.

### Запускаю

```bash
dotnet test \
  MeetingFlow.Microservices/tests/MeetingFlow.DataAccessor.ComponentTests/MeetingFlow.DataAccessor.ComponentTests.csproj
```

Во время запуска открываю Docker Desktop и показываю появившиеся контейнеры.

### Объясняю второй контейнер Testcontainers

> Во время теста кроме PostgreSQL мы видим служебный контейнер Ryuk. Это resource
> reaper библиотеки Testcontainers. Он отслеживает созданные тестовым процессом
> контейнеры и другие Docker-ресурсы и помогает удалить их, даже если test process
> завершился нештатно.

> То есть это не вторая база данных и не ещё один микросервис MeetingFlow. Для
> нашего component test продуктовая инфраструктура здесь одна — PostgreSQL.
> Ryuk относится только к управлению жизненным циклом тестовых ресурсов.

После завершения suite показываю, что PostgreSQL Testcontainer и Ryuk больше не
работают.

### Что этот пример учит

- инфраструктурная зависимость компонента может оставаться реальной;
- Testcontainers даёт настоящий PostgreSQL и одноразовый lifecycle;
- контейнер можно переиспользовать между тестами, не переиспользуя данные;
- Respawn решает reset состояния, а не startup инфраструктуры;
- каждый тест должен владеть данными, на которых делает assertions;
- production seed не должен становиться скрытой fixture;
- проверка DTO boundary так же важна, как проверка EF query.

### Что было бы плохим паттерном

- искать «первый meeting» из production seed и делать assertions на него;
- рассчитывать, что тесты выполнятся в определённом порядке;
- использовать одну локальную базу разработчика и очищать её целиком;
- запускать новый PostgreSQL для каждого `Fact`, не измерив необходимость;
- использовать EF InMemory provider и считать, что PostgreSQL integration уже
  проверена;
- добавлять фиксированные `Task.Delay`, чтобы «база успела».

---

## 5. Пример №3 — RegistrationsManager с HTTP stubs и spy

### Показываю

1. production flow в `src/Managers/RegistrationsManager/Program.cs`;
2. `RegistrationsManagerFixture.cs`;
3. `RegistrationsManagerComponentTests.cs`;
4. `SpyEventPublisher.cs`.

### Сначала проговариваю ответственность Manager

> RegistrationsManager не просто сохраняет одну запись. Он получает meeting
> context, получает attendee, проверяет повторную регистрацию, спрашивает
> SchedulingEngine о capacity, рассчитывает цену, сохраняет регистрацию и
> публикует integration event.

> Именно orchestration является его ответственностью. Поэтому для component
> tests мы оставляем настоящий Manager и настоящий HTTP pipeline, но берём под
> контроль его внешние зависимости.

### Что реально, а что заменено

```text
Реально:
  RegistrationsManager;
  endpoint, validation, orchestration;
  typed HttpClient и HTTP serialization до stub-серверов;
  pricing logic.

Заменено:
  DataAccessor          → WireMock HTTP stub;
  SchedulingEngine      → WireMock HTTP stub;
  RabbitMQ publisher    → in-memory spy;
  системное время       → StubTimeProvider.
```

### Почему WireMock, а не mock `DataAccessorClient`

> Можно было подменить C# client interface обычным mock. Это было бы быстрее и
> ближе к unit test. Но WireMock поднимает настоящий локальный HTTP stub server.
> Поэтому production typed client реально формирует URL, сериализует body и
> читает HTTP response. Мы сохраняем больше уверенности в HTTP-интеграции, но не
> запускаем DataAccessor и PostgreSQL.

> Stub отвечает заранее заданными данными. Spy ничего не решает за тестируемый
> код, а записывает опубликованные события, чтобы мы могли сделать assertions.
> Слово mock часто используют как общий термин, но различие полезно: stub даёт
> входные ответы, spy позволяет наблюдать выходные взаимодействия.

### Показываю конфигурацию fixture

```csharp
builder.UseSetting("DATA_ACCESSOR_URL", DataAccessorStub.Url!);
builder.UseSetting("SCHEDULING_ENGINE_URL", SchedulingEngineStub.Url!);
```

и замену DI:

```csharp
services.RemoveAll<IEventPublisher>();
services.AddSingleton<IEventPublisher>(EventPublisher);
services.RemoveAll<TimeProvider>();
services.AddSingleton<TimeProvider>(fixedTime);
```

### Говорю

> Здесь важна seam — точка, в которой production dependency можно заменить без
> изменения бизнес-кода. RabbitMQ скрыт за `IEventPublisher`, а время — за
> `TimeProvider`. Детерминированное время нужно потому, что цена зависит от даты.
> Без него один и тот же тест мог бы вернуть другую цену через неделю.

### Зачем `Reset()` вызывается в конструкторе test class

> Fixture создаётся один раз на класс, а WireMock logs и список событий живут в
> нём между тестами. xUnit создаёт новый экземпляр test class перед каждым
> `Fact`, поэтому вызов `_fixture.Reset()` в конструкторе очищает stubs, request
> logs и spy перед каждым сценарием.

> Это не универсальная магия и не обязательный метод с названием `Reset`.
> Важно обеспечить изоляцию mutable fixture state. Альтернативы — отдельная
> fixture на тест, `IAsyncLifetime.InitializeAsync`, уникальные stub servers или
> immutable setup. Конструктор здесь удобен, потому что reset синхронный и
> короткий.

### Тест успешной регистрации

Показываю по порядку:

1. stubs meeting и attendee;
2. пустой список существующих регистраций;
3. ответ SchedulingEngine;
4. ответ на сохранение;
5. POST к Manager;
6. assertions на response, outgoing requests и event.

### Говорю

> Мы проверяем не только итоговый `201`. Для orchestrator важно доказать, что он
> отправил downstream правильные данные: capacity `800`, registration count `0`
> и нормализованный ticket type `General`. Затем spy подтверждает routing key и
> содержимое versioned event.

> Это interaction assertions. Ими не стоит злоупотреблять в обычной domain
> логике, потому что они связывают тест с реализацией. Но для Manager порядок и
> содержание внешних взаимодействий — часть его observable responsibility.

### Тест duplicate registration

> Stub возвращает существующую регистрацию этого attendee. SchedulingEngine
> вообще не настроен. Ожидаем `409`, пустые логи SchedulingEngine и отсутствие
> event. Так мы проверяем не только результат, но и раннюю остановку: после
> бизнес-отказа дорогие и изменяющие состояние операции выполняться не должны.

### Тест full meeting

> Здесь capacity равен количеству регистраций. SchedulingEngine возвращает
> `HasCapacity=false`. Мы проверяем, что Manager не вызвал POST сохранения и не
> опубликовал событие.

### Запускаю

```bash
dotnet test \
  MeetingFlow.Microservices/tests/MeetingFlow.RegistrationsManager.ComponentTests/MeetingFlow.RegistrationsManager.ComponentTests.csproj
```

### Что этот пример учит

- component test может использовать реальные HTTP stubs без реальных
  downstream-сервисов;
- оркестрацию удобно проверять управляемыми сценариями зависимостей;
- важны не только successful calls, но и доказательство отсутствия side effects;
- внешнее время нужно контролировать;
- shared fixture state надо сбрасывать между тестами;
- RabbitMQ здесь не нужен, потому что цель — решение Manager опубликовать
  правильное событие, а не доставка этого события.

### Альтернатива

> Можно поднять DataAccessor и SchedulingEngine реально, но тогда тест станет
> шире, медленнее и будет сложнее точно создать failure cases. Реальную
> совместимость компонентов мы отдельно проверим targeted integration test, а
> полную сборку — system test. Именно разделение ответственности тестов не даёт
> пирамиде превратиться в набор одинаковых E2E-сценариев.

---

## 6. Пример №4 — targeted integration через RabbitMQ

### Показываю

1. папку `IntegrationTests/RegistrationNotifications`;
2. `RegistrationNotificationsFixture.cs`;
3. `RegistrationNotificationsIntegrationTests.cs`.

### Рисую границу

```text
real EventPublisher
  → RabbitMQ Testcontainer
    → real RegistrationEventConsumer внутри NotificationsAccessor
      → PostgreSQL Testcontainer
        → GET NotificationsAccessor API
```

За границей:

```text
Gateway, RegistrationsManager endpoint, DataAccessor, SchedulingEngine, Web UI
```

### Говорю

> Теперь вопрос другой: не принимает ли Manager правильное решение, а совместимы
> ли реальные producer и consumer через настоящий broker? Поэтому spy уже
> недостаточен.

> Мы используем production `EventPublisher`, настоящий RabbitMQ, настоящий
> versioned event `RegistrationCreatedV1`, настоящий hosted consumer и настоящую
> запись NotificationsAccessor в PostgreSQL.

> Это targeted integration test: он шире одного компонента, но намеренно не
> поднимает всю систему. Если он падает, область поиска ограничена messaging
> integration, а не десятью сервисами.

### Почему здесь два Testcontainer

> RabbitMQ — транспорт проверяемой интеграции. PostgreSQL — реальная
> инфраструктура consumer, необходимая для наблюдаемого результата. Контейнеры
> запускаются параллельно через `Task.WhenAll`, потому что не зависят друг от
> друга на этапе startup.

### Почему ждём consumer readiness

Показываю `WaitForConsumerAsync`.

> «Контейнер RabbitMQ запущен» не означает «consumer уже объявил queue и
> подписался». Если опубликовать сообщение слишком рано, тест может вести себя
> нестабильно. Fixture пассивно проверяет production queue и ждёт, пока у неё
> появится consumer.

> Фиксированный `Task.Delay(3000)` — плохая синхронизация. На быстром компьютере
> он зря замедляет тест, на медленном CI трёх секунд может не хватить. Лучше ждать
> конкретное условие с ограниченным timeout.

### Почему результат тоже polling

Показываю `WaitForNotificationAsync`.

> Message delivery асинхронна. `PublishAsync` подтверждает публикацию, но не
> завершение consumer. Поэтому тест опрашивает read API до появления записи.
> Важно, что ожидание ограничено timeout: тест никогда не должен зависнуть
> навсегда.

> Мы ищем результат по уникальному `attendeeId`. Нельзя просто взять последнюю
> notification или ожидать пустую базу как неявное предусловие. В данном
> примере контейнер одноразовый и тест один. Если suite расширится, понадобится
> cleanup/Respawn либо уникальные correlation IDs и более строгая изоляция.

### Что именно доказывает тест

- publisher использует правильные exchange/routing key;
- событие сериализуется совместимо с consumer;
- consumer подписан на правильную queue;
- handler создаёт корректную notification;
- запись действительно сохраняется;
- результат доступен через HTTP boundary NotificationsAccessor.

### Чего он не доказывает

- что Gateway вызывает RegistrationsManager;
- что Manager решит публиковать событие в нужном бизнес-сценарии;
- что registration сохранилась в DataAccessor;
- что весь deployment сконфигурирован правильно.

### Запускаю

```bash
dotnet test \
  MeetingFlow.Microservices/tests/MeetingFlow.Microservices.IntegrationTests/MeetingFlow.Microservices.IntegrationTests.csproj \
  --filter Category=Integration
```

### Можно ли здесь тестировать retry

> Да, retry, dead-letter routing, redelivery, idempotency и поведение при
> временной ошибке — нормальные темы для messaging integration tests, потому что
> они зависят от реального broker и consumer configuration. Но это должны быть
> отдельные сфокусированные сценарии.

> Для retry нужно уметь детерминированно вызвать временную ошибку и наблюдать
> число попыток или итоговую dead-letter queue. Не стоит проверять retry просто
> долгим ожиданием. И не надо добавлять такие сценарии, если retry-политика ещё
> не является частью production design.

---

## 7. Пример №5 — system test полного registration flow

### Показываю

1. `System/SystemIntegrationFixture.cs`;
2. `System/SystemIntegrationTests.cs`;
3. `docker-compose.system-tests.yml`;
4. test-only mappings в двух Accessor `Program.cs`.

### Сначала определяю system boundary

```text
test → Gateway → RegistrationsManager → DataAccessor → PostgreSQL
                              ├→ SchedulingEngine
                              └→ RabbitMQ → NotificationsAccessor → PostgreSQL
```

### Говорю

> Здесь тестируем уже не один компонент и не один стык. Нас интересует, работает
> ли критический backend flow в собранной системе. Клиент необязательно означает
> браузер. Для backend system test достаточно войти через публичную границу
> backend — Gateway. UI можно проверять отдельным browser E2E suite, если этот
> риск важен.

> В отличие от component tests, fixture не поднимает сервисы через
> `WebApplicationFactory` и не создаёт всю топологию Testcontainers. Она
> подключается к уже запущенному локальному Docker Compose. Это позволяет
> запускать или отлаживать конкретный тест кнопкой из VS Code и отдельно видеть
> логи сервисов.

### Почему Compose не запускается из xUnit fixture

> Поднять Compose из fixture технически возможно. Но для полного стека это часто
> неудобно: IDE может создать несколько testhost-процессов, порты могут
> конфликтовать, cleanup при аварийной остановке сложнее, а инфраструктурная
> ошибка выглядит как ошибка fixture.

> Поэтому здесь lifecycle окружения внешний: локально его запускает разработчик,
> в CI это делает workflow. Fixture отвечает за readiness и клиентов, но не за
> orchestration Docker.

> Другой валидный вариант — отдельный script: compose up, readiness, dotnet test,
> сбор логов и compose down. Мы не делаем script обязательной частью примера,
> чтобы тест было удобно запускать из IDE, но в CI такая автоматизация обычно
> полезна.

### Обычный запуск и system-test конфигурация

Обычная система:

```bash
docker compose up --build
```

С test-support:

```bash
docker compose \
  -f docker-compose.yml \
  -f docker-compose.system-tests.yml \
  up --build
```

### Говорю

> Override-файл не читается автоматически и не создаёт вторую систему. Compose
> объединяет файлы слева направо. Второй файл добавляет двум сервисам только
> `TestSupport__Enabled=true`. Имена контейнеров, порты и существующие volumes
> остаются теми же.

> ASP.NET Core преобразует environment variable `TestSupport__Enabled` в ключ
> `TestSupport:Enabled`. Если флаг отсутствует, test-only routes вообще не
> попадают в endpoint table и возвращают `404`.

### Fixture: что она проверяет

> Fixture создаёт клиентов для Gateway, DataAccessor и NotificationsAccessor.
> Gateway используется как public boundary. Прямые клиенты к Accessors нужны
> только для наблюдения notification и технического cleanup.

> Затем fixture проверяет health endpoints, наличие test-support routes и
> активного RabbitMQ consumer. Это fail-fast: если окружение запущено неправильно,
> мы получаем понятную инфраструктурную ошибку до создания test data.

### Важное ограничение текущего учебного примера

> URL сейчас фиксированы на `127.0.0.1`. Для CI или cloud deployment их следует
> вынести в environment variables. Test runner также должен иметь сетевой доступ
> к внутренним Accessor-сервисам. В production такие сервисы обычно не выставляют
> в публичный ingress.

### Setup через публичные endpoints

Показываю создание:

1. `POST /venues`;
2. `POST /meetings`;
3. `POST /attendees`.

### Говорю

> Раньше этот тест создавал prerequisites прямыми SQL-командами. Это работало,
> но связывало system test со схемой таблиц: любое изменение schema требовало
> поддерживать отдельный SQL setup. Кроме того, мы обходили реальные контракты.

> Теперь логичные product use cases выполняются через публичный Gateway.
> Создание venue, meeting и attendee — нормальные операции системы, а не API,
> придуманные только для теста.

> Все имена и email содержат уникальный scenario ID. Поэтому тест не зависит от
> seed data, не требует пустой базы и не конфликтует с существующими локальными
> записями. Ownership — ключевая идея: тест изменяет и удаляет только то, что сам
> создал.

### Act и синхронные assertions

> Основное действие — `POST /registrations` через Gateway. Мы проверяем `201`,
> server-generated ID, ссылки на созданные meeting и attendee, нормализованный
> ticket type и payment status.

> Затем отдельным публичным GET читаем registrations для meeting. Как и в
> Accessor-тесте, это подтверждает persistence через наблюдаемую границу, а не
> только красивый POST response.

### Асинхронный результат

> Notification появляется не в рамках HTTP-транзакции. Поэтому тест polling-ом
> ждёт notification для конкретного attendee и дополнительно ищет registration
> ID в body. Уникальная корреляция защищает от ложного успеха из-за старой записи.

> В реальном публичном продукте notification read API мог бы быть недоступен.
> Тогда наблюдать результат можно через email sandbox, event audit API,
> observability storage или внутренний test probe. Выбор зависит от того, что
> является приемлемым observable result системы.

### Cleanup и порядок зависимостей

Показываю `finally`.

```text
notifications
  → registrations
    → attendee
    → meeting
      → venue
```

### Говорю

> Cleanup находится в `finally`, поэтому запускается и после успешных
> assertions, и после исключения. В отличие от старого `await using`, здесь нет
> скрытого `DisposeAsync`: порядок явно виден в тесте.

> Сначала удаляются dependent records, затем principals. Иначе публичное
> удаление attendee или meeting правильно вернёт `409 Conflict`.

> Метод `TryDeleteAsync` собирает ошибки, а не прекращает cleanup после первой.
> Если удаление notification сломалось, тест всё равно попробует убрать
> registration, attendee, meeting и venue. В конце ошибки объединяются в
> `AggregateException`.

### Почему появились test-only endpoints

Показываю условную регистрацию:

```csharp
if (app.Configuration.GetValue<bool>("TestSupport:Enabled"))
{
    app.MapDelete("/_test/...", ...);
}
```

### Говорю

> Удаление venue, meeting и attendee — логичные product operations, поэтому они
> публичны. А удаление отправленной notification или отдельной registration в
> нашем примере не является публичным бизнес-сценарием. Добавлять их в Gateway
> только ради теста означало бы загрязнить production contract.

> Поэтому их owning Accessors имеют opt-in test-support routes. Они не
> проксируются Gateway, включаются только конфигурацией и идемпотентны: повторная
> очистка безопасно возвращает `204`.

> Это один из возможных паттернов, а не универсальное требование. В серьёзной
> системе одного feature flag недостаточно. Test deployment должен иметь
> отдельную базу, приватную сеть, ограниченный доступ и гарантию, что флаг не
> включён в production. При необходимости test-support API дополнительно
> аутентифицируется.

### Альтернативы cleanup

> Вариант первый — одноразовое окружение на test run. В CI это часто лучший
> выбор: подняли чистый deployment, выполнили тесты и уничтожили всю базу. Тогда
> cleanup отдельных строк может быть не нужен для защиты следующего pipeline,
> хотя ownership IDs всё равно полезны для диагностики и параллельности.

> Вариант второй — публичные delete operations, если они действительно нужны
> продукту. Это лучший вариант, когда операция имеет реальный business meaning.

> Вариант третий — внутренний admin/test-support API, как здесь. Его надо
> изолировать от production traffic.

> Вариант четвёртый — прямой SQL/EF cleanup. Он бывает практичным для
> контролируемой тестовой базы, особенно если публичного API нет. Но system test
> начинает знать внутреннюю schema и требует дополнительного сопровождения при
> migrations.

> Вариант пятый — cleanup по test run ID или tenant ID отдельным обслуживающим
> процессом. Это удобно для больших параллельных suites.

> То есть правильный ответ зависит от среды. Обязательны не test-only endpoints,
> а изоляция, ownership, повторяемость и отсутствие риска удалить чужие данные.

### Запускаю system test

```bash
dotnet test \
  MeetingFlow.Microservices/tests/MeetingFlow.Microservices.IntegrationTests/MeetingFlow.Microservices.IntegrationTests.csproj \
  --filter Category=System
```

Или запускаю конкретный тест из VS Code Testing после старта Compose.

### Что этот пример учит

- system test входит через публичную backend boundary;
- UI не обязателен для backend system test;
- полную топологию можно запускать внешним orchestrator, а не fixture;
- setup должен создавать уникальные данные через подходящую границу;
- system test не должен зависеть от seed и пустой базы;
- асинхронный flow требует readiness и bounded polling;
- cleanup должен выполняться после падения и учитывать зависимости;
- test-support API — допустимый, но не единственный вариант;
- один критический happy path даёт больше пользы, чем дублирование всей матрицы
  component tests.

---

## 8. Как эти тесты образуют пирамиду и не дублируют друг друга

### Показываю итоговую таблицу

| Уровень | Главный вопрос | Что реально | Что намеренно не проверяем повторно |
| --- | --- | --- | --- |
| SchedulingEngine component | Верно ли HTTP-поведение scheduling rules? | Весь Engine | БД, очередь, Managers |
| DataAccessor component | Верны ли HTTP contract, EF queries и persistence? | Accessor + PostgreSQL | Manager orchestration, RabbitMQ |
| RegistrationsManager component | Верно ли оркестрируется registration use case? | Manager + HTTP clients | Реальная БД и доставка сообщения |
| RabbitMQ integration | Совместимы ли publisher, broker и consumer? | Publisher + RabbitMQ + consumer + PostgreSQL | Gateway и бизнес-решение Manager |
| System | Работает ли критический flow в deployment целиком? | Весь backend | Полная матрица edge cases |

### Говорю

> Пирамида — это не требование иметь строго определённый процент каждого вида
> тестов. Это принцип обратной связи: большинство вариантов бизнес-логики
> проверяются дешёвыми и локализованными тестами, меньше тестов проверяет реальные
> интеграции, и совсем немного — полную систему.

> Посмотрим на отсутствие дублирования. Full meeting подробно проверяется в
> Manager component test. В system test мы не создаём десять регистраций, чтобы
> снова проверить capacity. Rabbit routing подробно проверяется targeted
> integration test. В system test нам достаточно увидеть итоговую notification.
> EF graph и закрытые поля DTO проверяются в DataAccessor component test, а не во
> всех верхних уровнях.

> Верхний тест не заменяет нижний, а нижний не заменяет верхний. Они отвечают на
> разные вопросы и при падении дают разную диагностическую ценность.

---

## 9. Как объяснить стоимость и скорость

### Говорю

> Стоимость теста — не только время выполнения. Это также сложность Arrange,
> число возможных причин падения, инфраструктура в CI, диагностика и поддержка
> данных.

```text
SchedulingEngine component
  дешёвый startup, минимум причин падения.

RegistrationsManager component
  немного дороже из-за HTTP stubs, но сценарии полностью управляемы.

DataAccessor component
  требует Docker и PostgreSQL; зато даёт уверенность в реальной persistence.

Targeted messaging integration
  требует двух контейнеров и асинхронного ожидания.

System test
  требует готовности всей топологии и имеет самую широкую область диагностики.
```

> Поэтому новая проверка должна располагаться на самом низком уровне, который
> способен уверенно обнаружить интересующий риск. Не «всегда ниже», а именно
> «самый дешёвый достаточный уровень».

---

## 10. Вопросы, которые вероятно зададут участники

### «Почему SchedulingEngine test не unit test?»

> Потому что вход идёт через HTTP boundary полного приложения. Проверяются
> routing, serialization, validation и endpoint behavior, а не отдельный метод.

### «Почему DataAccessor плюс PostgreSQL всё ещё называется component test?»

> Наша product boundary — DataAccessor. PostgreSQL является необходимой
> инфраструктурой компонента. Другая команда может назвать это integration test;
> важнее явно описать границу, чем спорить о ярлыке.

### «Почему не использовать EF InMemory?»

> Он не воспроизводит многие особенности PostgreSQL: SQL translation,
> constraints, relational behavior и типы. Для repository/accessor confidence
> настоящий provider ценнее.

### «Почему не поднимать новый PostgreSQL для каждого теста?»

> Это максимально изолированно, но дороже. Сейчас один контейнер на класс плюс
> Respawn обеспечивает достаточную изоляцию быстрее. Если появятся утечки
> состояния, решение можно пересмотреть.

### «Почему stubs Manager-теста не делают тест интеграционным?»

> Реальная HTTP serialization до WireMock проверяется, но product integration с
> настоящим DataAccessor не проверяется. Основная boundary остаётся одним
> Manager. Поэтому в нашей классификации это component test.

### «Зачем spy, если можно поднять RabbitMQ?»

> В Manager suite нам нужно проверить намерение опубликовать корректное событие.
> Реальная доставка отдельно проверяется targeted integration test. RabbitMQ в
> каждом Manager-сценарии увеличил бы стоимость без новой уверенности.

### «Почему system test не использует Testcontainers?»

> Testcontainers отлично подходит для одной или нескольких изолированных
> зависимостей. Здесь мы сознательно проверяем уже развёрнутую полную систему в
> Docker Compose. Технически всю топологию можно собирать Testcontainers, но это
> другой lifecycle и более сложная fixture.

### «Может ли system test работать с Testcontainers?»

> Да. Нет запрета поднимать всю систему программно. Решение зависит от числа
> сервисов, CI, портов, логов, параллельности и удобства локальной отладки. В
> нашем учебном примере внешний Compose делает границу deployment нагляднее.

### «Почему после теста нужен cleanup, если CI-среда одноразовая?»

> В локальной общей среде cleanup нужен для повторных запусков. В одноразовом CI
> он может быть не нужен для следующего запуска, но остаётся полезным, если тесты
> идут параллельно или среда сохраняется для диагностики. Стратегия зависит от
> lifecycle окружения.

### «Можно ли включить test-only endpoints в облаке?»

> Да, в отдельном test deployment через configuration. Но test runner должен
> иметь приватный сетевой доступ, база должна быть тестовой, а production
> deployment не должен регистрировать эти routes. Feature flag — только один
> слой защиты, а не полноценная security model.

### «Почему не сделать все delete endpoints публичными?»

> Публичный API должен выражать продуктовые возможности. Если удаление сущности
> имеет business meaning — публичный endpoint нормален. Если операция нужна
> исключительно для уборки теста, лучше не расширять внешний контракт без
> необходимости.

### «Нужно ли всегда удалять test data?»

> Нет единственного правила. Одноразовую базу можно уничтожить целиком. Общую
> долгоживущую среду нужно очищать безопасно. Но в любом случае тест должен явно
> владеть своими данными и не зависеть от порядка запуска.

---

## 11. Рекомендуемый порядок живой демонстрации

### Перед лекцией

- открыть solution из корня, где расположен `MeetingFlow.slnx`;
- убедиться, что тесты видны в VS Code Testing;
- запустить Docker Desktop;
- проверить, что порты MeetingFlow не заняты другим проектом;
- заранее восстановить NuGet packages;
- для system test запустить Compose с override;
- открыть Docker Desktop и pgAdmin как дополнительные визуальные инструменты;
- иметь готовые terminals для отдельных `dotnet test --filter` команд.

### Во время показа

1. Показать architecture flow и сформулировать границы.
2. Запустить один `Theory` SchedulingEngine из IDE.
3. Показать DataAccessor fixture, PostgreSQL и Ryuk в Docker Desktop.
4. Запустить DataAccessor test и показать, что контейнер удаляется после suite.
5. Показать WireMock setup и spy в Manager test.
6. Поставить breakpoint в Manager или test и запустить отдельный сценарий.
7. Показать два контейнера targeted integration test и readiness polling.
8. Перейти к уже запущенному Compose и выполнить system test через Gateway.
9. После system test показать, что test-owned rows удалены, а seed/local data
   остались.
10. Завершить таблицей пирамиды и объяснить, где мы сознательно не дублируем
    проверки.

### Команды по уровням

```bash
# SchedulingEngine component tests
dotnet test \
  MeetingFlow.Microservices/tests/MeetingFlow.SchedulingEngine.ComponentTests/MeetingFlow.SchedulingEngine.ComponentTests.csproj

# DataAccessor component tests
dotnet test \
  MeetingFlow.Microservices/tests/MeetingFlow.DataAccessor.ComponentTests/MeetingFlow.DataAccessor.ComponentTests.csproj

# RegistrationsManager component tests
dotnet test \
  MeetingFlow.Microservices/tests/MeetingFlow.RegistrationsManager.ComponentTests/MeetingFlow.RegistrationsManager.ComponentTests.csproj

# Targeted RabbitMQ integration test
dotnet test \
  MeetingFlow.Microservices/tests/MeetingFlow.Microservices.IntegrationTests/MeetingFlow.Microservices.IntegrationTests.csproj \
  --filter Category=Integration

# System test
dotnet test \
  MeetingFlow.Microservices/tests/MeetingFlow.Microservices.IntegrationTests/MeetingFlow.Microservices.IntegrationTests.csproj \
  --filter Category=System
```

---

## 12. Финальный закадровый вывод

### Говорю

> На этих примерах видно, что хороший набор тестов начинается не с выбора
> библиотеки, а с выбора риска и границы.

> SchedulingEngine показывает быстрый component test полного HTTP-сервиса без
> инфраструктуры. DataAccessor показывает реальную базу, Testcontainers,
> Respawn и ownership данных. RegistrationsManager показывает управляемую
> оркестрацию через HTTP stubs, spy и фиксированное время. RabbitMQ integration
> test доказывает совместимость реальных producer и consumer. System test
> подтверждает один критический flow через Gateway на полностью запущенной
> системе.

> Главное — не пытаться получить всю уверенность одним дорогим E2E-тестом. Мы
> распределяем проверки так, чтобы нижние уровни быстро и точно покрывали
> варианты поведения, интеграционные тесты проверяли рискованные стыки, а
> системные тесты оставались немногочисленным подтверждением того, что deployment
> действительно работает как единое целое.

> И ещё один общий принцип для всех уровней: тест должен владеть своими данными,
> не зависеть от seed или порядка запуска, ждать наблюдаемые условия вместо
> фиксированных пауз и оставлять после себя предсказуемое состояние.
