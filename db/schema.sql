-- ============================================================================
--  Кейтеринговая компания «Сибирский стол» (вариант 15, ТЗ ПР23-КТР)
--  Схема БД: 7 таблиц, 3НФ, InnoDB, utf8mb4, явные внешние ключи (RESTRICT/CASCADE)
--  Запуск: phpMyAdmin (http://localhost/phpmyadmin/) → «Импорт» → выбрать schema.sql
--  либо:   mysql -u root -p < db/schema.sql
--  Даты проведения банкетов подобраны с запасом; «сегодня» — 19.11.2026.
-- ============================================================================

CREATE DATABASE IF NOT EXISTS catering_db
  CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE catering_db;

SET FOREIGN_KEY_CHECKS = 0;
DROP TABLE IF EXISTS contract_menu_items;
DROP TABLE IF EXISTS catering_contracts;
DROP TABLE IF EXISTS catering_clients;
DROP TABLE IF EXISTS banquet_dishes;
DROP TABLE IF EXISTS event_types;
DROP TABLE IF EXISTS users;
DROP TABLE IF EXISTS roles;
SET FOREIGN_KEY_CHECKS = 1;

-- ---------------------------------------------------------------------------
-- 1. Роли пользователей (Администратор / Оператор-Менеджер + штатные должности)
-- ---------------------------------------------------------------------------
CREATE TABLE roles (
  id   INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(100) NOT NULL UNIQUE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------------
-- 2. Учётные записи сотрудников (пароли — только SHA256 hex)
--    Демо-логины и пароли:
--      ivanov   / chef123   (Администратор — шеф-менеджер кейтеринга)
--      smirnova / coord123  (Менеджер — банкетный координатор)
--      kuznetsov/ admin123  (Шеф-менеджер)
-- ---------------------------------------------------------------------------
CREATE TABLE users (
  id            INT AUTO_INCREMENT PRIMARY KEY,
  role_id       INT          NOT NULL,
  login         VARCHAR(50)  NOT NULL UNIQUE,
  password_hash VARCHAR(64)  NOT NULL,
  full_name     VARCHAR(255) NOT NULL,
  CONSTRAINT fk_users_role FOREIGN KEY (role_id) REFERENCES roles (id)
    ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------------
-- 3. Справочник типов мероприятий: наценка площадки и мин. число гостей (FSM)
-- ---------------------------------------------------------------------------
CREATE TABLE event_types (
  id                 INT AUTO_INCREMENT PRIMARY KEY,
  type_title         VARCHAR(150)   NOT NULL,
  service_multiplier DECIMAL(5,2)   NOT NULL DEFAULT 1.00,
  min_guests         INT            NOT NULL DEFAULT 10
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------------
-- 4. Справочник блюд банкетного меню / технологические карты
-- ---------------------------------------------------------------------------
CREATE TABLE banquet_dishes (
  id              INT AUTO_INCREMENT PRIMARY KEY,
  dish_title      VARCHAR(255)    NOT NULL,
  category        VARCHAR(100)    NOT NULL,
  portion_weight_g INT            NOT NULL DEFAULT 0,
  cost_per_person DECIMAL(10,2)   NOT NULL DEFAULT 0.00
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------------
-- 5. Клиенты (контрагенты) кейтеринговой компании
-- ---------------------------------------------------------------------------
CREATE TABLE catering_clients (
  id             INT AUTO_INCREMENT PRIMARY KEY,
  client_name    VARCHAR(255) NOT NULL,
  phone          VARCHAR(32)  NOT NULL,
  event_location VARCHAR(500) NOT NULL,
  is_vip         TINYINT(1)   NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------------
-- 6. Договоры кейтерингового обслуживания — заголовок документа (Master)
--    Номер по маске ПР23-КТР-{Год}-{Номер:D5}; статус — FSM.
-- ---------------------------------------------------------------------------
CREATE TABLE catering_contracts (
  id                INT AUTO_INCREMENT PRIMARY KEY,
  contract_number   VARCHAR(40)    NOT NULL UNIQUE,
  user_id           INT            NOT NULL,
  catering_client_id INT           NOT NULL,
  event_type_id     INT            NOT NULL,
  event_datetime    DATETIME       NOT NULL,
  guests_count      INT            NOT NULL,
  total_budget      DECIMAL(12,2)  NOT NULL DEFAULT 0.00,
  status            VARCHAR(50)    NOT NULL DEFAULT 'Предварительный расчет',
  cancel_reason     VARCHAR(1000)  NULL,
  CONSTRAINT fk_contracts_user   FOREIGN KEY (user_id)            REFERENCES users (id)
    ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_contracts_client FOREIGN KEY (catering_client_id) REFERENCES catering_clients (id)
    ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_contracts_type   FOREIGN KEY (event_type_id)      REFERENCES event_types (id)
    ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT chk_guests_positive CHECK (guests_count >= 0),
  CONSTRAINT chk_budget_nonneg   CHECK (total_budget >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------------
-- 7. Спецификация договора — строки заказа (Detail)
--    line_cost = servings_count * cost_per_person + equipment_rent_fee
-- ---------------------------------------------------------------------------
CREATE TABLE contract_menu_items (
  id                INT AUTO_INCREMENT PRIMARY KEY,
  contract_id       INT            NOT NULL,
  banquet_dish_id   INT            NOT NULL,
  servings_count    INT            NOT NULL,
  equipment_rent_fee DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  line_cost         DECIMAL(12,2)  NOT NULL DEFAULT 0.00,
  CONSTRAINT fk_items_contract FOREIGN KEY (contract_id)     REFERENCES catering_contracts (id)
    ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_items_dish     FOREIGN KEY (banquet_dish_id) REFERENCES banquet_dishes (id)
    ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT chk_servings_nonneg CHECK (servings_count >= 0),
  CONSTRAINT chk_rent_nonneg     CHECK (equipment_rent_fee >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE INDEX idx_contracts_status ON catering_contracts (status);
CREATE INDEX idx_contracts_event_date ON catering_contracts (event_datetime);
CREATE INDEX idx_items_contract ON contract_menu_items (contract_id);

-- ===========================================================================
--  Демонстрационные данные (5–10+ записей в каждой таблице)
-- ===========================================================================

INSERT INTO roles (id, name) VALUES
  (1, 'Администратор'),
  (2, 'Менеджер'),
  (3, 'Шеф-менеджер'),
  (4, 'Финансовый аналитик'),
  (5, 'Экспедитор');

INSERT INTO users (id, role_id, login, password_hash, full_name) VALUES
  (1, 1, 'ivanov',    'fa0990ab6f2ecfd562611cedad67152e8c1117f91c22d15094d1e242314243af', 'Иванов Сергей Петрович'),
  (2, 2, 'smirnova',  '8c63a2fc2b14d8ae6f9d0bf2e2c4227ac2dc4bd84768e1259226b0c3d84f1c65', 'Смирнова Ольга Андреевна'),
  (3, 3, 'kuznetsov', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 'Кузнецов Андрей Владимирович'),
  (4, 4, 'fedorova',  '93fa20ef07c6c1cd1cbd6d630ef41f6c0d6d4f9d81a888e684d21577ad6e27fe', 'Федорова Дарья Игоревна'),
  (5, 5, 'romanov',   'f3a08382ce7659823189350f0c715a7b5cb2d915b2c5d2360dc06f6d087c7431', 'Романов Сергей Николаевич');

INSERT INTO event_types (id, type_title, service_multiplier, min_guests) VALUES
  (1, 'Свадьба',             1.25, 30),
  (2, 'Корпоративный банкет', 1.10, 20),
  (3, 'Фуршет',              1.15, 25),
  (4, 'Выездной пикник',     1.30, 15),
  (5, 'Юбилей',              1.10, 15),
  (6, 'Деловой обед',        1.00, 10);

INSERT INTO banquet_dishes (id, dish_title, category, portion_weight_g, cost_per_person) VALUES
  (1,  'Брускетта с лососем',              'Холодные закуски', 90,   185.00),
  (2,  'Канапе с ростбифом',               'Холодные закуски', 60,   210.00),
  (3,  'Тартар из тунца',                  'Холодные закуски', 110,  320.00),
  (4,  'Жульен с курицей и грибами',       'Горячие закуски',  120,  175.00),
  (5,  'Суп-крем из тыквы',                'Первые блюда',     250,  150.00),
  (6,  'Салат «Цезарь» с курицей',         'Салаты',           150,  190.00),
  (7,  'Греческий салат',                  'Салаты',           140,  165.00),
  (8,  'Филе миньон с перечным соусом',    'Горячие блюда',    280,  640.00),
  (9,  'Лосось на гриле с овощами',        'Горячие блюда',    260,  590.00),
  (10, 'Панна-котта с ягодным соусом',     'Десерты',          120,  180.00),
  (11, 'Морс клюквенный (кувшин 1 л)',     'Напитки',          1000, 90.00),
  (12, 'Лимонад домашний (кувшин 1 л)',    'Напитки',          1000, 85.00);

INSERT INTO catering_clients (id, client_name, phone, event_location, is_vip) VALUES
  (1, 'ООО «Вектор Медиа»',                '+7 (384) 221-30-40', 'Площадь Советская, 12 (открытая площадка)',            1),
  (2, 'ИП Громов Дмитрий Сергеевич',       '+7 (913) 455-67-21', 'Ресторан «Сибирь», малый зал',                         0),
  (3, 'АО «КузбассУголь»',                 '+7 (384) 355-11-87', 'ДК Кузнецкий металлург, колонный зал',                 1),
  (4, 'Гимназия № 42 (выпускной)',         '+7 (384) 312-77-09', 'МАУК «Дворец культуры», большой зал',                  0),
  (5, 'ООО «СтройГарант»',                 '+7 (903) 940-22-15', 'Коттедж «Лесная поляна», терраса (открытая площадка)', 1),
  (6, 'Лебедева Мария Андреевна',          '+7 (923) 511-08-64', 'Загородный клуб «Таёжный», беседка',                   0),
  (7, 'АО «Химпром»',                      '+7 (384) 266-45-78', 'БЦ «Меркурий», 12 этаж, переговорная',                 0),
  (8, 'ГК «Атлас Девелопмент»',            '+7 (905) 901-17-33', 'Пансионат «Золотые пески», ресторан',                  1);

INSERT INTO catering_contracts (id, contract_number, user_id, catering_client_id, event_type_id, event_datetime, guests_count, total_budget, status, cancel_reason) VALUES
(1, 'ПР23-КТР-2026-00001', 2, 1, 1, '2026-06-20 12:00:00', 80, 168756.25, 'Смета утверждена', NULL),
(2, 'ПР23-КТР-2026-00002', 2, 2, 2, '2026-06-25 19:00:00', 45, 88692.50, 'Закупка сырья', NULL),
(3, 'ПР23-КТР-2026-00003', 1, 3, 1, '2026-07-11 18:30:00', 120, 268012.50, 'Банкет проведен', NULL),
(4, 'ПР23-КТР-2026-00004', 2, 4, 5, '2026-07-19 14:00:00', 35, 47667.50, 'Предварительный расчет', NULL),
(5, 'ПР23-КТР-2026-00005', 1, 5, 4, '2026-08-02 11:00:00', 25, 69976.25, 'Смета утверждена', NULL),
(6, 'ПР23-КТР-2026-00006', 2, 6, 3, '2026-08-14 17:00:00', 50, 68153.75, 'Отмена клиентом', 'Клиент перенёс мероприятие на сентябрь 2026 года'),
(7, 'ПР23-КТР-2026-00007', 1, 7, 6, '2026-08-27 13:00:00', 18, 33690.00, 'Закупка сырья', NULL),
(8, 'ПР23-КТР-2026-00008', 2, 8, 2, '2026-09-05 20:00:00', 60, 64204.50, 'Предварительный расчет', NULL),
(9, 'ПР23-КТР-2026-00009', 1, 2, 5, '2026-05-17 18:00:00', 28, 68362.00, 'Банкет проведен', NULL);


INSERT INTO contract_menu_items (contract_id, banquet_dish_id, servings_count, equipment_rent_fee, line_cost) VALUES
(1, 1, 30, 2000.00, 7550.00),
(1, 3, 15, 3000.00, 7800.00),
(1, 6, 60, 1500.00, 12900.00),
(1, 8, 80, 12000.00, 63200.00),
(1, 10, 80, 4000.00, 18400.00),
(1, 11, 40, 1000.00, 4600.00),
(2, 2, 25, 2500.00, 7750.00),
(2, 4, 30, 2000.00, 7250.00),
(2, 7, 45, 0.00, 7425.00),
(2, 9, 45, 8000.00, 34550.00),
(2, 12, 20, 1500.00, 3200.00),
(3, 1, 40, 3000.00, 10400.00),
(3, 4, 60, 5000.00, 15500.00),
(3, 5, 40, 3500.00, 9500.00),
(3, 8, 120, 15000.00, 91800.00),
(3, 9, 30, 6000.00, 23700.00),
(3, 10, 120, 5000.00, 26600.00),
(3, 11, 60, 2000.00, 7400.00),
(4, 2, 20, 1000.00, 5200.00),
(4, 6, 35, 1200.00, 7850.00),
(4, 7, 35, 800.00, 6575.00),
(4, 10, 35, 1500.00, 7800.00),
(5, 1, 15, 5000.00, 7775.00),
(5, 5, 20, 4000.00, 7000.00),
(5, 6, 25, 2500.00, 7250.00),
(5, 9, 25, 9000.00, 23750.00),
(5, 12, 10, 2500.00, 3350.00),
(6, 2, 50, 3000.00, 13500.00),
(6, 3, 20, 2500.00, 8900.00),
(6, 10, 50, 3000.00, 12000.00),
(6, 12, 25, 1000.00, 3125.00),
(7, 4, 18, 1000.00, 4150.00),
(7, 5, 18, 1200.00, 3900.00),
(7, 9, 18, 4500.00, 15120.00),
(7, 11, 8, 800.00, 1520.00),
(8, 1, 30, 2500.00, 8050.00),
(8, 6, 60, 1800.00, 13200.00),
(8, 10, 60, 2500.00, 13300.00),
(9, 2, 28, 1800.00, 7680.00),
(9, 7, 28, 900.00, 5520.00),
(9, 8, 28, 9500.00, 27420.00),
(9, 10, 28, 1800.00, 6840.00),
(9, 11, 14, 700.00, 1960.00);

-- Контрольный запрос: итоги сходятся с total_budget
-- SELECT c.contract_number, c.total_budget FROM catering_contracts c ORDER BY c.id;
