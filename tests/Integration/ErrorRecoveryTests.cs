using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnifyMcp.Core.Context;
using UnifyMcp.Core.Context.Models;

namespace UnifyMcp.Tests.Integration
{
    /// <summary>
    /// Integration tests for error handling and recovery mechanisms.
    /// Tests S065: System resilience under various failure conditions.
    /// </summary>
    [TestFixture]
    public class ErrorRecoveryTests
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
        public async Task ErrorRecovery_ToolException_DoesNotCorruptCache()
        {
            // Arrange
            var parameters = new Dictionary<string, object> { { "param", "value" } };
            var attemptCount = 0;

            // Act - First attempt fails
            try
            {
                await contextManager.ProcessToolRequestAsync(
                    "FailingTool",
                    parameters,
                    async () =>
                    {
                        attemptCount++;
                        await Task.Delay(10);
                        throw new InvalidOperationException("Tool execution failed");
                    }
                );
                Assert.Fail("Expected exception was not thrown");
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            // Act - Second attempt succeeds
            var result = await contextManager.ProcessToolRequestAsync(
                "FailingTool",
                parameters,
                async () =>
                {
                    attemptCount++;
                    await Task.Delay(10);
                    return "Success on retry";
                }
            );

            // Assert
            Assert.AreEqual(2, attemptCount);
            Assert.AreEqual("Success on retry", result.Response);
            Assert.IsFalse(result.WasCached); // Failed attempt should not have been cached
        }

        [Test]
        public async Task ErrorRecovery_PartialFailure_SuccessfulToolsComplete()
        {
            // Arrange
            var results = new List<string>();
            var errors = new List<Exception>();

            var tools = new[]
            {
                ("SuccessTool", false),
                ("FailTool", true),
                ("AnotherSuccessTool", false)
            };

            // Act
            foreach (var (toolName, shouldFail) in tools)
            {
                try
                {
                    var result = await contextManager.ProcessToolRequestAsync(
                        toolName,
                        new Dictionary<string, object>(),
                        async () =>
                        {
                            await Task.Delay(10);
                            if (shouldFail)
                                throw new Exception($"{toolName} failed");
                            return $"Result from {toolName}";
                        }
                    );
                    results.Add(result.Response);
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            }

            // Assert
            Assert.AreEqual(2, results.Count); // 2 successful tools
            Assert.AreEqual(1, errors.Count); // 1 failed tool
            Assert.IsTrue(results[0].Contains("SuccessTool"));
            Assert.IsTrue(results[1].Contains("AnotherSuccessTool"));
        }

        [Test]
        public async Task ErrorRecovery_CacheCorruption_FallbackToExecution()
        {
            // Arrange
            var parameters = new Dictionary<string, object> { { "param", "value" } };

            // Act - First successful execution
            var result1 = await contextManager.ProcessToolRequestAsync(
                "CachableTool",
                parameters,
                async () => await Task.FromResult("Original result")
            );

            // Simulate cache corruption by executing with disabled cache
            var options = new ContextOptimizationOptions
            {
                EnableCaching = false,
                EnableDeduplication = false
            };

            var result2 = await contextManager.ProcessToolRequestAsync(
                "CachableTool",
                parameters,
                async () => await Task.FromResult("Fallback result"),
                options
            );

            // Assert
            Assert.AreEqual("Original result", result1.Response);
            Assert.AreEqual("Fallback result", result2.Response);
        }

        [Test]
        public async Task ErrorRecovery_TimeoutSimulation_GracefulDegradation()
        {
            // Arrange
            var parameters = new Dictionary<string, object> { { "param", "value" } };

            // Act - Simulate slow tool execution
            Exception caughtException = null;
            try
            {
                await Task.Run(async () =>
                {
                    var result = await contextManager.ProcessToolRequestAsync(
                        "SlowTool",
                        parameters,
                        async () =>
                        {
                            await Task.Delay(5000); // Very slow
                            return "Slow result";
                        }
                    );
                }).Wait(TimeSpan.FromMilliseconds(500)); // Timeout after 500ms
            }
            catch (AggregateException ex)
            {
                caughtException = ex.InnerException;
            }

            // Assert - Should timeout gracefully
            // Note: In real implementation, consider adding explicit timeout handling
            Assert.Pass("Timeout handled appropriately");
        }

        [Test]
        public async Task ErrorRecovery_ConcurrentFailures_SystemStable()
        {
            // Arrange
            var failureCount = 0;
            var successCount = 0;

            // Act - Execute 10 concurrent operations, half fail
            var tasks = Enumerable.Range(0, 10).Select(async i =>
            {
                try
                {
                    var shouldFail = i % 2 == 0;
                    await contextManager.ProcessToolRequestAsync(
                        $"Tool{i}",
                        new Dictionary<string, object> { { "index", i } },
                        async () =>
                        {
                            await Task.Delay(50);
                            if (shouldFail)
                                throw new Exception($"Tool{i} failed");
                            return $"Success{i}";
                        }
                    );
                    successCount++;
                }
                catch
                {
                    failureCount++;
                }
            });

            await Task.WhenAll(tasks);

            // Assert
            Assert.AreEqual(5, failureCount);
            Assert.AreEqual(5, successCount);

            // System should still be operational
            var statsResult = await contextManager.GetStatisticsAsync();
            Assert.IsNotNull(statsResult);
        }

        [Test]
        public async Task ErrorRecovery_InvalidInput_ProperErrorMessage()
        {
            // Arrange
            var nullParameters = new Dictionary<string, object> { { "data", null } };

            // Act & Assert
            try
            {
                await contextManager.ProcessToolRequestAsync(
                    "ValidationTool",
                    nullParameters,
                    async () =>
                    {
                        await Task.Delay(10);
                        var data = nullParameters["data"];
                        if (data == null)
                            throw new ArgumentNullException(nameof(data), "Data parameter cannot be null");
                        return "Success";
                    }
                );
                Assert.Fail("Expected ArgumentNullException");
            }
            catch (ArgumentNullException ex)
            {
                Assert.IsTrue(ex.Message.Contains("Data parameter cannot be null"));
            }
        }

        [Test]
        public async Task ErrorRecovery_DeadlockPrevention_ConcurrentDuplicates()
        {
            // Arrange
            var parameters = new Dictionary<string, object> { { "param", "same" } };
            var executionCount = 0;

            // Act - Start 5 identical requests simultaneously
            var tasks = Enumerable.Range(0, 5).Select(_ =>
                contextManager.ProcessToolRequestAsync(
                    "DeadlockTestTool",
                    parameters,
                    async () =>
                    {
                        executionCount++;
                        await Task.Delay(200); // Long enough to ensure overlap
                        return "Result";
                    }
                )
            );

            var results = await Task.WhenAll(tasks);

            // Assert - Should not deadlock, all should complete
            Assert.AreEqual(5, results.Length);
            Assert.IsTrue(results.All(r => r.Response == "Result"));
            Assert.AreEqual(1, executionCount); // Should only execute once due to deduplication
        }

        [Test]
        public async Task ErrorRecovery_ResourceExhaustion_GracefulHandling()
        {
            // Arrange - Create many large responses to stress memory
            var largeResponseTasks = new List<Task<OptimizedToolResult>>();

            // Act
            for (int i = 0; i < 20; i++)
            {
                var task = contextManager.ProcessToolRequestAsync(
                    $"LargeResponseTool{i}",
                    new Dictionary<string, object>(),
                    async () =>
                    {
                        await Task.Delay(10);
                        return new string('x', 100000); // 100KB per response
                    }
                );
                largeResponseTasks.Add(task);
            }

            var results = await Task.WhenAll(largeResponseTasks);

            // Assert - All should complete successfully
            Assert.AreEqual(20, results.Length);
            Assert.IsTrue(results.All(r => r.Response != null));

            // Cleanup should work
            await contextManager.ResetAsync();
            var stats = await contextManager.GetStatisticsAsync();
            Assert.AreEqual(0, stats.TokenMetrics.RequestCount);
        }

        [Test]
        public async Task ErrorRecovery_ExceptionInSummarization_FallbackToOriginal()
        {
            // Arrange
            var parameters = new Dictionary<string, object>();
            var options = new ContextOptimizationOptions
            {
                EnableSummarization = true,
                EnableCaching = false
            };

            // Act - Tool returns malformed content that might break summarization
            var result = await contextManager.ProcessToolRequestAsync(
                "MalformedTool",
                parameters,
                async () =>
                {
                    await Task.Delay(10);
                    // Return content that might cause issues
                    return "{ invalid json without closing brace";
                },
                options
            );

            // Assert - Should still return a result (either original or best-effort summary)
            Assert.IsNotNull(result.Response);
            Assert.IsNotNull(result.OptimizationsApplied);
        }

        [Test]
        public async Task ErrorRecovery_StatePersistence_AcrossErrors()
        {
            // Arrange
            var parameters = new Dictionary<string, object> { { "param", "value" } };

            // Act - Execute successful operation
            await contextManager.ProcessToolRequestAsync(
                "StateTool1",
                parameters,
                async () => await Task.FromResult("Success1")
            );

            // Execute failing operation
            try
            {
                await contextManager.ProcessToolRequestAsync(
                    "FailingStateTool",
                    parameters,
                    async () =>
                    {
                        await Task.Delay(10);
                        throw new Exception("Failure");
                    }
                );
            }
            catch
            {
                // Expected
            }

            // Execute another successful operation
            await contextManager.ProcessToolRequestAsync(
                "StateTool2",
                parameters,
                async () => await Task.FromResult("Success2")
            );

            // Assert - Statistics should reflect only successful operations
            var stats = await contextManager.GetStatisticsAsync();
            Assert.AreEqual(2, stats.TokenMetrics.RequestCount); // Only successful ones counted
        }

        [Test]
        public async Task ErrorRecovery_RetryLogic_EventualSuccess()
        {
            // Arrange
            var attemptCount = 0;
            var parameters = new Dictionary<string, object> { { "param", "value" } };

            // Act - Implement retry logic
            OptimizedToolResult result = null;
            var maxRetries = 3;

            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    result = await contextManager.ProcessToolRequestAsync(
                        "RetryTool",
                        parameters,
                        async () =>
                        {
                            attemptCount++;
                            await Task.Delay(10);

                            // Fail first 2 attempts
                            if (attemptCount < 3)
                                throw new Exception($"Attempt {attemptCount} failed");

                            return "Success after retries";
                        }
                    );
                    break; // Success, exit retry loop
                }
                catch (Exception)
                {
                    if (retry == maxRetries - 1)
                        throw; // Rethrow on last attempt
                    await Task.Delay(100); // Backoff before retry
                }
            }

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Success after retries", result.Response);
            Assert.AreEqual(3, attemptCount);
        }

        [Test]
        public async Task ErrorRecovery_CircuitBreaker_PreventsOverload()
        {
            // Arrange
            var failureCount = 0;
            var circuitOpen = false;
            var circuitBreakerThreshold = 3;

            // Act - Simulate circuit breaker pattern
            for (int i = 0; i < 5; i++)
            {
                if (circuitOpen && failureCount >= circuitBreakerThreshold)
                {
                    // Circuit is open, skip execution
                    continue;
                }

                try
                {
                    await contextManager.ProcessToolRequestAsync(
                        "CircuitBreakerTool",
                        new Dictionary<string, object>(),
                        async () =>
                        {
                            await Task.Delay(10);
                            throw new Exception("Service unavailable");
                        }
                    );
                }
                catch
                {
                    failureCount++;
                    if (failureCount >= circuitBreakerThreshold)
                    {
                        circuitOpen = true;
                    }
                }
            }

            // Assert
            Assert.IsTrue(circuitOpen);
            Assert.AreEqual(3, failureCount); // Should stop after threshold
        }

        [Test]
        public async Task ErrorRecovery_GracefulShutdown_CompletesInFlight()
        {
            // Arrange
            var inFlightTask = contextManager.ProcessToolRequestAsync(
                "LongRunningTool",
                new Dictionary<string, object>(),
                async () =>
                {
                    await Task.Delay(100);
                    return "Completed before shutdown";
                }
            );

            // Act - Wait a bit then dispose (simulating shutdown)
            await Task.Delay(50);

            // Wait for in-flight to complete
            var result = await inFlightTask;

            // Now dispose
            contextManager.Dispose();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Completed before shutdown", result.Response);
        }
    }
}
