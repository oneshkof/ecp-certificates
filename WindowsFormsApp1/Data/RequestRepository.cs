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
SELECT Id, FullName, RequestNumber, RequestType, CreatedDate
FROM Requests
ORDER BY Id DESC;";
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
INSERT INTO Requests (FullName, RequestNumber, RequestType, CreatedDate)
VALUES ($fullName, $number, $type, $created);";
            AddParameters(command, request);
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
    CreatedDate   = $created
WHERE Id = $id;";
            AddParameters(command, request);
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

        private static void AddParameters(SqliteCommand command, Request request)
        {
            command.Parameters.AddWithValue("$fullName", (object)request.FullName ?? DBNull.Value);
            command.Parameters.AddWithValue("$number", (object)request.RequestNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("$type", (object)request.RequestType ?? DBNull.Value);
            command.Parameters.AddWithValue("$created", request.CreatedDate.ToString(DateFormat));
        }

        private static Request Map(SqliteDataReader reader)
        {
            return new Request
            {
                Id = reader.GetInt64(0),
                FullName = reader.GetString(1),
                RequestNumber = reader.GetString(2),
                RequestType = reader.GetString(3),
                CreatedDate = DateTime.TryParse(reader.GetString(4), out var d) ? d : DateTime.Today
            };
        }
    }
}