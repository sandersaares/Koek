using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Koek
{
    /// <summary>
    /// An external tool (exe, bat or other) that can be executed by automation when needed.
    /// Provides standard functionality such as output capture and reacting to meaningful results (e.g. throw on error).
    /// </summary>
    /// <remarks>
    /// To use, create an instance, fill the properties and call Start/ExecuteAsync.
    /// </remarks>
    public sealed class ExternalTool
    {
        /// <summary>
        /// There are various operations that should complete near-instantly but for
        /// reasons of operating systems magic may hang. This timeout controls when we give up.
        /// </summary>
        private static readonly TimeSpan LastResortTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Absolute or relative path to the executable. Relative paths are resolved mostly
        /// according to OS principles (PATH environment variable and potentially some others).
        /// </summary>
        public string? ExecutablePath { get; set; }

        /// <summary>
        /// Arguments string to provide to the executable.
        /// </summary>
        public string? Arguments { get; set; }

        /// <summary>
        /// Any environment variables to add to the normal environment variable set.
        /// </summary>
        public Dictionary<string, string>? EnvironmentVariables { get; set; }

        /// <summary>
        /// Defaults to the working directory of the current process.
        /// </summary>
        public string? WorkingDirectory { get; set; }

        /// <summary>
        /// Copies the standard output and standard error streams to the specified file if set.
        /// </summary>
        public string? OutputFilePath { get; set; }

        /// <summary>
        /// Allows a custom action to consume data from the standard output stream.
        /// The action is executed on a dedicated thread.
        /// If set, standard output stream is not captured to string and not visible in the tool results.
        /// </summary>
        public Action<Stream>? StandardOutputConsumer { get; set; }

        /// <summary>
        /// Allows a custom action to consume data from the standard error stream.
        /// The action is executed on a dedicated thread.
        /// If set, standard error stream is not captured to string and not visible in the tool results.
        /// </summary>
        public Action<Stream>? StandardErrorConsumer { get; set; }

        /// <summary>
        /// Allows a custom action to provide data on the standard input stream.
        /// The action is executed on a dedicated thread.
        /// </summary>
        public Action<Stream>? StandardInputProvider { get; set; }

        /// <summary>
        /// If set, any strings in this collection are censored in log output (though not stdout/stderr).
        /// Useful if you pass credentials on the command line.
        /// </summary>
        public IReadOnlyCollection<string>? CensoredStrings { get; set; }

        /// <summary>
        /// Sets the process priority class to use for the started tool.
        /// </summary>
        public ProcessPriorityClass ProcessPriority { get; set; } = ProcessPriorityClass.BelowNormal;

        /// <summary>
        /// Whether to capture the stdout/stderr to strings. Set this to false if you expect the streams to grow excessively large of if the data is expected to be non-text.
        /// This only has any effect when you are not setting custom standard stream consumers - those override the string capture no matter what.
        /// </summary>
        public bool CaptureOutputStreamsToString { get; set; } = true;

        /// <summary>
        /// Starts a new instance of the external tool. Use this if you want more detailed control over the process
        /// e.g. the ability to terminate it or to inspect the running process. Otherwise, just use the synchronous Execute().
        /// </summary>
        public Instance Start()
        {
            return new Instance(this);
        }

        /// <summary>
        /// Synchronously executes an instance of the external tool and consumes the result.
        /// </summary>
        public ExternalToolResult Execute(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero)
                throw new TimeoutException("The external tool could not be executed because the operation had already timed out.");

            var instance = Start();
            var result = instance.GetResult(timeout);

            result.Consume();

            return result;
        }

        /// <summary>
        /// Asynchronously executes an instance of the external tool and consumes the result.
        /// </summary>
        public async Task<ExternalToolResult> ExecuteAsync(CancellationToken cancel = default)
        {
            cancel.ThrowIfCancellationRequested();

            var instance = Start();
            var result = await instance.GetResultAsync(cancel);

            result.Consume();

            return result;
        }

        /// <summary>
        /// Helper method to quickly execute a command with arguments and consume the result.
        /// </summary>
        public static ExternalToolResult Execute(string executablePath, string arguments, TimeSpan timeout)
        {
            Helpers.Argument.ValidateIsNotNullOrWhitespace(executablePath, "executablePath");

            return new ExternalTool
            {
                ExecutablePath = executablePath,
                Arguments = arguments
            }.Execute(timeout);
        }

        /// <summary>
        /// Helper method to quickly execute a command with arguments and consume the result.
        /// </summary>
        public static Task<ExternalToolResult> ExecuteAsync(string executablePath, string arguments, CancellationToken cancel = default)
        {
            Helpers.Argument.ValidateIsNotNullOrWhitespace(executablePath, "executablePath");

            return new ExternalTool
            {
                ExecutablePath = executablePath,
                Arguments = arguments
            }.ExecuteAsync(cancel);
        }

        public ExternalTool()
        {
            EnvironmentVariables = new Dictionary<string, string>();
        }

        /// <summary>
        /// A started instance of an external tool. May have finished running already.
        /// </summary>
        public sealed class Instance
        {
            public string ExecutablePath { get; }
            public string Arguments { get; }
            public string CensoredArguments { get; }
            public IReadOnlyDictionary<string, string> EnvironmentVariables { get; }
            public string WorkingDirectory { get; }
            public string? OutputFilePath { get; }
            public ProcessPriorityClass ProcessPriority { get; }
            public bool CaptureOutputStreamsToString { get; }

            /// <summary>
            /// Checks whether the process is still running.
            /// If false, the process has exited and the result is available.
            /// </summary>
            public bool IsRunning
            {
                get
                {
                    try
                    {
                        return _process.HasExited == false;
                    }
                    catch (Exception ex)
                    {
                        Trace.Error($"Unable to read process status: {ex}");
                        return false;
                    }
                }
            }

            internal TraceWriter Trace { get; }

            private readonly Process _process;
            private readonly string _shortName;

            /// <summary>
            /// Waits for the tool to exit and retrieves the result.
            /// If a timeout occurs, the running external tool process is killed.
            /// </summary>
            /// <exception cref="TimeoutException">Thrown if a timeout occurs.</exception>
            public ExternalToolResult GetResult(TimeSpan timeout)
            {
                if (!_result.Task.Wait(timeout))
                {
                    Trace.Verbose($"Terminating due to timeout.");

                    _process.Kill();

                    // Wait for result to be available so that all the output gets written to file.
                    // This may not work if something is very wrong, but we do what we can to help.
                    _result.Task.Wait(LastResortTimeout);

                    throw new TimeoutException(string.Format("Timeout waiting for external tool to finish: \"{0}\" {1}", ExecutablePath, Arguments));
                }

                return _result.Task.WaitAndUnwrapExceptions();
            }

            /// <summary>
			/// Waits for the tool to exit and retrieves the result.
            /// If the cancellation token is cancelled, the running external tool process is killed.
			/// </summary>
            public async Task<ExternalToolResult> GetResultAsync(CancellationToken cancel = default)
            {
                try
                {
                    return await _result.Task.WithAbandonment(cancel);
                }
                catch (TaskCanceledException)
                {
                    Trace.Verbose($"Terminating due to cancellation.");

                    // If a cancellation is signaled, we need to kill the process and set error to really time it out.
                    _process.Kill();

                    // Wait for result to be available so that all the output gets written to file.
                    // This may not work if something is very wrong, but we do what we can to help.
                    try
                    {
                        using (var lastResort = new CancellationTokenSource(LastResortTimeout))
                            await _result.Task.WithAbandonment(lastResort.Token);
                    }
                    catch
                    {
                        // We are not awaiting to get a result, just to actually wait for the process to finish.
                        // Accordingly, we do not care what the result is (whether error or success).
                    }

                    throw new TaskCanceledException(string.Format("External tool execution cancelled: \"{0}\" {1}", ExecutablePath, Arguments));
                }
            }

            private Action<Stream>? _standardOutputConsumer;
            private Action<Stream>? _standardInputProvider;
            private Action<Stream>? _standardErrorConsumer;

            private readonly TaskCompletionSource<ExternalToolResult> _result = new TaskCompletionSource<ExternalToolResult>();

            /// <summary>
            /// Creates a new instance of an external tool, using the specified template. Does not start it yet.
            /// </summary>
            internal Instance(ExternalTool template)
            {
                Helpers.Argument.ValidateIsNotNull(template, nameof(template));

                if (string.IsNullOrWhiteSpace(template.ExecutablePath))
                    throw new ArgumentException("Executable path must be specified.", nameof(template));

                if (template.WorkingDirectory != null && !Directory.Exists(template.WorkingDirectory))
                    throw new ArgumentException("The working directory does not exist.", nameof(template));

                _shortName = Path.GetFileName(template.ExecutablePath);
                Trace = TraceWriter.ForTypeAndSubcategory<ExternalTool>(_shortName);

                var executablePath = template.ExecutablePath;

                // First, resolve the path.
                if (!Path.IsPathRooted(executablePath))
                {
                    var resolvedPath = Helpers.Filesystem.ResolvePath(executablePath);
                    Trace.Verbose($"{executablePath} resolved to {resolvedPath}");

                    executablePath = resolvedPath;
                }

                // Then prepare the variables.
                ExecutablePath = executablePath;
                Arguments = template.Arguments ?? "";
                EnvironmentVariables = new Dictionary<string, string>(template.EnvironmentVariables ?? new Dictionary<string, string>());
                WorkingDirectory = template.WorkingDirectory ?? Environment.CurrentDirectory;
                OutputFilePath = template.OutputFilePath;
                ProcessPriority = template.ProcessPriority;
                CaptureOutputStreamsToString = template.CaptureOutputStreamsToString;

                _standardInputProvider = template.StandardInputProvider;
                _standardOutputConsumer = template.StandardOutputConsumer;
                _standardErrorConsumer = template.StandardErrorConsumer;

                // We may need to censor the log line!
                CensoredArguments = Arguments;

                if (template.CensoredStrings?.Count > 0)
                {
                    foreach (var censoredString in template.CensoredStrings)
                    {
                        if (string.IsNullOrWhiteSpace(censoredString))
                            continue;

                        CensoredArguments = CensoredArguments.Replace(censoredString, "*********");
                    }
                }

                _process = Start();
            }

            /// <summary>
            /// We want to suppress any Windows error reporting dialogs that occur due to the external tool crashing.
            /// During the lifetime of this object, this is done for the current process and any started process
            /// will inherit this configuration from the current process, so ensure this object is alive during child start.
            /// 
            /// Note that this class also acts as a mutex.
            /// </summary>
            /// <remarks>
            /// This class does nothing on non-Windows operating systems.
            /// </remarks>
            private sealed class CrashDialogSuppressionBlock : IDisposable
            {
                public CrashDialogSuppressionBlock()
                {
                    if (Helpers.Environment.IsNonMicrosoftOperatingSystem())
                        return;

                    Monitor.Enter(_errorModeLock);

                    // Keep any default flags the OS gives us and ensure that our own flags are added.
                    _previousErrorMode = GetErrorMode();
                    SetErrorMode(_previousErrorMode.Value | ErrorModes.SEM_FAILCRITICALERRORS | ErrorModes.SEM_NOGPFAULTERRORBOX);
                }

                private ErrorModes? _previousErrorMode;

                public void Dispose()
                {
                    if (Helpers.Environment.IsNonMicrosoftOperatingSystem())
                        return;

                    // Can only dispose once.
                    if (_previousErrorMode == null)
                        return;

                    SetErrorMode(_previousErrorMode.Value);
                    _previousErrorMode = null;

                    Monitor.Exit(_errorModeLock);
                }

                [DllImport("kernel32.dll")]
                private static extern ErrorModes SetErrorMode(ErrorModes uMode);

                [DllImport("kernel32.dll")]
                private static extern ErrorModes GetErrorMode();

                [Flags]
                private enum ErrorModes : uint
                {
                    SYSTEM_DEFAULT = 0x0,
                    SEM_FAILCRITICALERRORS = 0x0001,
                    SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,
                    SEM_NOGPFAULTERRORBOX = 0x0002,
                    SEM_NOOPENFILEERRORBOX = 0x8000
                }

                /// <summary>
                /// We only want one thread to be touching the error mode at the same time.
                /// </summary>
                private static readonly object _errorModeLock = new object();
            }

            internal Process Start()
            {
                Trace.Verbose($"Executing: {ExecutablePath} {CensoredArguments}");

                // We write both stderr and stdout to the output file, line by line.
                // If you need to make a distinction, capture the streams yourself.
                // We capture both into strings separately, though, if there is no file output.
                StreamWriter? outputFileWriter = TryCreateOutputFileWriter();
                var outputFileWriterLock = new object();

                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        Arguments = Arguments,
                        ErrorDialog = false,
                        FileName = ExecutablePath,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        WorkingDirectory = WorkingDirectory,
                        CreateNoWindow = true
                    };

                    if (EnvironmentVariables != null)
                    {
                        foreach (var pair in EnvironmentVariables)
                            startInfo.EnvironmentVariables[pair.Key] = pair.Value;
                    }

                    if (_standardInputProvider != null)
                        startInfo.RedirectStandardInput = true;

                    var runtime = Stopwatch.StartNew();

                    // We do not dispose of the Process for workflow simplicity. The GC will take care of handle cleanup.
                    // If you create 1000s of processes, maybe fix this. Otherwise, not worth the extra complexity.
                    Process process;

                    using (new CrashDialogSuppressionBlock())
                        process = Process.Start(startInfo);

                    Trace.Verbose("Process started.");

                    try
                    {
                        // This opens the handle to the process and holds it for the lifetime of the Process object.
                        process.PriorityClass = ProcessPriority;
                    }
                    catch (InvalidOperationException)
                    {
                        // If the process has already exited, we get this on Windows. This is fine.
                    }
                    catch (Win32Exception ex) when (ex.NativeErrorCode == 3) // "No such process"
                    {
                        // If the process has already exited, we get this on Linux. This is fine.
                    }

                    // These are only set if they are created by ExternalTool - we don't care about user threads.
                    Thread? standardErrorReader = null;
                    Thread? standardOutputReader = null;

                    var standardOutput = new StringBuilder();
                    var standardError = new StringBuilder();

                    if (_standardErrorConsumer != null)
                    {
                        // Caller wants to have it. Okay, fine. We do not need to track this thread, just create it.
                        new Thread((ThreadStart)delegate
                        {
                            try
                            {
                                _standardErrorConsumer(process.StandardError.BaseStream);
                            }
                            catch (Exception ex)
                            {
                                Trace.Error($"Caller-provided stderr consumer crashed! {ex}");
                                process.StandardError.Close();
                                throw;
                            }
                        })
                        {
                            Name = $"{_shortName} stderr reader (custom)",
                            IsBackground = true
                        }.Start();
                    }
                    else
                    {
                        // We'll store it ourselves.
                        standardErrorReader = new Thread((ThreadStart)delegate
                        {
                            while (true)
                            {
                                var line = process.StandardError.ReadLine();

                                if (line == null)
                                    return; // End of stream.

                                if (CaptureOutputStreamsToString)
                                    standardError.AppendLine(line);

                                lock (outputFileWriterLock)
                                    if (outputFileWriter != null)
                                        outputFileWriter.WriteLine(line);
                            }
                        })
                        {
                            Name = $"{_shortName} stderr reader",
                            IsBackground = true
                        };

                        standardErrorReader.Start();
                    }

                    if (_standardOutputConsumer != null)
                    {
                        // Caller wants to have it. Okay, fine. We do not need to track this thread, just create it.
                        new Thread((ThreadStart)delegate
                        {
                            try
                            {
                                _standardOutputConsumer(process.StandardOutput.BaseStream);
                            }
                            catch (Exception ex)
                            {
                                Trace.Error($"Caller-provided stdout consumer crashed! {ex}");
                                process.StandardOutput.Close();
                                throw;
                            }
                        })
                        {
                            Name = $"{_shortName} stdout reader (custom)",
                            IsBackground = true
                        }.Start();
                    }
                    else
                    {
                        // We'll store it ourselves.
                        standardOutputReader = new Thread((ThreadStart)delegate
                        {
                            while (true)
                            {
                                var line = process.StandardOutput.ReadLine();

                                if (line == null)
                                    return; // End of stream.

                                if (CaptureOutputStreamsToString)
                                    standardOutput.AppendLine(line);

                                lock (outputFileWriterLock)
                                    if (outputFileWriter != null)
                                        outputFileWriter.WriteLine(line);
                            }
                        })
                        {
                            Name = $"{_shortName} stdout reader",
                            IsBackground = true
                        };

                        standardOutputReader.Start();
                    }

                    if (_standardInputProvider != null)
                    {
                        // We don't care about monitoring this later, since ExternalTool does not need to touch stdin.
                        new Thread((ThreadStart)delegate
                        {
                            try
                            {
                                // Closing stdin after providing input is critical or the app may just hang forever.
                                using (var stdin = process.StandardInput.BaseStream)
                                    _standardInputProvider(stdin);
                            }
                            catch (Exception ex)
                            {
                                Trace.Error($"Caller-provided stdin provider crashed! {ex}");
                                throw;
                            }
                        })
                        {
                            Name = $"{_shortName} stdin provider",
                            IsBackground = true
                        }.Start();
                    }

                    var resultThread = new Thread((ThreadStart)delegate
                    {
                        try
                        {
                            process.WaitForExit();
                            Trace.Verbose("Process exited.");

                            var exitCode = process.ExitCode;
                            runtime.Stop();

                            // NB! Streams may stay open and blocked after process exits.
                            // This happens e.g. if you go cmd.exe -> start.exe.
                            // Even if you kill cmd.exe, start.exe remains and keeps the pipes open.
                            standardErrorReader?.Join();
                            standardOutputReader?.Join();

                            lock (outputFileWriterLock)
                                if (outputFileWriter != null)
                                    outputFileWriter.Dispose();

                            _result.TrySetResult(new ExternalToolResult(this, standardOutput.ToString(), standardError.ToString(), exitCode, runtime.Elapsed));
                        }
                        catch (Exception ex)
                        {
                            Trace.Error($"Failed to observe results. Process may remain running unobserved. {ex}");
                            _result.TrySetException(ex);
                        }
                    })
                    {
                        Name = $"{_shortName} result observer",
                        IsBackground = true
                    };

                    // All the rest happens in the result thread, which waits for the process to exit.
                    resultThread.Start();

                    return process;
                }
                catch (Exception)
                {
                    // Don't leave this lingering if starting the process fails.
                    outputFileWriter?.Dispose();

                    throw;
                }
            }

            private StreamWriter? TryCreateOutputFileWriter()
            {
                if (string.IsNullOrWhiteSpace(OutputFilePath))
                    return null;

                // Make sure the file can be created - parent directory exists.
                var parent = Path.GetDirectoryName(OutputFilePath);

                // No need to create it if it is a relative path with no parent.
                if (!string.IsNullOrWhiteSpace(parent))
                    Directory.CreateDirectory(parent);

                // Create the file.
                return File.CreateText(OutputFilePath);
            }
        }
    }
}