using Microsoft.Extensions.Logging;
using System;
using System.Globalization;

namespace Koek
{
    /// <summary>
    /// The result of executing an instance of an external tool.
    /// Available once the external tool has finished its work.
    /// </summary>
    public sealed class ExternalToolResult
    {
        public bool Succeeded { get; }

        public string StandardOutput { get; }
        public string StandardError { get; }

        public int ExitCode { get; }

        public TimeSpan Duration { get; }

        // We use this to report some configuration but be careful - the instance itself might already be disposed!
        private readonly ExternalTool.Instance _instance;

        /// <summary>
        /// Forwards the external tool's standard output to the current app's standard output.
        /// </summary>
        public void ForwardOutputs()
        {
            if (!string.IsNullOrWhiteSpace(StandardOutput))
                _instance.Logger.LogDebug("Captured standard output stream: {StandardOutput}", StandardOutput);

            if (!string.IsNullOrWhiteSpace(StandardError))
                _instance.Logger.LogDebug("Captured standard error stream: {StandardError}", StandardError);
        }

        /// <summary>
        /// Consumes the result. This forwards the output and throws an exception if the tool execution failed.
        /// </summary>
        public void Consume()
        {
            ForwardOutputs();
            VerifySuccess();

            _instance.Logger.LogDebug("Finished in {ExternalToolDuration}s.", Duration.TotalSeconds.ToString("F2", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Verifies that the tool execution was successful. Throws an exception if any failure occurred.
        /// </summary>
        public void VerifySuccess()
        {
            if (!Succeeded)
            {
                // We report first 1 KB of stderr or stdout, to provide extra information if available.
                var detailsSource = (!string.IsNullOrWhiteSpace(StandardError) ? StandardError : StandardOutput) ?? "";
                var details = detailsSource.Substring(0, Math.Min(detailsSource.Length, 1024));

                throw new EnvironmentException($"External tool failure detected! Command: \"{_instance.ExecutablePath}\" {_instance.CensoredArguments}; Exit code: {ExitCode}; Runtime: {Duration.TotalSeconds:F2}s. Head of output: {details}");
            }
        }

        internal ExternalToolResult(ExternalTool.Instance externalToolInstance, string standardOutput, string standardError, int exitCode, TimeSpan duration)
        {
            _instance = externalToolInstance;
            StandardOutput = standardOutput;
            StandardError = standardError;
            ExitCode = exitCode;
            Duration = duration;

            Succeeded = DetermineSuccess();
        }

        /// <summary>
        /// Detects whether any failures occurred during external tool usage.
        /// </summary>
        private bool DetermineSuccess() => ExitCode == 0;
    }
}