# 💬 ChatApp — Модульный чат на .NET 9 с SignalR

![.NET 9](https://img.shields.io/badge/.NET-9-purple?style=flat-square)
![SignalR](https://img.shields.io/badge/SignalR-Real--time-blue?style=flat-square)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Database-blue?style=flat-square)
![Redis](https://img.shields.io/badge/Redis-Cache-red?style=flat-square)
![Docker](https://img.shields.io/badge/Docker-Containerized-blue?style=flat-square)

## О проекте

**ChatApp** —  чат-приложение на .NET 9, реализующее чистую архитектуру (DDD, разделение слоёв), JWT-аутентификацию, real-time обмен сообщениями с помощью SignalR, хранение данных в PostgreSQL и Redis, а также удобное логирование через Serilog.

Проект включает в себя реализацию REST API для работы с пользователями, чатами и сообщениями, а также полностью поддерживает веб-сокеты для мгновенной передачи сообщений.

### ✨ Основные возможности

- 🔐 **Регистрация и аутентификация пользователей** (JWT)
- 💬 **Создание, просмотр, поиск чатов и сообщений**
- ⚡ **Реальное время**: SignalR-hub для отправки/приёма сообщений
- 🔍 **Полнотекстовый поиск** по сообщениям (PostgreSQL)
- 📝 **Логирование и обработка ошибок**
- 🏗️ **Чистая архитектура**: разделение на Application, Domain, Infrastructure и Web-слои
- 🐳 **Готов к деплою** в Docker/Kubernetes/Azure App Service

---

## 🛠️ Технологический стек

| Технология | Назначение |
|------------|------------|
| **.NET 9** | Основной фреймворк |
| **ASP.NET Core Web API** | REST API |
| **SignalR** | Real-time коммуникация |
| **Entity Framework Core + Npgsql** | ORM и работа с PostgreSQL |
| **Redis** | Backplane для SignalR |
| **JWT Authentication** | Аутентификация |
| **Serilog** | Логирование |
| **Docker Compose** | Инфраструктура |
| **Swagger** | Документация API |

---

## 🚀 Быстрый старт (Docker Compose)

### 1. Клонирование репозитория

```bash
git clone <URL>
cd <repo_folder>
```

### 2. Запуск всех сервисов

```bash
docker compose up --build
```

> 📌 По умолчанию сервис API будет доступен на `http://localhost:5000`

### 3. Доступ к сервисам

- **🔧 Swagger UI**: [http://localhost:5000/swagger](http://localhost:5000/swagger) — полная документация и тестирование API
- **🧪 Тестовая HTML-страница**: `/test.html` — простой веб-клиент для SignalR

---

## 🧪 Ручное тестирование через Swagger + SignalR

### 1️⃣ Регистрация пользователей

1. Перейдите в **Swagger** (`/swagger`)
2. Используйте метод `POST /api/Auth/register`:

```json
{
  "userName": "user1",
  "password": "StrongPassword1!",
  "displayName": "User One"
}
```

3. Зарегистрируйте двух пользователей: `user1` и `user2`

### 2️⃣ Аутентификация и получение JWT

Используйте метод `POST /api/Auth/login` для каждого пользователя:

```json
{
  "userName": "user1",
  "password": "StrongPassword1!"
}
```

> 💡 В ответе получите `accessToken` — сохраните его для авторизации и SignalR

### 3️⃣ Создание чата

1. В Swagger авторизуйтесь как `user1` (через "Authorize" и Bearer-токен)
2. Вызовите `POST /api/Chats`:

```json
{
  "participantIds": [
    "<GUID user1>", 
    "<GUID user2>"
  ],
  "name": "Тестовый чат"
}
```

> 📝 `participantIds` — это GUID'ы пользователей из результата регистрации/логина

### 4️⃣ Получение истории сообщений

`GET /api/Messages/by-chat/{chatId}` — вернёт список сообщений в чате

### 5️⃣ Тестирование real-time через HTML-страницу

1. Откройте `/test.html` в браузере
2. Заполните поля:
   - **JWT token**: `accessToken` пользователя
   - **Chat ID (GUID)**: ID созданного чата
3. Нажмите **Connect** для подключения к SignalR
4. Откройте второе окно браузера с другим пользователем
5. Отправляйте сообщения — они будут отображаться в реальном времени! 🎉

---

## 📋 Примеры API запросов

### Регистрация пользователя

```http
POST /api/Auth/register
Content-Type: application/json

{
  "userName": "user1",
  "password": "StrongPassword1!",
  "displayName": "User One"
}
```

### Авторизация

```http
POST /api/Auth/login
Content-Type: application/json

{
  "userName": "user1",
  "password": "StrongPassword1!"
}
```

### Создание чата

```http
POST /api/Chats
Authorization: Bearer <your-jwt-token>
Content-Type: application/json

{
  "participantIds": [
    "GUID_пользователь1",
    "GUID_пользователь2"
  ],
  "name": "Test Chat"
}
```

---

## 🌐 Использование тестовой SignalR-страницы

### Настройка тестового клиента

1. **Откройте** `test.html` в браузере
2. **Заполните поля**:
   - `JWT token` — accessToken пользователя
   - `Chat ID (GUID)` — ID созданного чата
3. **Подключитесь** и отправьте сообщение
4. **Откройте несколько вкладок** для имитации нескольких пользователей

---

## 🔧 Запуск в режиме разработки (без Docker)

### Предварительные требования

- PostgreSQL и Redis должны быть запущены локально
- Настроены соответствующие строки подключения

### Команды для запуска

```bash
cd src/ChatApp.Web
dotnet run
```

> 🌐 Приложение будет доступно по адресу `http://localhost:5000`

---

## ⚠️ Важные моменты

| Аспект | Описание |
|--------|----------|
| **SignalR токен** | Используйте `accessToken` **без** префикса `Bearer` |
| **CORS** | В разработке разрешены любые origins для тестирования |
| **Тестовый клиент** | `test.html` можно разместить в любом месте |
| **API endpoint** | Сервер должен быть доступен по `localhost:5000` |

---

## 📁 Структура проекта

```
ChatApp/
├── 📂 src/
│   ├── 📂 ChatApp.Domain/          # Бизнес-логика и сущности (User, Chat, Message)
│   ├── 📂 ChatApp.Application/     # Сервисы, интерфейсы, DTO
│   ├── 📂 ChatApp.Infrastructure/  # Репозитории, EF Core, JWT, интеграции
│   └── 📂 ChatApp.Web/            # API-контроллеры, SignalR-хаб, Program.cs
├── 📄 test.html                    # Простой клиент для тестирования SignalR
├── 📄 docker-compose.yml          # Docker конфигурация
└── 📄 README.md                   # Этот файл
```

---
