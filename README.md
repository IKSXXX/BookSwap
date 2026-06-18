<div align="center">

# 📚 BookSwap

**Платформа для обмена книгами между читателями**

Прочитанные книги часто пылятся на полке, а новые хочется получить без лишних трат.
BookSwap решает эту проблему: пользователи выкладывают свои книги, находят тех, кто готов
обменяться, и договариваются о встрече — без денег, только живой читательский интерес.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-EF%20Core-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Tests](https://img.shields.io/badge/tests-xUnit-brightgreen?logo=xunit)](https://xunit.net/)

</div>

---

## ✨ Возможности

| | |
|---|---|
| 📖 **Каталог книг** | Просмотр, поиск, фильтрация по жанрам, пагинация |
| 🗂️ **Карточка книги** | Обложка, описание, владельцы, статус доступности |
| 🔄 **Обмены** | Запрос на обмен, принятие/отклонение, история |
| ⭐ **Избранное** | Добавление книг в закладки |
| 💬 **Обсуждения** | Темы и комментарии к книгам (в реальном времени) |
| 📅 **Книга дня** | Ежедневная подборка на главной |
| 🧩 **Квиз** | Викторина «угадай книгу по цитате» |
| 🤖 **AI-рекомендации** | Поиск книг по описанию через GigaChat + fallback по ключевым словам |
| 🔔 **Уведомления** | Доставка в реальном времени через SignalR |
| 🛠️ **Админ-панель** | Управление книгами, пользователями, квизами |
| 🔐 **Авторизация** | Регистрация, вход, сброс пароля, роли (User / Admin) |

## 🧰 Стек

- **ASP.NET Core 8.0** (MVC)
- **Entity Framework Core** — InMemory (разработка) / PostgreSQL (продакшн)
- **ASP.NET Core Identity** — аутентификация и роли
- **SignalR** — чат и живые уведомления
- **AutoMapper** — маппинг сущностей в ViewModel
- **Serilog** — логирование (консоль + файлы с ротацией по дням)
- **GigaChat API** — AI-рекомендации книг
- **xUnit + Moq** — модульные тесты

## 🏗️ Архитектура

Решение состоит из трёх проектов:

```
BookSwap/
├── BookSwap.Web/      → ASP.NET Core MVC: контроллеры, сервисы, представления, хабы SignalR
├── BookSwap.Db/       → доменные сущности, EF Core, репозитории, Unit of Work
└── BookSwap.Tests/    → модульные тесты (xUnit + Moq)
```

- **Repository + Unit of Work** — доступ к данным абстрагирован за интерфейсами `IRepository<T>` / `IUnitOfWork`.
- **Mock-слой** — при `UseMockData: true` подключается `MockUnitOfWork` с тестовыми данными поверх InMemory-БД, что позволяет запускать приложение без настройки PostgreSQL.
- **Сервисный слой** — бизнес-логика вынесена в сервисы (`BookService`, `ExchangeService`, `AdminService`, `NotificationService`), возвращающие типизированный `ServiceResult`.
- **Rate limiting** — обращения к AI-эндпоинту ограничены отдельной политикой (HTTP 429 при превышении).

## 🚀 Запуск

Требуется [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
dotnet run --project BookSwap/BookSwap
```

При запуске с `UseMockData: true` БД автоматически заполняется тестовыми данными — настройка PostgreSQL не нужна.

### Тесты

```bash
dotnet test
```

## ⚙️ Конфигурация

В `appsettings.json`:

| Ключ | Описание |
|------|----------|
| `UseMockData` | `true` — InMemory + тестовые данные, `false` — PostgreSQL |
| `ConnectionStrings:DefaultConnection` | Строка подключения к PostgreSQL (при `UseMockData: false`) |
| `GigaChat:AcceptAnyServerCertificate` | `true` — пропускать проверку TLS-сертификата GigaChat (по умолчанию только в Development) |

### 🔑 Секреты

`GigaChat:AuthorizationKey` **не хранится в репозитории**. Задайте его через User Secrets (для локальной разработки):

```bash
dotnet user-secrets --project BookSwap/BookSwap set "GigaChat:AuthorizationKey" "<ваш-ключ>"
```

или через переменную окружения:

```bash
export GigaChat__AuthorizationKey="<ваш-ключ>"
```
