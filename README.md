# TechStore API

REST API для интернет-магазина техники, разработанный на ASP.NET Core 8.0.

## Функционал
- **Пользователи:** Регистрация, Аутентификация (JWT), Роли (Admin/Customer).
- **Товары:** CRUD операции, Soft Delete, Валидация наличия на складе.
- **Заказы:** Создание заказа, списание товаров, расчет итоговой суммы, история заказов.

## Технологии
- **Backend:** C# / ASP.NET Core Web API
- **Database:** MSSQL / Entity Framework Core
- **Testing:** xUnit / Moq / InMemory Database

## Тестирование
Проект покрыт Unit-тестами (xUnit) на **90%** (Services + Controllers).
Используется `InMemoryDatabase` для изоляции тестов.

### Запуск тестов:
1. Открыть решение в Visual Studio.
2. Перейти в `Test Explorer`.
3. Нажать `Run All Tests`.