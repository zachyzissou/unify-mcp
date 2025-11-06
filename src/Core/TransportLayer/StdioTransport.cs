using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnifyMcp.Common.Threading;

namespace UnifyMcp.Core.TransportLayer
{
    /// <summary>
    /// Stdio transport layer for MCP communication.
    /// Reads from Console.In, writes to Console.Out, marshals Unity API calls to main thread.
    /// Handles JSON-RPC 2.0 messages.
    /// </summary>
    public class StdioTransport : IDisposable
    {
        private readonly MainThreadDispatcher dispatcher;
        private readonly TextReader input;
        private readonly TextWriter output;
        private readonly SemaphoreSlim writeLock;
        private CancellationTokenSource cancellationTokenSource;
        private Task readLoopTask;
        private bool isDisposed;

        /// <summary>
        /// Event raised when a message is received.
        /// </summary>
        public event Action<string> OnMessageReceived;

        /// <summary>
        /// Event raised when an error occurs.
        /// </summary>
        public event Action<Exception> OnError;

        /// <summary>
        /// Gets whether the transport is currently running.
        /// </summary>
        public bool IsRunning { get; private set; }

        public StdioTransport(MainThreadDispatcher dispatcher = null, TextReader input = null, TextWriter output = null)
        {
            this.dispatcher = dispatcher ?? MainThreadDispatcher.Instance ?? throw new InvalidOperationException("MainThreadDispatcher not initialized");
            this.input = input ?? Console.In;
            this.output = output ?? Console.Out;
            this.writeLock = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Starts the stdio transport and begins reading messages.
        /// </summary>
        public void Start()
        {
            if (IsRunning)
                return;

            if (isDisposed)
                throw new ObjectDisposedException(nameof(StdioTransport));

            cancellationTokenSource = new CancellationTokenSource();
            IsRunning = true;

            // Start read loop on background thread
            readLoopTask = Task.Run(() => ReadLoop(cancellationTokenSource.Token));
        }

        /// <summary>
        /// Stops the stdio transport.
        /// </summary>
        public async Task StopAsync()
        {
            if (!IsRunning)
                return;

            IsRunning = false;
            cancellationTokenSource?.Cancel();

            // Wait for read loop to finish
            if (readLoopTask != null)
            {
                try
                {
                    await Task.WhenAny(readLoopTask, Task.Delay(5000));
                }
                catch (Exception)
                {
                    // Ignore exceptions during shutdown
                }
            }
        }

        /// <summary>
        /// Sends a message via stdio.
        /// Thread-safe with write lock.
        /// </summary>
        /// <param name="message">JSON-RPC 2.0 message</param>
        public async Task SendMessageAsync(string message)
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(StdioTransport));

            await writeLock.WaitAsync();
            try
            {
                await output.WriteLineAsync(message);
                await output.FlushAsync();
            }
            finally
            {
                writeLock.Release();
            }
        }

        /// <summary>
        /// Read loop running on background thread.
        /// </summary>
        private async Task ReadLoop(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var line = await input.ReadLineAsync();

                    if (line == null)
                    {
                        // EOF reached
                        break;
                    }

                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        // Marshal message handling to main thread if needed
                        // For now, invoke on current thread (will be improved in Phase 4)
                        OnMessageReceived?.Invoke(line);
                    }
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                OnError?.Invoke(ex);
            }
            finally
            {
                IsRunning = false;
            }
        }

        /// <summary>
        /// Disposes the transport and stops reading.
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            StopAsync().Wait(TimeSpan.FromSeconds(5));

            cancellationTokenSource?.Dispose();
            writeLock?.Dispose();
        }
    }
}
