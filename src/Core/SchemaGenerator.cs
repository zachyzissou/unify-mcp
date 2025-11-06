using System;
using System.Collections.Concurrent;
using System.Reflection;
using NJsonSchema;
using NJsonSchema.Generation;

namespace UnifyMcp.Core
{
    /// <summary>
    /// Generates JSON schemas from C# method parameters using NJsonSchema.
    /// Integrates with ModelContextProtocol SDK attribute system and caches generated schemas.
    /// </summary>
    public class SchemaGenerator
    {
        private readonly ConcurrentDictionary<Type, JsonSchema> schemaCache;
        private readonly JsonSchemaGenerator generator;

        public SchemaGenerator()
        {
            schemaCache = new ConcurrentDictionary<Type, JsonSchema>();

            var settings = new JsonSchemaGeneratorSettings
            {
                DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull,
                GenerateAbstractProperties = true,
                FlattenInheritanceHierarchy = false
            };

            generator = new JsonSchemaGenerator(settings);
        }

        /// <summary>
        /// Generates JSON schema for a method's parameters.
        /// </summary>
        /// <param name="method">Method to generate schema for</param>
        /// <returns>JSON schema as string</returns>
        public string GenerateMethodSchema(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            var parameters = method.GetParameters();
            if (parameters.Length == 0)
            {
                return "{}"; // Empty schema for parameterless methods
            }

            // For single parameter, generate schema for that type
            if (parameters.Length == 1)
            {
                return GenerateTypeSchema(parameters[0].ParameterType);
            }

            // For multiple parameters, create composite schema
            // TODO: Implement composite schema generation in Phase 4
            return "{}";
        }

        /// <summary>
        /// Generates JSON schema for a type.
        /// Uses cache to avoid regenerating schemas.
        /// </summary>
        /// <param name="type">Type to generate schema for</param>
        /// <returns>JSON schema as string</returns>
        public string GenerateTypeSchema(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var schema = schemaCache.GetOrAdd(type, t => generator.Generate(t));
            return schema.ToJson();
        }

        /// <summary>
        /// Clears the schema cache.
        /// </summary>
        public void ClearCache()
        {
            schemaCache.Clear();
        }

        /// <summary>
        /// Gets the number of cached schemas.
        /// </summary>
        public int CachedSchemaCount => schemaCache.Count;
    }
}
