# Library API

REST API для управления библиотекой: каталог книг, учёт физических экземпляров, выдача и возврат, очередь бронирований и штрафы. Реализованы роли читателя (`READER`) и библиотекаря (`LIBRARIAN`), аутентификация по JWT и получение сведений о книгах через Open Library.

## Технологии

- C# и ASP.NET Core, .NET 9.
- Entity Framework Core 9 и PostgreSQL (Npgsql).
- JWT Bearer для защищённых запросов.
- Postman: сценарии запросов в каталоге `postman/collections`.

## Запуск

Потребуются .NET 9 SDK и доступный сервер PostgreSQL. Выполняйте команды из каталога с `Library.csproj`.

1. Настройте подключение к PostgreSQL и параметры приложения через переменные окружения (пример для PowerShell):

```powershell
$env:ConnectionStrings__DefaultConnection = 'Host=localhost;Port=5432;Database=library_db;Username=postgres;Password=YOUR_PASSWORD'
$env:Jwt__SecretKey = 'replace-with-a-random-secret-at-least-32-bytes-long'
$env:Database__ResetOnStartup = 'false'
$env:Seed__LibrarianEmail = 'librarian@example.com'
$env:Seed__LibrarianPassword = 'ChangeMe123!'
$env:Seed__LibrarianName = 'Librarian'
```

2. Восстановите зависимости и запустите приложение:

```powershell
dotnet restore
dotnet build
dotnet run --launch-profile http
```

API будет доступно по адресу `http://localhost:8080`. При запуске автоматически применяются миграции и выполняется начальное заполнение учётной записи библиотекаря. Для создания и изменения схемы пользователь PostgreSQL должен иметь соответствующие права.

**Важно:** в `appsettings.Development.json` включён `Database:ResetOnStartup`. При значении `true` приложение удаляет базу перед применением миграций. Указанная выше переменная окружения отключает это поведение.

## Конфигурация

| Раздел | Назначение |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | Подключение к PostgreSQL |
| `Jwt:SecretKey` | Ключ подписи JWT |
| `Jwt:ExpirationMinutes` | Срок действия токена; в конфигурации — 60 минут |
| `OpenLibrary:BaseUrl` | Адрес Open Library |
| `OpenLibrary:TimeoutSeconds` | Тайм-аут внешних запросов; в конфигурации — 10 секунд |
| `Seed` | Имя, email и пароль начального библиотекаря |
| `Database:ResetOnStartup` | Удаление базы при старте |

Переменные окружения используют двойное подчёркивание вместо двоеточия. Для собственного окружения задайте свои пароли и ключ подписи.

## Работа с API

JSON использует `snake_case`. Зарегистрируйте читателя запросом `POST /api/auth/register`:

```json
{
  "email": "reader@example.com",
  "password": "Reader123!",
  "name": "Reader"
}
```

Для входа отправьте email и пароль на `POST /api/auth/login`. Полученный токен передавайте в заголовке защищённых запросов:

```http
Authorization: Bearer <token>
```

Для входа библиотекаря используйте данные раздела `Seed`.

### Основные маршруты

| Метод | Маршрут | Назначение |
| --- | --- | --- |
| POST | `/api/auth/register` | Регистрация читателя |
| POST | `/api/auth/login` | Получение токена |
| GET | `/api/books` | Каталог книг |
| POST | `/api/books` | Добавление книги |
| PUT / PATCH | `/api/books/{id}` | Изменение книги |
| GET / POST | `/api/books/{id}/copies` | Просмотр и добавление экземпляров |
| PATCH | `/api/copies/{id}` | Изменение экземпляра |
| POST | `/api/loans` | Выдача книги |
| POST | `/api/loans/{id}/return` | Возврат книги |
| POST | `/api/reservations` | Бронирование |
| DELETE | `/api/reservations/{id}` | Отмена бронирования |
| GET | `/api/users/{id}/loans` | Выдачи пользователя |
| GET | `/api/users/{id}/fines` | Штрафы пользователя |
| POST | `/api/fines/{id}/pay` | Оплата штрафа |

Доступ к операциям зависит от роли и проверок в контроллерах и сервисах. Каталог доступен без токена. Управление книгами и экземплярами предназначено для библиотекаря.

## Правила библиотеки

Константы находятся в `Common/LibraryRules.cs`:

- До 5 активных выдач, срок выдачи — 14 дней.
- Удержание готовой брони — 48 часов.
- Штраф за просрочку — 10 за день, повреждение — 300, потерю — 1000.
- Порог суммы неоплаченных штрафов — 500.

## Структура проекта

- `Controllers` — HTTP-маршруты и авторизация.
- `Services` — бизнес-логика, очередь бронирований, JWT и клиент Open Library.
- `Models` — сущности и перечисления.
- `DTOs` — модели запросов и ответов.
- `Data` — контекст EF Core и начальное заполнение данных.
- `Migrations` — изменения схемы и снимок модели EF Core.
- `Common`, `Filters` — общие правила, исключения и валидация.
- `Options` — классы параметров конфигурации.
- `postman` — материалы для проверки API.

## Проверка

Для проверки компиляции используйте `dotnet build`. Сценарии Postman охватывают авторизацию, каталог, выдачи, бронирования, лимиты и контроль доступа; для их выполнения нужны запущенное API и база данных. Swagger UI в текущем `Program.cs` не подключён.
