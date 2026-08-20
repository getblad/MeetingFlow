## 1. SchedulingEngine через `WebApplicationFactory`

### Показываю

1. `src/Engines/SchedulingEngine/Program.cs`;
2. `tests/MeetingFlow.SchedulingEngine.ComponentTests/MeetingFlow.SchedulingEngine.ComponentTests.csproj`;
3. `tests/MeetingFlow.SchedulingEngine.ComponentTests/SchedulingEngineComponentTests.cs`.

В `.csproj` показываю `Microsoft.AspNetCore.Mvc.Testing` и project references на
сам `SchedulingEngine` и его contracts.

> Первый package даёт `WebApplicationFactory`. Project reference на production
> сервис нужен, чтобы тип `Program` стал точкой входа тестового приложения, а
> reference на contracts позволяет отправлять те же модели, что использует
> реальный клиент.

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

### Альтернативы

> Чистый алгоритм также можно покрыть unit tests, если вынести его в отдельный
> класс. Такие тесты будут ещё быстрее. Но они не подтвердят routing, binding и
> HTTP contract. Можно иметь оба уровня, если алгоритм сложный, но не стоит
> механически дублировать каждый случай. Например, много граничных комбинаций
> оставить unit-тестам, а несколько репрезентативных контрактных случаев —
> component suite.

---

## 2. DataAccessor и настоящий PostgreSQL

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

### Как тест создаёт собственные данные

Показываю generic helper из fixture:

```csharp
public async Task SeedAsync<TEntity>(params TEntity[] entities)
    where TEntity : class
{
    using var scope = _application!.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MeetingFlowDbContext>();

    db.Set<TEntity>().AddRange(entities);
    await db.SaveChangesAsync();
}
```

> Для component test самого DataAccessor прямое создание EF entities допустимо:
> база и EF model находятся внутри выбранной границы этого компонента. Так мы
> можем точно подготовить граф, необходимый для проверки read endpoint.

> Generic-параметр позволяет helper работать с любой EF entity без `object[]` и
> runtime-проверок типов. Сущности разных типов передаются отдельными вызовами:
> сначала `SeedAsync(venue)`, затем `SeedAsync(meeting)` и
> `SeedAsync(attendee)`. Порядок повторяет foreign-key зависимости.

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

---

## 3. RegistrationsManager с HTTP stubs и spy

### Показываю

1. production flow в `src/Managers/RegistrationsManager/Program.cs`;
2. test project `.csproj`;
3. `RegistrationsManagerFixture.cs`;
4. `RegistrationsManagerComponentTests.cs`;
5. `SpyEventPublisher.cs`.

В `.csproj` выделяю `Microsoft.AspNetCore.Mvc.Testing`, `WireMock.Net` и project
references на Manager и используемые им contracts.

> `WebApplicationFactory` запускает Manager, а `WireMock.Net` даёт два локальных
> HTTP-сервера для ответов DataAccessor и SchedulingEngine. Отдельный mock
> framework для publisher не нужен: маленький `SpyEventPublisher` проще увидеть
> и объяснить прямо в коде.

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

### Альтернатива

> Можно поднять DataAccessor и SchedulingEngine реально, но тогда тест станет
> шире, медленнее и будет сложнее точно создать failure cases. Реальную
> совместимость компонентов мы отдельно проверим targeted integration test, а
> полную сборку — system test.

---

## 4. Targeted integration через RabbitMQ

### Показываю

1. `.csproj` общего проекта integration tests;
2. папку `IntegrationTests/RegistrationNotifications`;
3. `RegistrationNotificationsFixture.cs`;
4. `RegistrationNotificationsIntegrationTests.cs`.

В `.csproj` выделяю `Testcontainers.RabbitMq`, `Testcontainers.PostgreSql`,
`RabbitMQ.Client` и project references на production producer, consumer и event
contracts.

> Два Testcontainers packages управляют инфраструктурой. `RabbitMQ.Client`
> fixture использует для readiness-проверки очереди. А production references
> позволяют проверить настоящие `EventPublisher` и consumer, не копируя их код
> в тестовый проект.

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

### Показываю assertions

- `attendeeId` совпадает с опубликованным событием;
- notification имеет канал `Email`;
- subject содержит название встречи;
- body содержит `registrationId`;
- `SentAt` заполнен после обработки consumer.

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

## 5. System test полного registration flow

### Показываю

1. `System/SystemIntegrationFixture.cs`;
2. `System/SystemIntegrationTests.cs`;
3. `docker-compose.system-tests.yml`;
4. test-only mappings в двух Accessor `Program.cs`.

> System test лежит в том же xUnit-проекте, но Testcontainers из этого сценария
> не вызываются. Здесь fixture создаёт обычные `HttpClient` к уже работающему
> Compose-стенду.

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

> Prerequisites создаются через публичный Gateway: сначала venue, затем связанный
> с ним meeting и после этого attendee. Эти операции являются нормальными
> продуктовыми use cases, поэтому system test использует те же контракты, что и
> настоящий клиент системы.

> Такой setup не зависит от внутренней структуры таблиц и одновременно
> подтверждает, что система готова выполнить основной registration flow. При этом
> assertions на создание prerequisites остаются минимальными: это подготовка
> сценария, а не три отдельных полноценных теста CRUD.

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
> assertions, и после исключения. Порядок очистки явно виден в самом тесте, что
> особенно полезно при наличии зависимых сущностей и нескольких API.

> Сначала удаляются dependent records, затем principals. Иначе публичное
> удаление attendee или meeting правильно вернёт `409 Conflict`.

> Метод `TryDeleteAsync` собирает ошибки, а не прекращает cleanup после первой.
> Если удаление notification сломалось, тест всё равно попробует убрать
> registration, attendee, meeting и venue. В конце ошибки объединяются в
> `AggregateException`.

### Почему здесь используются test-only endpoints

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
> этом сценарии не имеет отдельного продуктового или административного смысла.
> Поэтому выставлять такие операции через Gateway только ради этого теста не
> требуется.

> Поэтому их owning Accessors имеют opt-in test-support routes. Они не
> проксируются Gateway, включаются только конфигурацией и идемпотентны: повторная
> очистка безопасно возвращает `204`.

> Если в системе уже есть подходящие CRUD-операции, нормально использовать их и
> для подготовки, и для очистки тестовых данных. Это позволяет тесту работать
> через те же контракты, которыми пользуются реальные клиенты, и не требует
> отдельного test API.

> Но продуктового CRUD может быть недостаточно. Например, обычный endpoint
> выполняет soft delete, запрещает удаление завершённой регистрации или вообще не
> предусматривает удаление отправленного уведомления. Для повторяемого теста при
> этом может требоваться настоящий hard delete или очистка связанных технических
> записей.

> В другом проекте можно выбрать иначе: добавить delete или admin operation в
> реальный API, если она полезна поддержке или другим продуктовым сценариям.
> Здесь я показываю именно opt-in test-support вариант. Для него одного feature
> flag недостаточно: тестовое окружение должно быть изолировано, а эти routes не
> должны быть доступны production traffic.

### Коротко называю альтернативы cleanup

> Кроме показанного подхода можно использовать публичные delete/admin
> operations, одноразовое окружение на test run или прямой cleanup тестовой базы.
> Последний вариант проще технически, но связывает system test со схемой БД.
> Для больших suites встречается и очистка всех записей по отдельному test-run
> или tenant ID. В любом варианте тест должен удалять только принадлежащие ему
> данные.

### Запускаю system test

```bash
dotnet test \
  MeetingFlow.Microservices/tests/MeetingFlow.Microservices.IntegrationTests/MeetingFlow.Microservices.IntegrationTests.csproj \
  --filter Category=System
```

Или запускаю конкретный тест из VS Code Testing после старта Compose.

---

## Завершение практической части

### Показываю

- зелёные результаты всех пяти примеров в панели Testing;
- остановившиеся Testcontainers в Docker Desktop;
- работающий Compose-стенд для system test.

### Говорю

> Мы прошли все примеры на уровне кода: запуск приложения через
> `WebApplicationFactory`, PostgreSQL и RabbitMQ через Testcontainers, очистку
> базы через Respawn, HTTP stubs через WireMock, spy для событий, фиксированное
> время и подключение system test к Docker Compose. Теперь можно вернуться к
> вопросам по конкретной реализации или запустить отдельный тест в debug-режиме.
