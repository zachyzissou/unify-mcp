using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnifyMcp.Core.Context;
using UnifyMcp.Core.Context.Models;

namespace UnifyMcp.Tests.Integration
{
    /// <summary>
    /// Integration tests for coordinated multi-tool operations.
    /// Tests S064: Complex workflows involving multiple tools.
    /// </summary>
    [TestFixture]
    public class MultiToolOperationTests
    {
        private ContextWindowManager contextManager;

        [SetUp]
        public void SetUp()
        {
            contextManager = new ContextWindowManager();
        }

        [TearDown]
        public void TearDown()
        {
            contextManager?.Dispose();
        }

        [Test]
        public async Task MultiTool_ParallelExecution_AllSucceed()
        {
            // Arrange
            var tools = new[]
            {
                ("Tool1", new Dictionary<string, object> { { "param", "value1" } }),
                ("Tool2", new Dictionary<string, object> { { "param", "value2" } }),
                ("Tool3", new Dictionary<string, object> { { "param", "value3" } })
            };

            // Act - Execute all tools in parallel
            var tasks = tools.Select(t =>
                contextManager.ProcessToolRequestAsync(
                    t.Item1,
                    t.Item2,
                    async () =>
                    {
                        await Task.Delay(50); // Simulate work
                        return $"Result from {t.Item1}";
                    }
                )
            ).ToArray();

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.AreEqual(3, results.Length);
            Assert.IsTrue(results.All(r => r.Response.StartsWith("Result from")));
            Assert.IsTrue(results.All(r => r.Error == null));
        }

        [Test]
        public async Task MultiTool_Sequential_WithDataDependency()
        {
            // Arrange - Simulate workflow where Tool2 depends on Tool1's result
            string tool1Result = null;

            // Act - Step 1: Execute Tool1
            var result1 = await contextManager.ProcessToolRequestAsync(
                "Tool1",
                new Dictionary<string, object> { { "query", "initial" } },
                async () =>
                {
                    await Task.Delay(50);
                    return "{\"id\": \"12345\", \"data\": \"test data\"}";
                }
            );

            tool1Result = result1.Response;

            // Act - Step 2: Extract ID from Tool1 result and use in Tool2
            var extractedId = "12345"; // In real scenario, parse from tool1Result

            var result2 = await contextManager.ProcessToolRequestAsync(
                "Tool2",
                new Dictionary<string, object> { { "id", extractedId } },
                async () =>
                {
                    await Task.Delay(50);
                    return $"{{\"id\": \"{extractedId}\", \"details\": \"additional details\"}}";
                }
            );

            // Assert
            Assert.IsNotNull(result1.Response);
            Assert.IsNotNull(result2.Response);
            Assert.IsTrue(result2.Response.Contains(extractedId));
        }

        [Test]
        public async Task MultiTool_BatchProcessing_ManyItems()
        {
            // Arrange - Simulate processing 20 items
            var itemCount = 20;
            var items = Enumerable.Range(1, itemCount).Select(i => $"item{i}").ToArray();

            // Act - Process all items
            var tasks = items.Select(item =>
                contextManager.ProcessToolRequestAsync(
                    "ProcessItem",
                    new Dictionary<string, object> { { "item", item } },
                    async () =>
                    {
                        await Task.Delay(10);
                        return $"{{\"item\": \"{item}\", \"processed\": true}}";
                    }
                )
            ).ToArray();

            var stopwatch = Stopwatch.StartNew();
            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            Assert.AreEqual(itemCount, results.Length);
            Assert.IsTrue(results.All(r => r.Response.Contains("\"processed\": true")));

            // Parallel execution should be faster than sequential
            // 20 items * 10ms = 200ms sequential, but parallel should be much faster
            Assert.Less(stopwatch.ElapsedMilliseconds, 150);
        }

        [Test]
        public async Task MultiTool_CascadingOptimizations()
        {
            // Arrange - Execute same tools multiple times to test cascading optimizations
            var executionCount = new Dictionary<string, int>
            {
                { "Tool1", 0 },
                { "Tool2", 0 },
                { "Tool3", 0 }
            };

            Func<string, Task<string>> CreateExecutor(string toolName) => async () =>
            {
                executionCount[toolName]++;
                await Task.Delay(20);
                return $"Result from {toolName}";
            };

            // Act - Execute each tool 3 times
            for (int i = 0; i < 3; i++)
            {
                await contextManager.ProcessToolRequestAsync(
                    "Tool1",
                    new Dictionary<string, object> { { "param", "value" } },
                    CreateExecutor("Tool1")
                );

                await contextManager.ProcessToolRequestAsync(
                    "Tool2",
                    new Dictionary<string, object> { { "param", "value" } },
                    CreateExecutor("Tool2")
                );

                await contextManager.ProcessToolRequestAsync(
                    "Tool3",
                    new Dictionary<string, object> { { "param", "value" } },
                    CreateExecutor("Tool3")
                );
            }

            // Assert - Each tool should execute only once due to caching/deduplication
            Assert.AreEqual(1, executionCount["Tool1"]);
            Assert.AreEqual(1, executionCount["Tool2"]);
            Assert.AreEqual(1, executionCount["Tool3"]);
        }

        [Test]
        public async Task MultiTool_MixedSuccess_AndFailure()
        {
            // Arrange
            var tools = new[]
            {
                ("SuccessTool1", true),
                ("FailureTool", false),
                ("SuccessTool2", true)
            };

            var results = new List<OptimizedToolResult>();
            var errors = new List<Exception>();

            // Act
            foreach (var (toolName, shouldSucceed) in tools)
            {
                try
                {
                    var result = await contextManager.ProcessToolRequestAsync(
                        toolName,
                        new Dictionary<string, object>(),
                        async () =>
                        {
                            await Task.Delay(10);
                            if (!shouldSucceed)
                                throw new InvalidOperationException($"{toolName} failed");
                            return $"Success from {toolName}";
                        }
                    );
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            }

            // Assert
            Assert.AreEqual(2, results.Count); // 2 successful
            Assert.AreEqual(1, errors.Count); // 1 failed
            Assert.IsTrue(errors[0].Message.Contains("FailureTool failed"));
        }

        [Test]
        public async Task MultiTool_DifferentOptimizationStrategies()
        {
            // Arrange - Different tools with different optimization needs
            var aggressiveOptions = new ContextOptimizationOptions
            {
                EnableSummarization = true,
                EnforceTokenBudget = true,
                SummarizationOptions = new SummarizationOptions { Mode = SummarizationMode.Aggressive }
            };

            var minimalOptions = new ContextOptimizationOptions
            {
                EnableSummarization = true,
                EnforceTokenBudget = false,
                SummarizationOptions = new SummarizationOptions { Mode = SummarizationMode.Minimal }
            };

            // Act
            var result1 = await contextManager.ProcessToolRequestAsync(
                "VerboseTool",
                new Dictionary<string, object>(),
                async () => await Task.FromResult(new string('x', 10000)),
                aggressiveOptions
            );

            var result2 = await contextManager.ProcessToolRequestAsync(
                "ConciseTool",
                new Dictionary<string, object>(),
                async () => await Task.FromResult("Short response"),
                minimalOptions
            );

            // Assert
            Assert.Less(result1.Response.Length, 10000); // Should be summarized
            Assert.AreEqual("Short response", result2.Response); // Should be unchanged
            Assert.Greater(result1.OptimizationsApplied.Count, result2.OptimizationsApplied.Count);
        }

        [Test]
        public async Task MultiTool_CrossToolStatistics()
        {
            // Arrange & Act - Execute multiple different tools
            await contextManager.ProcessToolRequestAsync(
                "Tool1",
                new Dictionary<string, object> { { "param", "a" } },
                async () => await Task.FromResult(new string('x', 1000))
            );

            await contextManager.ProcessToolRequestAsync(
                "Tool2",
                new Dictionary<string, object> { { "param", "b" } },
                async () => await Task.FromResult(new string('y', 2000))
            );

            await contextManager.ProcessToolRequestAsync(
                "Tool1",
                new Dictionary<string, object> { { "param", "c" } },
                async () => await Task.FromResult(new string('z', 1500))
            );

            var stats = await contextManager.GetStatisticsAsync();

            // Assert
            Assert.AreEqual(3, stats.TokenMetrics.RequestCount);
            Assert.IsTrue(stats.TokenMetrics.ToolUsage.ContainsKey("Tool1"));
            Assert.IsTrue(stats.TokenMetrics.ToolUsage.ContainsKey("Tool2"));
            Assert.AreEqual(2, stats.TokenMetrics.ToolUsage["Tool1"].InvocationCount);
            Assert.AreEqual(1, stats.TokenMetrics.ToolUsage["Tool2"].InvocationCount);
        }

        [Test]
        public async Task MultiTool_ToolChaining_WithTransformation()
        {
            // Arrange - Chain 3 tools where each transforms the previous output
            var initialData = "start";

            // Act - Tool chain: Tool1 -> Tool2 -> Tool3
            var result1 = await contextManager.ProcessToolRequestAsync(
                "TransformTool1",
                new Dictionary<string, object> { { "input", initialData } },
                async () =>
                {
                    await Task.Delay(10);
                    return $"[Step1: {initialData}]";
                }
            );

            var result2 = await contextManager.ProcessToolRequestAsync(
                "TransformTool2",
                new Dictionary<string, object> { { "input", result1.Response } },
                async () =>
                {
                    await Task.Delay(10);
                    return $"[Step2: {result1.Response}]";
                }
            );

            var result3 = await contextManager.ProcessToolRequestAsync(
                "TransformTool3",
                new Dictionary<string, object> { { "input", result2.Response } },
                async () =>
                {
                    await Task.Delay(10);
                    return $"[Step3: {result2.Response}]";
                }
            );

            // Assert
            Assert.IsTrue(result1.Response.Contains("Step1"));
            Assert.IsTrue(result2.Response.Contains("Step2"));
            Assert.IsTrue(result3.Response.Contains("Step3"));
            Assert.IsTrue(result3.Response.Contains("start")); // Original data preserved through chain
        }

        [Test]
        public async Task MultiTool_ConditionalExecution()
        {
            // Arrange - Execute Tool2 only if Tool1 succeeds with specific condition
            bool executeTool2 = false;

            // Act - Tool1 execution
            var result1 = await contextManager.ProcessToolRequestAsync(
                "ConditionalTool1",
                new Dictionary<string, object>(),
                async () =>
                {
                    await Task.Delay(10);
                    return "{\"status\": \"success\", \"shouldContinue\": true}";
                }
            );

            // Check condition
            executeTool2 = result1.Response.Contains("\"shouldContinue\": true");

            OptimizedToolResult result2 = null;
            if (executeTool2)
            {
                result2 = await contextManager.ProcessToolRequestAsync(
                    "ConditionalTool2",
                    new Dictionary<string, object>(),
                    async () =>
                    {
                        await Task.Delay(10);
                        return "{\"status\": \"completed\"}";
                    }
                );
            }

            // Assert
            Assert.IsNotNull(result1);
            Assert.IsNotNull(result2);
            Assert.IsTrue(result2.Response.Contains("completed"));
        }

        [Test]
        public async Task MultiTool_AggregateResults()
        {
            // Arrange - Execute multiple tools and aggregate their results
            var tools = new[] { "DataTool1", "DataTool2", "DataTool3" };
            var allResults = new List<string>();

            // Act
            foreach (var toolName in tools)
            {
                var result = await contextManager.ProcessToolRequestAsync(
                    toolName,
                    new Dictionary<string, object>(),
                    async () =>
                    {
                        await Task.Delay(10);
                        return $"{{\"tool\": \"{toolName}\", \"data\": \"sample_{toolName}\"}}";
                    }
                );
                allResults.Add(result.Response);
            }

            // Aggregate
            var aggregatedData = string.Join(",\n", allResults);
            var finalResult = $"[{aggregatedData}]";

            // Assert
            Assert.AreEqual(3, allResults.Count);
            Assert.IsTrue(finalResult.Contains("DataTool1"));
            Assert.IsTrue(finalResult.Contains("DataTool2"));
            Assert.IsTrue(finalResult.Contains("DataTool3"));
        }

        [Test]
        public async Task MultiTool_ProgressiveOptimization()
        {
            // Arrange - Repeated execution should show improving optimization
            var parameters = new Dictionary<string, object> { { "query", "test" } };
            var durations = new List<long>();

            // Act - Execute same tool 5 times
            for (int i = 0; i < 5; i++)
            {
                var stopwatch = Stopwatch.StartNew();

                await contextManager.ProcessToolRequestAsync(
                    "ProgressiveTool",
                    parameters,
                    async () =>
                    {
                        await Task.Delay(100); // Simulate slow operation
                        return new string('x', 5000);
                    }
                );

                stopwatch.Stop();
                durations.Add(stopwatch.ElapsedMilliseconds);
            }

            // Assert - Later executions should be faster due to caching
            var firstExecution = durations[0];
            var lastExecution = durations[4];

            Assert.Less(lastExecution, firstExecution * 0.5); // At least 50% faster
        }

        [Test]
        public async Task MultiTool_ErrorPropagation()
        {
            // Arrange - Tool chain where error in Tool2 should be handled properly
            string errorMessage = null;

            try
            {
                // Tool1 succeeds
                var result1 = await contextManager.ProcessToolRequestAsync(
                    "ChainTool1",
                    new Dictionary<string, object>(),
                    async () => await Task.FromResult("{\"status\": \"ok\"}")
                );

                // Tool2 fails
                await contextManager.ProcessToolRequestAsync(
                    "ChainTool2",
                    new Dictionary<string, object> { { "input", result1.Response } },
                    async () =>
                    {
                        await Task.Delay(10);
                        throw new ArgumentException("Invalid input for Tool2");
                    }
                );
            }
            catch (ArgumentException ex)
            {
                errorMessage = ex.Message;
            }

            // Assert
            Assert.IsNotNull(errorMessage);
            Assert.IsTrue(errorMessage.Contains("Invalid input for Tool2"));
        }
    }
}
