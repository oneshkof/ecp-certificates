using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace WindowsFormsApp1.Data
{
    public static class Database
    {
        private static string _dbPath;

        public static string DbPath
        {
            get
            {
                if (_dbPath == null)
                {
                    _dbPath = Path.Combine(ResolveDataDirectory(), "ecp.db");
                }
                return _dbPath;
            }
        }

        private static string ResolveDataDirectory()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            // Поднимаемся на несколько уровней вверх в поисках файла проекта.
            for (int i = 0; i < 8 && directory != null; i++)
            {
                if (directory.GetFiles("*.csproj").Length > 0)
                {
                    return directory.FullName;
                }
                directory = directory.Parent;
            }

            // Файл проекта не найден (например, готовая сборка) — кладём рядом с .exe.
            return AppContext.BaseDirectory;
        }

        public static string ConnectionString => new SqliteConnectionStringBuilder
        {
            DataSource = DbPath,
            ForeignKeys = true
        }.ToString();

        public static SqliteConnection OpenConnection()
        {
            var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            return connection;
        }

        public static void Initialize()
        {
            using var connection = OpenConnection();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = SchemaSql;
                command.ExecuteNonQuery();
            }

            ApplyMigrations(connection);
            SeedReferenceData(connection);
        }

        private static void ApplyMigrations(SqliteConnection connection)
        {
            EnsureColumn(connection, "Certificates", "Comment", "TEXT");
            EnsureColumn(connection, "Certificates", "NotifiedThresholds", "TEXT");
            EnsureColumn(connection, "Certificates", "TelegramUsername", "TEXT");
            EnsureColumn(connection, "Certificates", "TelegramChatId", "TEXT");
        }

        private static void EnsureColumn(SqliteConnection connection, string table, string column, string type)
        {
            bool exists = false;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA table_info(" + table + ");";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }
            }

            if (!exists)
            {
                using var alter = connection.CreateCommand();
                alter.CommandText = "ALTER TABLE " + table + " ADD COLUMN " + column + " " + type + ";";
                alter.ExecuteNonQuery();
            }
        }

        private const string SchemaSql = @"
PRAGMA foreign_keys = ON;

-- 1. Справочник подразделений
CREATE TABLE IF NOT EXISTS Departments (
    Id   INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE
);

-- 2. Сотрудники
CREATE TABLE IF NOT EXISTS Employees (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName     TEXT NOT NULL,
    DepartmentId INTEGER NULL REFERENCES Departments(Id) ON DELETE SET NULL,
    Position     TEXT,
    Email        TEXT
);

-- 3. Удостоверяющие центры (УЦ), выдавшие сертификат
CREATE TABLE IF NOT EXISTS CertificateAuthorities (
    Id   INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    Inn  TEXT
);

-- 4. Типы сертификатов (справочник)
CREATE TABLE IF NOT EXISTS CertificateTypes (
    Id   INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE
);

-- 5. Реестр выданных ЭЦП
CREATE TABLE IF NOT EXISTS Certificates (
    Id                INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName          TEXT NOT NULL,
    EmployeeId        INTEGER NULL REFERENCES Employees(Id) ON DELETE SET NULL,
    DepartmentId      INTEGER NULL REFERENCES Departments(Id) ON DELETE SET NULL,
    AuthorityId       INTEGER NULL REFERENCES CertificateAuthorities(Id) ON DELETE SET NULL,
    CertificateTypeId INTEGER NULL REFERENCES CertificateTypes(Id) ON DELETE SET NULL,
    SerialNumber      TEXT,
    StoreLogin        TEXT,
    StorePassword     TEXT,
    CertPassword      TEXT,
    IssueDate         TEXT NOT NULL,
    ExpiryDate        TEXT NOT NULL,
    Comment           TEXT,
    NotifiedThresholds TEXT,
    TelegramUsername  TEXT,
    TelegramChatId    TEXT
);

-- 6. Справочник типов получения заявки
CREATE TABLE IF NOT EXISTS RequestTypes (
    Id   INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE
);

-- 7. Справочник статусов заявки
CREATE TABLE IF NOT EXISTS RequestStatuses (
    Id   INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE
);

-- 8. Заявки на получение ЭЦП
CREATE TABLE IF NOT EXISTS Requests (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName        TEXT NOT NULL,
    EmployeeId      INTEGER NULL REFERENCES Employees(Id) ON DELETE SET NULL,
    RequestNumber   TEXT NOT NULL UNIQUE,
    RequestType     TEXT NOT NULL,
    RequestTypeId   INTEGER NULL REFERENCES RequestTypes(Id) ON DELETE SET NULL,
    RequestStatusId INTEGER NULL REFERENCES RequestStatuses(Id) ON DELETE SET NULL,
    CreatedDate     TEXT NOT NULL,
    CertificateId   INTEGER NULL REFERENCES Certificates(Id) ON DELETE SET NULL
);

-- Индексы по внешним ключам для ускорения соединений
CREATE INDEX IF NOT EXISTS IX_Employees_Department ON Employees(DepartmentId);
CREATE INDEX IF NOT EXISTS IX_Certificates_Employee ON Certificates(EmployeeId);
CREATE INDEX IF NOT EXISTS IX_Certificates_Department ON Certificates(DepartmentId);
CREATE INDEX IF NOT EXISTS IX_Certificates_Authority ON Certificates(AuthorityId);
CREATE INDEX IF NOT EXISTS IX_Certificates_Type ON Certificates(CertificateTypeId);
CREATE INDEX IF NOT EXISTS IX_Requests_Employee ON Requests(EmployeeId);
CREATE INDEX IF NOT EXISTS IX_Requests_Type ON Requests(RequestTypeId);
CREATE INDEX IF NOT EXISTS IX_Requests_Status ON Requests(RequestStatusId);
";

        private static void SeedReferenceData(SqliteConnection connection)
        {
            SeedLookup(connection, "Departments", new[]
            {
                "Регистратура",
                "Регистратура детская",
                "Приёмная",
                "Экономисты",
                "Отдел кадров",
                "Лаборатория",
                "Хирург (14 каб.)",
                "Женская консультация",
                "Психиатр",
                "Нарколог",
                "Стоматология",
                "Зубопротезная",
                "Терапевты Каб. №27,28",
                "Невролог",
                "Пищеблок",
                "Скорая",
                "Приёмный покой",
                "Эндоскописты",
                "УЗИ",
                "ЭКГ",
                "Хирургическое отделение",
                "Терапевтическое отделение",
                "Детское отделение"
            });

            SeedLookup(connection, "CertificateAuthorities", new[]
            {
                "ФНС России",
                "Федеральное казначейство",
                "АО «Аналитический центр»",
                "УЦ Сбербанка"
            });

            SeedLookup(connection, "CertificateTypes", new[]
            {
                "Руководителя",
                "Сотрудника",
                "Для ЭДО",
                "Для сдачи отчётности"
            });

            SeedLookup(connection, "RequestTypes", new[]
            {
                "Новый",
                "Замена",
                "Обновление данных"
            });

            SeedLookup(connection, "RequestStatuses", new[]
            {
                "Новая",
                "В работе",
                "Завершена",
                "Отклонена"
            });
        }

        private static void SeedLookup(SqliteConnection connection, string table, string[] values)
        {
            using (var check = connection.CreateCommand())
            {
                check.CommandText = "SELECT COUNT(*) FROM " + table + ";";
                long count = Convert.ToInt64(check.ExecuteScalar());
                if (count > 0)
                {
                    return;
                }
            }

            using var transaction = connection.BeginTransaction();
            foreach (var value in values)
            {
                using var insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText = "INSERT INTO " + table + " (Name) VALUES ($name);";
                insert.Parameters.AddWithValue("$name", value);
                insert.ExecuteNonQuery();
            }
            transaction.Commit();
        }
    }
}