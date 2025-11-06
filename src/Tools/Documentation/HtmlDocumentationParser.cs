using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;

namespace UnifyMcp.Tools.Documentation
{
    /// <summary>
    /// Parses Unity ScriptReference HTML pages using AngleSharp.
    /// Extracts class.method signatures, parameters with types, return types, descriptions, code examples.
    /// </summary>
    public class HtmlDocumentationParser
    {
        private readonly HtmlParser htmlParser;

        public HtmlDocumentationParser()
        {
            htmlParser = new HtmlParser();
        }

        /// <summary>
        /// Parses Unity documentation HTML and extracts structured documentation entry
        /// </summary>
        /// <param name="html">HTML content of Unity ScriptReference page</param>
        /// <param name="documentationUrl">URL of the documentation page</param>
        /// <returns>DocumentationEntry with extracted information, or null if parsing fails</returns>
        public DocumentationEntry ParseHtml(string html, string documentationUrl)
        {
            if (string.IsNullOrWhiteSpace(html))
                return null;

            try
            {
                var document = htmlParser.ParseDocument(html);

                // Extract basic information
                var entry = new DocumentationEntry
                {
                    DocumentationUrl = documentationUrl,
                    LastUpdated = DateTime.UtcNow
                };

                // Extract title (e.g., "Unity - Scripting API: Transform.Translate")
                var title = document.QuerySelector("title")?.TextContent ?? "";
                ParseTitleForClassAndMethod(title, entry);

                // Try h1 as fallback for class.method
                if (string.IsNullOrEmpty(entry.ClassName) || string.IsNullOrEmpty(entry.MethodName))
                {
                    var h1 = document.QuerySelector("h1")?.TextContent ?? "";
                    ParseTitleForClassAndMethod(h1, entry);
                }

                // Extract signature (return type, parameters)
                var signature = document.QuerySelector(".signature code, div.signature code")?.TextContent ?? "";
                if (!string.IsNullOrEmpty(signature))
                {
                    ParseSignature(signature, entry);
                }

                // Extract description
                var descriptionElements = document.QuerySelectorAll(".description p, div.description p");
                var descriptionParts = new List<string>();
                foreach (var elem in descriptionElements)
                {
                    var text = elem.TextContent?.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                        descriptionParts.Add(text);
                }
                entry.Description = string.Join(" ", descriptionParts);

                // Check for static methods in description or signature
                if (signature.Contains("static") || entry.Description?.Contains("static") == true)
                {
                    // Add static note to description if not already present
                    if (entry.Description != null && !entry.Description.Contains("static"))
                    {
                        entry.Description = "(static) " + entry.Description;
                    }
                }

                // Extract code examples
                var codeExamples = new List<string>();
                var exampleElements = document.QuerySelectorAll(".example pre code, div.example pre code, pre code");
                foreach (var codeElem in exampleElements)
                {
                    var code = codeElem.TextContent?.Trim();
                    if (!string.IsNullOrWhiteSpace(code) && code.Length > 10) // Filter out very short snippets
                    {
                        codeExamples.Add(code);
                    }
                }
                entry.CodeExamples = codeExamples.ToArray();

                // Extract parameter details
                var parameterElements = document.QuerySelectorAll(".parameters .parameter, div.parameters div.parameter");
                if (parameterElements.Any())
                {
                    var paramList = new List<string>();
                    foreach (var paramElem in parameterElements)
                    {
                        var paramName = paramElem.QuerySelector(".name, span.name")?.TextContent?.Trim() ?? "";
                        var paramType = paramElem.QuerySelector(".type, span.type")?.TextContent?.Trim() ?? "";

                        if (!string.IsNullOrEmpty(paramName) && !string.IsNullOrEmpty(paramType))
                        {
                            paramList.Add($"{paramType} {paramName}");
                        }
                    }

                    // Use detailed parameter info if available, otherwise keep signature parameters
                    if (paramList.Count > 0)
                    {
                        entry.Parameters = paramList.ToArray();
                    }
                }

                // Check for deprecation
                var deprecatedElement = document.QuerySelector(".deprecated-message, div.deprecated-message");
                if (deprecatedElement != null)
                {
                    entry.IsDeprecated = true;
                    var deprecatedText = deprecatedElement.TextContent ?? "";

                    // Try to extract replacement API
                    // Look for patterns like "Use NewMethod instead" or "Use ClassName.NewMethod"
                    var replacementMatch = Regex.Match(deprecatedText,
                        @"[Uu]se\s+([A-Za-z0-9_.]+)\s+instead",
                        RegexOptions.IgnoreCase);

                    if (replacementMatch.Success)
                    {
                        entry.ReplacementApi = replacementMatch.Groups[1].Value;
                    }
                }

                // Validate we extracted meaningful data
                if (string.IsNullOrEmpty(entry.ClassName) && string.IsNullOrEmpty(entry.Description))
                {
                    return null; // Not enough data extracted
                }

                return entry;
            }
            catch (Exception)
            {
                // Failed to parse HTML
                return null;
            }
        }

        /// <summary>
        /// Parses title or h1 text to extract class name and method name.
        /// Examples:
        /// - "Unity - Scripting API: Transform.Translate" → Transform, Translate
        /// - "Transform.position" → Transform, position
        /// - "GameObject.GetComponent" → GameObject, GetComponent
        /// </summary>
        private void ParseTitleForClassAndMethod(string title, DocumentationEntry entry)
        {
            if (string.IsNullOrEmpty(title))
                return;

            // Extract the last part after colon (if present)
            var parts = title.Split(':');
            var apiPart = parts[parts.Length - 1].Trim();

            // Split by dot to get Class.Method
            var dotIndex = apiPart.LastIndexOf('.');
            if (dotIndex > 0 && dotIndex < apiPart.Length - 1)
            {
                entry.ClassName = apiPart.Substring(0, dotIndex);
                entry.MethodName = apiPart.Substring(dotIndex + 1);
            }
        }

        /// <summary>
        /// Parses method signature to extract return type and parameters.
        /// Examples:
        /// - "public void Translate(Vector3 translation, Space relativeTo = Space.Self);"
        /// - "public Vector3 position { get; set; }"
        /// - "public static GameObject Find(string name);"
        /// - "public T GetComponent&lt;T&gt;() where T : Component;"
        /// </summary>
        private void ParseSignature(string signature, DocumentationEntry entry)
        {
            if (string.IsNullOrEmpty(signature))
                return;

            // Clean up signature
            signature = signature.Trim();

            // Check if it's a property (contains { get; set; })
            bool isProperty = signature.Contains("{") && signature.Contains("}");

            if (isProperty)
            {
                // Parse property signature: "public Vector3 position { get; set; }"
                ParsePropertySignature(signature, entry);
            }
            else
            {
                // Parse method signature
                ParseMethodSignature(signature, entry);
            }
        }

        /// <summary>
        /// Parses property signature: "public Vector3 position { get; set; }"
        /// </summary>
        private void ParsePropertySignature(string signature, DocumentationEntry entry)
        {
            // Remove "public", "static", "virtual", etc.
            var cleaned = Regex.Replace(signature, @"\b(public|private|protected|internal|static|virtual|override|abstract)\b", "").Trim();

            // Remove { get; set; } part
            cleaned = Regex.Replace(cleaned, @"\{[^}]*\}", "").Trim();

            // Now should have: "Vector3 position"
            var parts = cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                entry.ReturnType = parts[0];
                // Property name might be in parts[1] or entry.MethodName from title
            }

            // Properties have no parameters
            entry.Parameters = Array.Empty<string>();
        }

        /// <summary>
        /// Parses method signature: "public void Translate(Vector3 translation, Space relativeTo = Space.Self);"
        /// </summary>
        private void ParseMethodSignature(string signature, DocumentationEntry entry)
        {
            // Find method name and return type
            // Pattern: [modifiers] returnType methodName(parameters)

            // Remove semicolon
            signature = signature.TrimEnd(';').Trim();

            // Find opening parenthesis
            var openParenIndex = signature.IndexOf('(');
            if (openParenIndex < 0)
            {
                // No parameters, just extract return type
                ExtractReturnType(signature, entry);
                entry.Parameters = Array.Empty<string>();
                return;
            }

            // Extract return type from before method name
            var beforeParen = signature.Substring(0, openParenIndex);
            ExtractReturnType(beforeParen, entry);

            // Extract parameters
            var closeParenIndex = signature.LastIndexOf(')');
            if (closeParenIndex > openParenIndex)
            {
                var paramString = signature.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1).Trim();
                ParseParameters(paramString, entry);
            }
        }

        /// <summary>
        /// Extracts return type from signature part before method name
        /// Example: "public static GameObject Find" → "GameObject"
        /// </summary>
        private void ExtractReturnType(string signaturePart, DocumentationEntry entry)
        {
            // Remove modifiers
            var cleaned = Regex.Replace(signaturePart, @"\b(public|private|protected|internal|static|virtual|override|abstract|sealed|readonly|const)\b", "").Trim();

            // Remove generic constraints (where T : Component)
            if (cleaned.Contains(" where "))
            {
                cleaned = cleaned.Substring(0, cleaned.IndexOf(" where ")).Trim();
            }

            // Split by whitespace
            var parts = cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Return type is typically second-to-last part (last is method name)
            if (parts.Length >= 2)
            {
                entry.ReturnType = parts[parts.Length - 2];
            }
            else if (parts.Length == 1)
            {
                // Might be just return type, or just method name - assume return type
                entry.ReturnType = parts[0];
            }
        }

        /// <summary>
        /// Parses parameter list: "Vector3 translation, Space relativeTo = Space.Self"
        /// </summary>
        private void ParseParameters(string paramString, DocumentationEntry entry)
        {
            if (string.IsNullOrWhiteSpace(paramString))
            {
                entry.Parameters = Array.Empty<string>();
                return;
            }

            // Split by comma (but not commas inside < >)
            var parameters = new List<string>();
            int depth = 0;
            int lastSplit = 0;

            for (int i = 0; i < paramString.Length; i++)
            {
                if (paramString[i] == '<')
                    depth++;
                else if (paramString[i] == '>')
                    depth--;
                else if (paramString[i] == ',' && depth == 0)
                {
                    parameters.Add(paramString.Substring(lastSplit, i - lastSplit).Trim());
                    lastSplit = i + 1;
                }
            }

            // Add last parameter
            parameters.Add(paramString.Substring(lastSplit).Trim());

            // Clean up each parameter (remove default values)
            var cleanedParams = new List<string>();
            foreach (var param in parameters)
            {
                if (string.IsNullOrWhiteSpace(param))
                    continue;

                // Remove default value (everything after =)
                var paramWithoutDefault = param.Split('=')[0].Trim();
                cleanedParams.Add(paramWithoutDefault);
            }

            entry.Parameters = cleanedParams.ToArray();
        }
    }
}
