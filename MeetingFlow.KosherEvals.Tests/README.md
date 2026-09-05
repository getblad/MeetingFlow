# Проверка кошерности / Kosher evaluations

Один тест вызывает настоящий сервис, сравнивает статусы и передаёт объяснения отдельной модели-судье.
One test calls the real service, compares statuses, and sends explanations to a separate judge model.

## Настройки / Settings

Задайте переменные окружения в терминале, из которого будете запускать тест. Не сохраняйте ключи в репозитории.
Set environment variables in the terminal used to run the test. Do not save keys in the repository.

| Переменная / Variable | Назначение / Purpose | По умолчанию / Default |
| --- | --- | --- |
| `AiChat__ApiKey` | Ключ OpenAI / OpenAI API key | Обязателен / Required |
| `AiChat__Model` | Проверяемая модель / Evaluated model | `gpt-5-mini` |
| `AiChat__Endpoint` | Адрес OpenAI / OpenAI endpoint | `https://api.openai.com/v1` |
| `AiJudge__ApiKey` | Ключ Groq / Groq API key | Обязателен / Required |
| `AiJudge__Model` | Модель-судья / Judge model | `openai/gpt-oss-120b` |
| `AiJudge__Endpoint` | Адрес Groq / Groq endpoint | `https://api.groq.com/openai/v1` |

Модель-судья должна поддерживать строгий ответ по JSON-схеме. Проверка формата остаётся включённой.
The judge model must support strict JSON-schema output. Schema validation remains enabled.

## Запуск / Run

Из корня репозитория / From the repository root:

```powershell
dotnet test MeetingFlow.KosherEvals.Tests --logger "console;verbosity=normal"
```

Каждый случай отправляется отдельно: запрос настоящему сервису, затем запрос судье.
Для шести случаев это шесть запросов сервису и шесть судье, последовательно.
Возможны расходы по тарифам провайдеров. Общий предел ожидания запросов — пять минут.
Each case is sent separately: a request to the real service followed by a judge request.
Six cases mean six service requests and six judge requests, executed sequentially.
Provider charges may apply. The overall request timeout is five minutes.

## Отчёты / Reports

В выводе теста будут полные пути к JSON и HTML. Обычно файлы находятся в
`MeetingFlow.KosherEvals.Tests/bin/Debug/net10.0/reports/`. Откройте HTML обычным браузером.
The test output prints full JSON and HTML paths. Files normally appear in the directory above.
Open the HTML file in a browser.

`ReportWriter.SaveJsonAndHtmlAsync(report, directory)` сохраняет оба файла и возвращает их пути в `JsonPath` и `HtmlPath`.
Сначала сохраняется JSON, затем `ReportWriter.ConvertJsonToHtmlAsync(jsonPath, htmlPath)` читает этот JSON и создаёт HTML по указанному пути.
Этот метод можно повторно вызвать для существующего отчёта без запросов к моделям.
`ReportWriter.SaveJsonAndHtmlAsync(report, directory)` saves both files and returns their paths in `JsonPath` and `HtmlPath`.
JSON is saved first; then `ReportWriter.ConvertJsonToHtmlAsync(jsonPath, htmlPath)` reads it and creates HTML at the specified path.
The method can also convert an existing report without any model calls.

Успех случая вычисляется кодом: статус совпал, `Score == 2`, `HasInventedFacts == false`.
Сбой запроса сохраняет уже завершённые случаи и пометку `Incomplete`; среднее считается только по полученным оценкам.
Ошибки настройки до создания сервиса и ошибки записи файлов могут помешать сохранению отчёта.
Case success is computed in code: matching status, `Score == 2`, and `HasInventedFacts == false`.
A request error preserves completed cases with `Incomplete`; averages include only received scores.
Setup errors before service creation and filesystem errors can prevent report creation.
