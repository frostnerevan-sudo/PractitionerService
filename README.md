# PractitionerService

## Технологический стек

.NET 8 / ASP.NET Core Web API
PostgreSQL 15 + Entity Framework Core
Redis
Apache Kafka 
Docker & Docker Compose

## Быстрый запуск (Docker Compose)

Весь стек (PostgreSQL, Apache Kafka, Zookeeper, Redis и сам сервис API) разворачивается одной командой из корневой директории проекта:

```bash
docker compose up -d --build
```

При первом запуске автоматически произойдут следующие действия:
1. Поднимутся контейнеры базы данных, кэша и брокера сообщений.
2. Init-контейнер создаст топик `practitioner-imports` в Kafka.
3. Приложение применит миграции EF Core и наполнит таблицу `practitioners` тестовыми данными (3 записи с фиксированными GUID для удобства проверки).

### Доступные эндпоинты после запуска
Swagger UI: `http://localhost:8080/swagger`
Базовый URL API: `http://localhost:8080/api/Practitioner`

## Асинхронный импорт через Kafka (Consumer)

Фоновый обработчик `PractitionerImportConsumer` запускается автоматически вместе с Web API как `HostedService` и подписывается на топик `practitioner-imports`.

### Как протестировать импорт вручную

1. Запустите интерактивный консольный продюсер внутри контейнера Kafka:
   ```bash
   docker exec -it fhir-kafka kafka-console-producer --bootstrap-server localhost:9092 --topic practitioner-imports
   ```

2. Вставьте JSON-сообщение:
   ```json
   {"sourceEventId":"evt-1001","firstName":"Дмитрий","lastName":"Волков","phone":"+375295554433","speciality":"Офтальмолог","placeOfWork":"Глазной центр","email":"volkov@clinic.by","isActive":true}
   ```
(При повторной отправке сообщения с тем же `sourceEventId` консьюмер зафиксирует дубликат в логах и пропустит запись).

## Примеры запросов к API

### 1. Получение врача по ID (Тестовая запись из БД)
```bash
curl -X GET "http://localhost:8080/api/Practitioner/11111111-1111-1111-1111-111111111111" \
     -H "Accept: application/fhir+json"
```

### 2. Полнотекстовый поиск и фильтрация
```bash
curl -X GET "http://localhost:8080/api/Practitioner?name=Иван%20Иванов&active=true" \
     -H "Accept: application/fhir+json"
```

### 3. Создание врача с ключом идемпотентности
```bash
curl -X POST "http://localhost:8080/api/Practitioner" \
     -H "Content-Type: application/json" \
     -H "X-Idempotency-Key: req-unique-001" \
     -d '{
       "firstName": "Елена",
       "lastName": "Козлова",
       "phone": "+375 29 123 45 67",
       "speciality": "Невролог",
       "placeOfWork": "Медицинский центр",
       "email": "kozlova@clinic.by",
       "isActive": true
     }'
```

### 4. Обновление врача с оптимистичной блокировкой
```bash
curl -X PUT "http://localhost:8080/api/Practitioner/11111111-1111-1111-1111-111111111111" \
     -H "Content-Type: application/json" \
     -H "If-Match: W/\"1\"" \
     -d '{
       "firstName": "Иван",
       "lastName": "Иванов",
       "phone": "+375 29 123 77 67",
       "speciality": "Главный терапевт",
       "placeOfWork": "Городская поликлиника №1",
       "email": "ivanov@://example.com",
       "isActive": true
     }'
```

### 5. Удаление врача (Soft Delete)
```bash
curl -X DELETE "http://localhost:8080/api/Practitioner/11111111-1111-1111-1111-111111111111"
```

## Тестирование через Postman

В корне репозитория приложен файл `PractitionerService.postman.json`. 
Коллекция содержит готовые сценарии для всех CRUD-операций.
