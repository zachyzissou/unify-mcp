using System;

namespace UnifyMcp.Tools.Documentation
{
    /// <summary>
    /// Parses Unity ScriptReference HTML pages using AngleSharp.
    /// Extracts class.method signatures, parameters with types, return types, descriptions, code examples.
    /// </summary>
    public class HtmlDocumentationParser
    {
        /// <summary>
        /// Parses Unity documentation HTML and extracts structured documentation entry
        /// </summary>
        /// <param name="html">HTML content of Unity ScriptReference page</param>
        /// <param name="documentationUrl">URL of the documentation page</param>
        /// <returns>DocumentationEntry with extracted information, or null if parsing fails</returns>
        public DocumentationEntry ParseHtml(string html, string documentationUrl)
        {
            // TODO: Implement in S016
            throw new NotImplementedException("HtmlDocumentationParser.ParseHtml - to be implemented in S016");
        }
    }
}
