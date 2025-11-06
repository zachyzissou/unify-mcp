using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace UnifyMcp.Tools.Documentation
{
    /// <summary>
    /// Indexes Unity API documentation using SQLite FTS5 for full-text search.
    /// Implements: CREATE VIRTUAL TABLE using FTS5, porter tokenizer for stemming, BM25 ranking
    /// </summary>
    public class UnityDocumentationIndexer : IDisposable
    {
        private readonly string databasePath;
        private SQLiteConnection connection;

        /// <summary>
        /// Creates a new UnityDocumentationIndexer with the specified database path
        /// </summary>
        /// <param name="databasePath">Path to SQLite database file</param>
        public UnityDocumentationIndexer(string databasePath)
        {
            this.databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
        }

        /// <summary>
        /// Creates the SQLite database with FTS5 virtual table and metadata table
        /// </summary>
        public void CreateDatabase()
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create connection
            connection = new SQLiteConnection($"Data Source={databasePath};Version=3;");
            connection.Open();

            // Create FTS5 virtual table with porter tokenizer for stemming
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    CREATE VIRTUAL TABLE IF NOT EXISTS documentation_fts5 USING fts5(
                        class_name,
                        method_name,
                        return_type,
                        parameters,
                        description,
                        code_examples,
                        unity_version,
                        documentation_url,
                        is_deprecated,
                        replacement_api,
                        tokenize='porter'
                    );
                ";
                command.ExecuteNonQuery();
            }

            // Create metadata table for version tracking
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS metadata (
                        key TEXT PRIMARY KEY,
                        value TEXT,
                        last_updated DATETIME DEFAULT CURRENT_TIMESTAMP
                    );
                ";
                command.ExecuteNonQuery();
            }

            // Insert schema version
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT OR REPLACE INTO metadata (key, value)
                    VALUES ('schema_version', '1.0');
                ";
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Indexes a documentation entry into the FTS5 table
        /// </summary>
        /// <param name="entry">Documentation entry to index</param>
        public void IndexDocument(DocumentationEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            EnsureConnectionOpen();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO documentation_fts5 (
                        class_name,
                        method_name,
                        return_type,
                        parameters,
                        description,
                        code_examples,
                        unity_version,
                        documentation_url,
                        is_deprecated,
                        replacement_api
                    ) VALUES (
                        @className,
                        @methodName,
                        @returnType,
                        @parameters,
                        @description,
                        @codeExamples,
                        @unityVersion,
                        @documentationUrl,
                        @isDeprecated,
                        @replacementApi
                    );
                ";

                command.Parameters.AddWithValue("@className", entry.ClassName ?? string.Empty);
                command.Parameters.AddWithValue("@methodName", entry.MethodName ?? string.Empty);
                command.Parameters.AddWithValue("@returnType", entry.ReturnType ?? string.Empty);
                command.Parameters.AddWithValue("@parameters", entry.GetParameterList());
                command.Parameters.AddWithValue("@description", entry.Description ?? string.Empty);
                command.Parameters.AddWithValue("@codeExamples",
                    entry.CodeExamples != null ? string.Join("\n", entry.CodeExamples) : string.Empty);
                command.Parameters.AddWithValue("@unityVersion", entry.UnityVersion ?? string.Empty);
                command.Parameters.AddWithValue("@documentationUrl", entry.DocumentationUrl ?? string.Empty);
                command.Parameters.AddWithValue("@isDeprecated", entry.IsDeprecated ? "1" : "0");
                command.Parameters.AddWithValue("@replacementApi", entry.ReplacementApi ?? string.Empty);

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Queries the documentation index using FTS5 with BM25 ranking
        /// </summary>
        /// <param name="query">Search query (supports FTS5 syntax)</param>
        /// <returns>List of matching documentation entries ordered by relevance</returns>
        public List<DocumentationEntry> QueryDocumentation(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<DocumentationEntry>();

            EnsureConnectionOpen();

            var results = new List<DocumentationEntry>();

            using (var command = connection.CreateCommand())
            {
                // Use FTS5 MATCH for full-text search with BM25 ranking
                command.CommandText = @"
                    SELECT
                        class_name,
                        method_name,
                        return_type,
                        parameters,
                        description,
                        code_examples,
                        unity_version,
                        documentation_url,
                        is_deprecated,
                        replacement_api,
                        rank
                    FROM documentation_fts5
                    WHERE documentation_fts5 MATCH @query
                    ORDER BY rank;
                ";

                command.Parameters.AddWithValue("@query", query);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var entry = new DocumentationEntry
                        {
                            ClassName = reader.GetString(0),
                            MethodName = reader.GetString(1),
                            ReturnType = reader.GetString(2),
                            Parameters = reader.GetString(3).Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries),
                            Description = reader.GetString(4),
                            CodeExamples = reader.GetString(5).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries),
                            UnityVersion = reader.GetString(6),
                            DocumentationUrl = reader.GetString(7),
                            IsDeprecated = reader.GetString(8) == "1",
                            ReplacementApi = reader.GetString(9)
                        };

                        results.Add(entry);
                    }
                }
            }

            return results;
        }

        private void EnsureConnectionOpen()
        {
            if (connection == null || connection.State != System.Data.ConnectionState.Open)
            {
                connection = new SQLiteConnection($"Data Source={databasePath};Version=3;");
                connection.Open();
            }
        }

        /// <summary>
        /// Disposes of database resources
        /// </summary>
        public void Dispose()
        {
            connection?.Close();
            connection?.Dispose();
            connection = null;
        }
    }
}
