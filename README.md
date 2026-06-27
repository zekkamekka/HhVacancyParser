# HhVacancyParser

Скриншоты и сравнение в .docx файле.

# Парсер и аналитика вакансий HeadHunter

Консольное приложение для практики: собирает вакансии с **hh.ru** и строит простую статистику.

## Как работает

Публичный JSON API `https://api.hh.ru/vacancies` сейчас часто возвращает **403 Forbidden** для программных запросов.  
Поэтому приложение загружает страницу поиска:

```
https://hh.ru/search/vacancy?text=...&page=...&area=...
```

и извлекает встроенный JSON (`HH-Lux-InitialState`) с помощью `HttpClient` + `System.Text.Json`.

## Запуск

```bash
cd HhVacancyParser
dotnet run -- --text ".NET developer"
dotnet run -- --text "Python developer"
```

## Что считает программа

1. **Топ-10 названий вакансий**
2. **Средняя зарплата** — только RUB/RUR; если указаны `from` и `to`, берётся середина
3. **Количество по городам**

## Visual Studio 2022/2026

1. Откройте `HaHaRu.sln`
2. Выберите профиль **DotNet developer** или **Python developer** рядом с кнопкой
3. Нажмите **Ctrl+F5**
