using NUnit.Framework;
using System.Linq;
using UnityMcp.Tools.Documentation;

namespace UnifyMcp.Tests.Documentation
{
    /// <summary>
    /// Tests for HtmlDocumentationParser using AngleSharp to parse Unity ScriptReference pages.
    /// Tests extraction of method signatures, parameters, descriptions, and code examples.
    /// </summary>
    [TestFixture]
    public class HtmlDocumentationParserTests
    {
        private HtmlDocumentationParser parser;

        [SetUp]
        public void SetUp()
        {
            parser = new HtmlDocumentationParser();
        }

        [Test]
        public void ParseHtml_TransformTranslate_ShouldExtractMethodSignature()
        {
            // Arrange - Sample Unity Transform.Translate documentation HTML
            var html = @"
                <html>
                <head><title>Unity - Scripting API: Transform.Translate</title></head>
                <body>
                    <div class='section'>
                        <h1>Transform.Translate</h1>
                        <div class='signature'>
                            <code>public void Translate(Vector3 translation, Space relativeTo = Space.Self);</code>
                        </div>
                        <div class='description'>
                            <p>Moves the transform in the direction and distance of translation.</p>
                        </div>
                    </div>
                </body>
                </html>";

            // Act
            var entry = parser.ParseHtml(html, "https://docs.unity3d.com/ScriptReference/Transform.Translate.html");

            // Assert
            Assert.IsNotNull(entry, "Should return a DocumentationEntry");
            Assert.AreEqual("Transform", entry.ClassName);
            Assert.AreEqual("Translate", entry.MethodName);
            Assert.AreEqual("void", entry.ReturnType);
            Assert.IsNotNull(entry.Parameters);
            Assert.AreEqual(2, entry.Parameters.Length, "Should extract 2 parameters");
        }

        [Test]
        public void ParseHtml_WithDescription_ShouldExtractText()
        {
            // Arrange
            var html = @"
                <html>
                <body>
                    <div class='description'>
                        <p>Moves the transform in the direction and distance of translation.</p>
                        <p>The translation is relative to the specified coordinate system.</p>
                    </div>
                </body>
                </html>";

            // Act
            var entry = parser.ParseHtml(html, "test-url");

            // Assert
            Assert.IsNotNull(entry.Description);
            StringAssert.Contains("Moves the transform", entry.Description);
            StringAssert.Contains("relative to the specified coordinate system", entry.Description);
        }

        [Test]
        public void ParseHtml_WithCodeExample_ShouldExtractExample()
        {
            // Arrange
            var html = @"
                <html>
                <body>
                    <div class='example'>
                        <pre><code>
                        void Update()
                        {
                            transform.Translate(Vector3.forward * Time.deltaTime);
                        }
                        </code></pre>
                    </div>
                </body>
                </html>";

            // Act
            var entry = parser.ParseHtml(html, "test-url");

            // Assert
            Assert.IsNotNull(entry.CodeExamples);
            Assert.IsNotEmpty(entry.CodeExamples, "Should extract code examples");
            StringAssert.Contains("transform.Translate", entry.CodeExamples[0]);
            StringAssert.Contains("Vector3.forward", entry.CodeExamples[0]);
        }

        [Test]
        public void ParseHtml_MultipleCodeExamples_ShouldExtractAll()
        {
            // Arrange
            var html = @"
                <html>
                <body>
                    <div class='example'>
                        <pre><code>transform.Translate(0, 0, 1);</code></pre>
                    </div>
                    <div class='example'>
                        <pre><code>transform.Translate(Vector3.up * speed);</code></pre>
                    </div>
                </body>
                </html>";

            // Act
            var entry = parser.ParseHtml(html, "test-url");

            // Assert
            Assert.AreEqual(2, entry.CodeExamples.Length, "Should extract both code examples");
        }

        [Test]
        public void ParseHtml_PropertyWithGetterSetter_ShouldExtractCorrectly()
        {
            // Arrange - Sample Unity Transform.position property
            var html = @"
                <html>
                <body>
                    <h1>Transform.position</h1>
                    <div class='signature'>
                        <code>public Vector3 position { get; set; }</code>
                    </div>
                    <div class='description'>
                        <p>The position of the transform in world space.</p>
                    </div>
                </body>
                </html>";

            // Act
            var entry = parser.ParseHtml(html, "test-url");

            // Assert
            Assert.AreEqual("Transform", entry.ClassName);
            Assert.AreEqual("position", entry.MethodName);
            Assert.AreEqual("Vector3", entry.ReturnType);
            Assert.IsEmpty(entry.Parameters, "Properties should have no parameters");
        }

        [Test]
        public void ParseHtml_StaticMethod_ShouldDetectStatic()
        {
            // Arrange
            var html = @"
                <html>
                <body>
                    <div class='signature'>
                        <code>public static GameObject Find(string name);</code>
                    </div>
                </body>
                </html>";

            // Act
            var entry = parser.ParseHtml(html, "test-url");

            // Assert
            Assert.AreEqual("GameObject", entry.ReturnType);
            StringAssert.Contains("static", entry.Description ?? "", "Should note method is static");
        }

        [Test]
        public void ParseHtml_WithParameterDescriptions_ShouldExtractParameterDetails()
        {
            // Arrange
            var html = @"
                <html>
                <body>
                    <div class='parameters'>
                        <div class='parameter'>
                            <span class='name'>translation</span>
                            <span class='type'>Vector3</span>
                            <p>Movement vector in the specified coordinate system.</p>
                        </div>
                        <div class='parameter'>
                            <span class='name'>relativeTo</span>
                            <span class='type'>Space</span>
                            <p>Coordinate system to move the transform in.</p>
                        </div>
                    </div>
                </body>
                </html>";

            // Act
            var entry = parser.ParseHtml(html, "test-url");

            // Assert
            Assert.IsNotNull(entry.Parameters);
            Assert.GreaterOrEqual(entry.Parameters.Length, 2, "Should extract parameter information");
        }

        [Test]
        public void ParseHtml_DeprecatedMethod_ShouldMarkAsDeprecated()
        {
            // Arrange
            var html = @"
                <html>
                <body>
                    <div class='deprecated-message'>
                        <p>This method is deprecated. Use NewMethod instead.</p>
                    </div>
                    <div class='signature'>
                        <code>public void OldMethod();</code>
                    </div>
                </body>
                </html>";

            // Act
            var entry = parser.ParseHtml(html, "test-url");

            // Assert
            Assert.IsTrue(entry.IsDeprecated, "Should mark deprecated methods");
            StringAssert.Contains("NewMethod", entry.ReplacementApi ?? "",
                "Should extract replacement API suggestion");
        }

        [Test]
        public void ParseHtml_InvalidHtml_ShouldReturnNull()
        {
            // Arrange
            var invalidHtml = "This is not valid HTML";

            // Act
            var entry = parser.ParseHtml(invalidHtml, "test-url");

            // Assert
            Assert.IsNull(entry, "Should return null for invalid HTML");
        }

        [Test]
        public void ParseHtml_EmptyHtml_ShouldReturnNull()
        {
            // Arrange
            var emptyHtml = "";

            // Act
            var entry = parser.ParseHtml(emptyHtml, "test-url");

            // Assert
            Assert.IsNull(entry, "Should return null for empty HTML");
        }

        [Test]
        public void ParseHtml_ShouldSetDocumentationUrl()
        {
            // Arrange
            var html = "<html><body><h1>Test</h1></body></html>";
            var url = "https://docs.unity3d.com/ScriptReference/Transform.html";

            // Act
            var entry = parser.ParseHtml(html, url);

            // Assert
            Assert.AreEqual(url, entry.DocumentationUrl);
        }

        [Test]
        public void ParseHtml_ComplexSignature_ShouldParseAllComponents()
        {
            // Arrange - Complex method with multiple parameters and generics
            var html = @"
                <html>
                <body>
                    <h1>GameObject.GetComponent</h1>
                    <div class='signature'>
                        <code>public T GetComponent&lt;T&gt;() where T : Component;</code>
                    </div>
                    <div class='description'>
                        <p>Returns the component of Type T if the game object has one attached, null if it doesn't.</p>
                    </div>
                </body>
                </html>";

            // Act
            var entry = parser.ParseHtml(html, "test-url");

            // Assert
            Assert.IsNotNull(entry);
            Assert.AreEqual("GameObject", entry.ClassName);
            Assert.AreEqual("GetComponent", entry.MethodName);
            StringAssert.Contains("component", entry.Description.ToLower());
        }
    }
}
