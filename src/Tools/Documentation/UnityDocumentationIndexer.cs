using System;
using System.Collections.Generic;

namespace UnifyMcp.Tools.Documentation
{
    /// <summary>
    /// Indexes Unity API documentation using SQLite FTS5 for full-text search.
    /// Implements: CREATE VIRTUAL TABLE using FTS5, porter tokenizer for stemming, BM25 ranking
    /// </summary>
    public class UnityDocumentationIndexer : IDisposable
    {
        private readonly string databasePath;

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
            // TODO: Implement in S014
            throw new NotImplementedException("UnityDocumentationIndexer.CreateDatabase - to be implemented in S014");
        }

        /// <summary>
        /// Indexes a documentation entry into the FTS5 table
        /// </summary>
        /// <param name="entry">Documentation entry to index</param>
        public void IndexDocument(DocumentationEntry entry)
        {
            // TODO: Implement in S014
            throw new NotImplementedException("UnityDocumentationIndexer.IndexDocument - to be implemented in S014");
        }

        /// <summary>
        /// Queries the documentation index using FTS5 with BM25 ranking
        /// </summary>
        /// <param name="query">Search query (supports FTS5 syntax)</param>
        /// <returns>List of matching documentation entries ordered by relevance</returns>
        public List<DocumentationEntry> QueryDocumentation(string query)
        {
            // TODO: Implement in S014
            throw new NotImplementedException("UnityDocumentationIndexer.QueryDocumentation - to be implemented in S014");
        }

        /// <summary>
        /// Disposes of database resources
        /// </summary>
        public void Dispose()
        {
            // TODO: Implement proper cleanup in S014
        }
    }
}
