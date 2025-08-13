# Data Checker

Консольное .NET 8 приложение для проверки качества данных в PostgreSQL по SQL‑правилам из таблицы `rules`. Новые правила проходят EXPLAIN‑проверки безопасности; валидные правила исполняются в read‑only транзакции, результаты сохраняются в `validation_results`.

## Возможности
- Проверка данных пользовательскими SQL‑правилами из таблицы `rules`
- EXPLAIN‑анализ (FORMAT JSON) для выявления опасных паттернов: DML/блокировки, SRF, системные схемы, запрещённые функции
- Выполнение проверок в `READ ONLY` транзакции с таймаутами
- Сохранение результатов в таблицу `validation_results`

## Запуск через Docker
1) Создайте `.env` в корне:
```env
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_HOST=postgres
```
2) Запуск:
```bash
docker compose up --build
```
3) Логи приложения:
```bash
docker compose logs -f datachecker
```
Остановить: `docker compose down` (с очисткой данных: `docker compose down -v`).

## Локальный запуск
Требуется .NET SDK 8.0 и PostgreSQL.
- Примените SQL из каталога `postgres/` к своей БД
- Переменные окружения (PowerShell):
```powershell
$env:POSTGRES_HOST="localhost"
$env:POSTGRES_USER="postgres"
$env:POSTGRES_PASSWORD="postgres"
```
- Запуск:
```powershell
dotnet run --project DataChecker
```

## Как добавить правило
`sql` должен возвращать `object_id int` и `is_fail boolean`; `fail_template` — текст описания при `is_fail = true`.
```sql
insert into rules (sql, fail_template)
values (
  $$
  select  object_id,
          (count(*) filter (where value_1 > 10)) > 1 as is_fail
  from test_data
  group by object_id
  $$,
  'Объект имеет более одного значения value_1 > 10'
);
```

## Полезное
- Основной compose: `compose.yaml`
- Инструменты анализа качества/безопасности: `compose.security.yaml`
- Значения можно переопределять через `.env`
