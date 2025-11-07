using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityMcp.Core.Protocol;

namespace UnifyMcp.Core.Transport
{
    /// <summary>
    /// Stdio transport for MCP protocol communication.
    /// Reads JSON-RPC messages from stdin and writes responses to stdout.
    /// Uses Console.Error for logging to avoid polluting the communication channel.
    /// </summary>
    public class StdioTransport : IDisposable
    {
        private readonly TextReader input;
        private readonly TextWriter output;
        private readonly TextWriter errorLog;
        private readonly CancellationTokenSource cancellationTokenSource;
        private bool isRunning;
        private bool isDisposed;

        /// <summary>
        /// Event raised when a message is received.
        /// </summary>
        public event Action<string> OnMessageReceived;

        /// <summary>
        /// Event raised when an error occurs.
        /// </summary>
        public event Action<Exception> OnError;

        public StdioTransport(TextReader input = null, TextWriter output = null, TextWriter errorLog = null)
        {
            this.input = input ?? Console.In;
            this.output = output ?? Console.Out;
            this.errorLog = errorLog ?? Console.Error;
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Starts listening for incoming messages.
        /// </summary>
        public async Task StartAsync()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(StdioTransport));

            if (isRunning)
                return;

            isRunning = true;
            LogError("[StdioTransport] Started listening for messages...");

            try
            {
                await ListenAsync(cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                LogError("[StdioTransport] Listening canceled");
            }
            catch (Exception ex)
            {
                LogError($"[StdioTransport] Error in listen loop: {ex.Message}");
                OnError?.Invoke(ex);
            }
            finally
            {
                isRunning = false;
            }
        }

        /// <summary>
        /// Sends a message via stdout.
        /// </summary>
        public async Task SendAsync(string message)
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(StdioTransport));

            try
            {
                await output.WriteLineAsync(message);
                await output.FlushAsync();
                LogError($"[StdioTransport] Sent: {message.Substring(0, Math.Min(100, message.Length))}...");
            }
            catch (Exception ex)
            {
                LogError($"[StdioTransport] Error sending message: {ex.Message}");
                OnError?.Invoke(ex);
                throw;
            }
        }

        /// <summary>
        /// Stops listening for messages.
        /// </summary>
        public void Stop()
        {
            if (!isRunning)
                return;

            LogError("[StdioTransport] Stopping...");
            cancellationTokenSource.Cancel();
        }

        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var line = await input.ReadLineAsync();

                    if (line == null)
                    {
                        // EOF reached
                        LogError("[StdioTransport] EOF reached, stopping");
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    LogError($"[StdioTransport] Received: {line.Substring(0, Math.Min(100, line.Length))}...");
                    OnMessageReceived?.Invoke(line);
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    LogError($"[StdioTransport] Error reading line: {ex.Message}");
                    OnError?.Invoke(ex);
                }
            }
        }

        private void LogError(string message)
        {
            try
            {
                errorLog?.WriteLine(message);
                errorLog?.Flush();
            }
            catch
            {
                // Ignore logging errors
            }
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            Stop();
            cancellationTokenSource?.Dispose();
        }
    }
}
