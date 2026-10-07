using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using WindowsFormsApp1.Models;

namespace WindowsFormsApp1.Data
{
    public class RequestRepository
    {
        private const string DateFormat = "yyyy-MM-dd";

        public List<Request> GetAll()
        {
            var result = new List<Request>();
            using var connection = Database.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT r.Id, r.FullName, r.RequestNumber, r.RequestType, IFNULL(s.Name, '') AS Status, r.CreatedDate
FROM Requests r
LEFT JOIN RequestStatuses s ON s.Id = r.RequestStatusId
ORDER BY r.Id DESC;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(Map(reader));
            }
            return result;
        }

        public long Add(Request request)
        {
            using var connection = Database.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
INSERT INTO Requests (FullName, RequestNumber, RequestType, RequestStatusId, CreatedDate)
VALUES ($fullName, $number, $type, $statusId, $created);";
            AddParameters(connection, command, request);
            command.ExecuteNonQuery();
            using var idCommand = connection.CreateCommand();
            idCommand.CommandText = "SELECT last_insert_rowid();";
            return Convert.ToInt64(idCommand.ExecuteScalar());
        }

        public void Update(Request request)
        {
            using var connection = Database.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
UPDATE Requests SET
    FullName      = $fullName,
    RequestNumber = $number,
    RequestType   = $type,
    RequestStatusId = $statusId,
    CreatedDate   = $created
WHERE Id = $id;";
            AddParameters(connection, command, request);
            command.Parameters.AddWithValue("$id", request.Id);
            command.ExecuteNonQuery();
        }

        public void Delete(long id)
        {
            using var connection = Database.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Requests WHERE Id = $id;";
            command.Parameters.AddWithValue("$id", id);
            command.ExecuteNonQuery();
        }

        public bool NumberExists(string number, long excludeId = 0)
        {
            using var connection = Database.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Requests WHERE RequestNumber = $number AND Id <> $id;";
            command.Parameters.AddWithValue("$number", number);
            command.Parameters.AddWithValue("$id", excludeId);
            return Convert.ToInt64(command.ExecuteScalar()) > 0;
        }

        public List<string> GetStatuses()
        {
            var result = new List<string>();
            using var connection = Database.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Name FROM RequestStatuses ORDER BY Id;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(reader.GetString(0));
            }
            return result;
        }

        private static void AddParameters(SqliteConnection connection, SqliteCommand command, Request request)
        {
            long statusId = GetOrCreateStatusId(connection, request.Status);
            command.Parameters.AddWithValue("$fullName", (object)request.FullName ?? DBNull.Value);
            command.Parameters.AddWithValue("$number", (object)request.RequestNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("$type", (object)request.RequestType ?? DBNull.Value);
            command.Parameters.AddWithValue("$statusId", statusId == 0 ? (object)DBNull.Value : statusId);
            command.Parameters.AddWithValue("$created", request.CreatedDate.ToString(DateFormat));
        }

        private static long GetOrCreateStatusId(SqliteConnection connection, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return 0;
            }

            using (var find = connection.CreateCommand())
            {
                find.CommandText = "SELECT Id FROM RequestStatuses WHERE Name = $name;";
                find.Parameters.AddWithValue("$name", name.Trim());
                var found = find.ExecuteScalar();
                if (found != null)
                {
                    return Convert.ToInt64(found);
                }
            }

            using var insert = connection.CreateCommand();
            insert.CommandText = "INSERT INTO RequestStatuses (Name) VALUES ($name);";
            insert.Parameters.AddWithValue("$name", name.Trim());
            insert.ExecuteNonQuery();
            using var idCommand = connection.CreateCommand();
            idCommand.CommandText = "SELECT last_insert_rowid();";
            return Convert.ToInt64(idCommand.ExecuteScalar());
        }

        private static Request Map(SqliteDataReader reader)
        {
            return new Request
            {
                Id = reader.GetInt64(0),
                FullName = reader.GetString(1),
                RequestNumber = reader.GetString(2),
                RequestType = reader.GetString(3),
                Status = reader.IsDBNull(4) ? "" : reader.GetString(4),
                CreatedDate = DateTime.TryParse(reader.GetString(5), out var d) ? d : DateTime.Today
            };
        }
    }
}
