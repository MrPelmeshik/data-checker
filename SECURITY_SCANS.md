## Инструкция: проверки безопасности и качества кода

Этот проект содержит отдельный Compose-файл `compose.security.yaml` для запуска инструментов анализа безопасности и качества кода. Отчёты сохраняются в каталоге `out/`.

### Что входит
- SonarQube (сервер) и SonarScanner (CLI и .NET/MSBuild)
- Semgrep (SAST)
- Trivy: анализ файловой системы (исходники) и Docker-образа
- Gitleaks (поиск секретов)
- OWASP Dependency-Check (анализ уязвимых зависимостей)
- Hadolint (линтер Dockerfile)
- .NET проверка уязвимостей пакетов (`dotnet list package --vulnerable`)

### Требования
- Установлен Docker Desktop
- Интернет-доступ для скачивания образов и баз уязвимостей

### Базовая структура
- Рабочая директория: `data-checker/`
- Основной Compose: `compose.yaml`
- Безопасность: `compose.security.yaml`
- Отчёты: `out/`

### Быстрый старт c SonarQube
1) Запустите сервер SonarQube:
```bash
docker compose -f compose.security.yaml up -d sonarqube
```
2) Откройте `http://localhost:9000` (по умолчанию логин/пароль: `admin`/`admin`, потребуется смена пароля).
3) Создайте токен и добавьте его в `data-checker/.env`:
```
SONAR_TOKEN=ВАШ_ТОКЕН
```
4) Запустите сканер (варианты ниже).

### Запуск сканирований (из папки `data-checker/`)

- SonarScanner (CLI, авто-режим):
```bash
docker compose -f compose.security.yaml run --rm sonar-scanner
```

- SonarScanner for .NET (MSBuild, рекомендуемый для C#):
```bash
docker compose -f compose.security.yaml run --rm sonar-scanner-dotnet
```

- Semgrep (SAST по исходникам):
```bash
docker compose -f compose.security.yaml up --no-deps semgrep
```

- Trivy по файловой системе (исходники, секреты, IaC, лицензии):
```bash
docker compose -f compose.security.yaml up --no-deps trivy-fs
```

- Trivy по Docker-образу приложения:
```bash
# Сначала соберите образ приложения (используется compose.yaml)
docker compose -f compose.yaml build datachecker

# Затем просканируйте образ (по умолчанию tag datachecker:latest)
docker compose -f compose.security.yaml up --no-deps trivy-image
```

- Gitleaks (поиск секретов в репо):
```bash
docker compose -f compose.security.yaml up --no-deps gitleaks
```

- OWASP Dependency-Check (зависимости NuGet и др.):
```bash
docker compose -f compose.security.yaml up --no-deps depcheck
```

- .NET уязвимости пакетов (включая транзитивные):
```bash
docker compose -f compose.security.yaml up --no-deps dotnet-vuln
```

- Hadolint (проверка `DataChecker/Dockerfile`):
```bash
docker compose -f compose.security.yaml up --no-deps hadolint
```

### Где смотреть результаты
Все отчёты пишутся в `data-checker/out/`:
- `semgrep.json`
- `trivy-fs.sarif`
- `trivy-image.sarif`
- `gitleaks.sarif`
- `dependency-check-report.html`, `dependency-check-report.json`, `dependency-check-report.xml`
- `hadolint.sarif`
- `dotnet_vulnerable.txt`

Аналитика SonarQube доступна в веб-интерфейсе `http://localhost:9000`.

Если папка `out/` отсутствует, она будет создана автоматически при записи отчётов.

### Полезно знать
- Для SonarScanner используйте MSBuild-вариант (`sonar-scanner-dotnet`) для максимально точного анализа C#.
- Для Trivy-образа можно указать другой тег, изменив команду в `compose.security.yaml` или параметр при запуске.
- Базы данных (SonarQube и Dependency-Check) кешируются в Docker volumes для ускорения повторных запусков.

### Остановка и очистка
- Остановить SonarQube:
```bash
docker compose -f compose.security.yaml stop sonarqube
```

- Полностью удалить контейнеры безопасности (с сохранением данных):
```bash
docker compose -f compose.security.yaml down
```

- Полный сброс (включая тома SonarQube/кеши):
```bash
docker compose -f compose.security.yaml down -v
```


