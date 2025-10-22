.PHONY: help build up down logs restart clean

help: ## Показать эту справку
	@echo "Доступные команды:"
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  %-15s %s\n", $$1, $$2}'

build: ## Собрать Docker образ
	docker-compose build

up: ## Запустить бота в фоновом режиме
	docker-compose up -d

down: ## Остановить бота
	docker-compose down

logs: ## Показать логи бота
	docker-compose logs -f

restart: ## Перезапустить бота
	docker-compose restart

clean: ## Остановить и удалить контейнеры и образы
	docker-compose down --rmi all

run-local: ## Запустить бота локально без Docker
	dotnet run

restore: ## Восстановить зависимости
	dotnet restore

test: ## Собрать проект
	dotnet build

