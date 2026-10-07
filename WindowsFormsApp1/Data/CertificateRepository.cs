using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using WindowsFormsApp1.Models;

namespace WindowsFormsApp1.Data
{
    public class CertificateRepository
    {
        private const string DateFormat = "yyyy-MM-dd";

        private const string SelectSql = @"
SELECT c.Id, c.FullName, IFNULL(d.Name, '') AS Department,
       IFNULL(a.Name, '') AS Authority, IFNULL(t.Name, '') AS CertificateType,
       c.SerialNumber,
       c.StoreLogin, c.StorePassword, c.CertPassword, c.IssueDate, c.ExpiryDate, c.Comment,
       c.NotifiedThresholds, c.TelegramUsername, c.TelegramChatId
FROM Certificates c
LEFT JOIN Departments d ON d.Id = c.DepartmentId
LEFT JOIN CertificateAuthorities a ON a.Id = c.AuthorityId
LEFT JOIN CertificateTypes t ON t.Id = c.CertificateTypeId
";

        public List<Certificate> GetAll()
        {
            var result = new List<Certificate>();
            using var connection = Database.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = SelectSql + "ORDER BY c.Id DESC;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(Map(reader));
            }
            return result;
        }

        public Certificate GetById(long id)
        {
            using var connection = Database.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = SelectSql + "WHERE c.Id = $id;";
            command.Parameters.AddWithValue("$id", id);
            using var reader = command.ExecuteReader();
            return reader.Read() ? Map(reader) : null;
        }

        public long Add(Certificate cert)
        {
            using var connection = Database.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
INSERT INTO Certificates
    (FullName, DepartmentId, AuthorityId, CertificateTypeId, SerialNumber, StoreLogin, StorePassword, CertPassword, IssueDate, ExpiryDate, Comment, TelegramUsername, TelegramChatId)
VALUES
    ($fullName, $departmentId, $authorityId, $typeId, $serial, $login, $storePass, $certPass, $issue, $expiry, $comment, $tgUser, $tgChat);";
            AddParameters(connection, command, cert);
            command.ExecuteNonQuery();
            using var idCommand = connection.CreateCommand();
            idCommand.CommandText = "SELECT last_insert_rowid();";
            return Convert.ToInt64(idCommand.ExecuteScalar());
        }

        public void Update(Certificate cert)
        {
            using var connection = Database.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
UPDATE Certificates SET
    FullName      = $fullName,
    DepartmentId  = $departmentId,
    AuthorityId   = $authorityId,
    CertificateTypeId = $typeId,
    SerialNumber  = $serial,
    StoreLogin    = $login,
    StorePassword = $storePass,
    CertPassword  = $certPass,
    IssueDate     = $issue,
    ExpiryDate    = $expiry,
    Comment       = $comment,
    TelegramUsername = $tgUser,
    TelegramChatId   = $tgChat
WHERE Id = $id;";
            AddParameters(connection, command, cert);
            command.Parameters.AddWithValue("$id", cert.Id);
            command.ExecuteNonQuery();
        }

        public void Delete(long id)
        {
            using var connection = Database.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Certificates WHERE Id = $id;";
            command.Parameters.AddWithValue("$id", id);
            command.ExecuteNonQuery();
        }

        public void UpdateNotifiedThresholds(long id, string notifiedThresholds)
        {
            using var connection = Database.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE Certificates SET NotifiedThresholds = $value WHERE Id = $id;";
            command.Parameters.AddWithValue("$value", (object)notifiedThresholds ?? DBNull.Value);
            command.Parameters.AddWithValue("$id", id);
            command.ExecuteNonQuery();
        }

        public List<string> GetDepartments() => GetLookup("Departments");

        public List<string> GetAuthorities() => GetLookup("CertificateAuthorities");

        public List<string> GetCertificateTypes() => GetLookup("CertificateTypes");

        private static List<string> GetLookup(string table)
        {
            var result = new List<string>();
            using var connection = Database.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Name FROM " + table + " ORDER BY Name;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(reader.GetString(0));
            }
            return result;
        }

        public long GetOrCreateDepartmentId(SqliteConnection connection, string name)
        {
            return GetOrCreateLookupId(connection, "Departments", name);
        }

        private static long GetOrCreateLookupId(SqliteConnection connection, string table, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return 0;
            }

            using (var find = connection.CreateCommand())
            {
                find.CommandText = "SELECT Id FROM " + table + " WHERE Name = $name;";
                find.Parameters.AddWithValue("$name", name.Trim());
                var found = find.ExecuteScalar();
                if (found != null)
                {
                    return Convert.ToInt64(found);
                }
            }

            using var insert = connection.CreateCommand();
            insert.CommandText = "INSERT INTO " + table + " (Name) VALUES ($name);";
            insert.Parameters.AddWithValue("$name", name.Trim());
            insert.ExecuteNonQuery();
            using var idCommand = connection.CreateCommand();
            idCommand.CommandText = "SELECT last_insert_rowid();";
            return Convert.ToInt64(idCommand.ExecuteScalar());
        }

        private void AddParameters(SqliteConnection connection, SqliteCommand command, Certificate cert)
        {
            long departmentId = GetOrCreateDepartmentId(connection, cert.Department);
            long authorityId = GetOrCreateLookupId(connection, "CertificateAuthorities", cert.Authority);
            long typeId = GetOrCreateLookupId(connection, "CertificateTypes", cert.CertificateType);
            command.Parameters.AddWithValue("$fullName", (object)cert.FullName ?? DBNull.Value);
            command.Parameters.AddWithValue("$departmentId", departmentId == 0 ? (object)DBNull.Value : departmentId);
            command.Parameters.AddWithValue("$authorityId", authorityId == 0 ? (object)DBNull.Value : authorityId);
            command.Parameters.AddWithValue("$typeId", typeId == 0 ? (object)DBNull.Value : typeId);
            command.Parameters.AddWithValue("$serial", (object)cert.SerialNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("$login", (object)cert.StoreLogin ?? DBNull.Value);
            command.Parameters.AddWithValue("$storePass", (object)cert.StorePassword ?? DBNull.Value);
            command.Parameters.AddWithValue("$certPass", (object)cert.CertPassword ?? DBNull.Value);
            command.Parameters.AddWithValue("$issue", cert.IssueDate.ToString(DateFormat));
            command.Parameters.AddWithValue("$expiry", cert.ExpiryDate.ToString(DateFormat));
            command.Parameters.AddWithValue("$comment", (object)cert.Comment ?? DBNull.Value);
            command.Parameters.AddWithValue("$tgUser", (object)cert.TelegramUsername ?? DBNull.Value);
            command.Parameters.AddWithValue("$tgChat", (object)cert.TelegramChatId ?? DBNull.Value);
        }

        private static Certificate Map(SqliteDataReader reader)
        {
            return new Certificate
            {
                Id = reader.GetInt64(0),
                FullName = reader.GetString(1),
                Department = reader.GetString(2),
                Authority = reader.GetString(3),
                CertificateType = reader.GetString(4),
                SerialNumber = reader.IsDBNull(5) ? "" : reader.GetString(5),
                StoreLogin = reader.IsDBNull(6) ? "" : reader.GetString(6),
                StorePassword = reader.IsDBNull(7) ? "" : reader.GetString(7),
                CertPassword = reader.IsDBNull(8) ? "" : reader.GetString(8),
                IssueDate = ParseDate(reader.GetString(9)),
                ExpiryDate = ParseDate(reader.GetString(10)),
                Comment = reader.IsDBNull(11) ? "" : reader.GetString(11),
                NotifiedThresholds = reader.IsDBNull(12) ? "" : reader.GetString(12),
                TelegramUsername = reader.IsDBNull(13) ? "" : reader.GetString(13),
                TelegramChatId = reader.IsDBNull(14) ? "" : reader.GetString(14)
            };
        }

        private static DateTime ParseDate(string value)
        {
            return DateTime.TryParse(value, out var date) ? date : DateTime.Today;
        }
    }
}
