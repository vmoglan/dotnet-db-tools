using CommandLine;
using Dapper;
using DbTools.Sqlite.Core.Encryption;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DbTools.Sqlite.PerformanceAssessment
{
    static class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    if (!string.IsNullOrEmpty(o.Password))
                    {
                        EncryptionUtils.encrypt(o.DataSource, o.Password);
                    }

                    var stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine("Starting SQLite performance assessment...");
                    stringBuilder.AppendLine("Underlying SQLite engine: SQLCipher");
                    stringBuilder.AppendLine($"Memory security enabled: {!o.IsMemorySecurityOff}");
                    stringBuilder.AppendLine($"Number of records to write and read: {o.RecordCount}");
                    Console.WriteLine(stringBuilder.ToString());

                    var connectionString = new SqliteConnectionStringBuilder
                    {
                        DataSource = o.DataSource,
                        Password = o.Password,
                        Mode = SqliteOpenMode.ReadWriteCreate
                    }.ConnectionString;

                    using var connection = new SqliteConnection(connectionString);
                    connection.Open();

                    if (o.IsMemorySecurityOff)
                    {
                        connection.Execute("PRAGMA cipher_memory_security = OFF;");
                    }

                    using var transaction = connection.BeginTransaction();

                    TimeSpan dbCreationTime = GetOperationExecutionTime(() => transaction.CreateTableIfNotExists());
                    string logEntry = $"Creating database took {dbCreationTime.TotalSeconds} seconds.";
                    Console.WriteLine(logEntry);
                    stringBuilder.AppendLine(logEntry);

                    TimeSpan insertionTime = GetOperationExecutionTime(() => transaction.PopulateTable(o.RecordCount));
                    logEntry = $"Inserting {o.RecordCount} took {insertionTime.TotalSeconds} seconds.";
                    Console.WriteLine(logEntry);
                    stringBuilder.AppendLine(logEntry);

                    transaction.Commit();

                    TimeSpan readTime = GetOperationExecutionTime(() => connection.ReadTable());
                    logEntry = $"Reading {o.RecordCount} took {readTime} seconds.";
                    Console.WriteLine(logEntry);
                    stringBuilder.AppendLine(logEntry);

                    if (!string.IsNullOrEmpty(o.OutputPath))
                    {
                        File.WriteAllText(o.OutputPath, stringBuilder.ToString());
                    }
                });
        }

        static void CreateTableIfNotExists(this IDbTransaction transaction)
        {
            var sql = @"CREATE TABLE IF NOT EXISTS Entity (
                            Id INTEGER PRIMARY KEY,
                            Name TEXT,
                            Value TEXT
                        )";

            transaction.Connection.Execute(sql, transaction);
        }

        static void PopulateTable(this IDbTransaction transaction, int recordCount)
        {
            for (int i = 0; i < recordCount; i++)
            {
                var entity = new Entity
                {
                    Id = i,
                    Name = $"Entity #{i}",
                    Value = 2.33333M
                };
                var sql = @"INSERT INTO Entity
                            VALUES (@Id, @Name, @Value)";
                transaction.Connection.Execute(sql, entity, transaction);
            }
        }

        static IEnumerable<Entity> ReadTable(this SqliteConnection connection)
        {
            var sql = "SELECT * FROM Entity";
            return connection.Query<Entity>(sql);
        }

        static TimeSpan GetOperationExecutionTime(Action a)
        {
            var watch = Stopwatch.StartNew();
            a();
            watch.Stop();
            return watch.Elapsed;
        }
    }
}
