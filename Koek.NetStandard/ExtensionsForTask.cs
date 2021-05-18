using Microsoft.Extensions.Logging;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Koek
{
    public static class ExtensionsForTask
    {
        /// <summary>
        /// Just a hint to the compiler that I know I am not doing anything with the task
        /// and it should shut up about it and not spam me with a warning.
        /// </summary>
        public static void Forget(this Task task)
        {
        }

        /// <summary>
        /// Logs any exceptions from the task to the trace log of the provided type.
        /// After logging, the exception is wrapped in an AggregateException and re-thrown.
        /// </summary>
        public static Task LogExceptionsAndRethrow(this Task task, ILogger logger) => task.ContinueWith(t =>
        {
            if (t.Exception == null)
                return Task.CompletedTask;

            logger.LogError(t.Exception, "{ErrorMessage}", t.Exception.Message);
            throw new AggregateException(t.Exception);
        });

        /// <summary>
        /// Logs any exceptions from the task to the trace log of the provided type.
        /// After logging, the exception is wrapped in an AggregateException and re-thrown.
        /// </summary>
        public static Task<TResult> LogExceptionsAndRethrow<TResult>(this Task<TResult> task, ILogger logger) => task.ContinueWith(t =>
        {
            if (t.Exception == null)
                return t.Result;

            logger.LogError(t.Exception, "{ErrorMessage}", t.Exception.Message);
            throw new AggregateException(t.Exception);
        });

        /// <summary>
        /// Logs any exceptions from the task to the trace log of the provided type.
        /// Beyond logging, exceptions are ignored.
        /// </summary>
        public static Task LogExceptionsAndIgnore(this Task task, ILogger logger) => task.ContinueWith(t =>
        {
            if (t.Exception == null)
                return Task.CompletedTask;

            logger.LogError(t.Exception, "{ErrorMessage}", t.Exception.Message);
            return Task.CompletedTask;
        });

        /// <summary>
        /// Logs any exceptions from the task to the trace log of the provided type.
        /// If an exception occurs, returns the default value of the result type.
        /// </summary>
        public static Task<TResult?> LogExceptionsAndReturnDefault<TResult>(this Task<TResult> task, ILogger logger) => task.ContinueWith(t =>
        {
            if (t.Exception == null)
                return t.Result;

            logger.LogError(t.Exception, "{ErrorMessage}", t.Exception.Message);
            return default;
        });

        /// <summary>
        /// Signals that the continuation does not have to run using the current synchronization context.
        /// A more human-readable name for the commonly used ConfigureAwait(false) pattern.
        /// </summary>
        public static ConfiguredTaskAwaitable IgnoreContext(this Task task)
        {
            Helpers.Argument.ValidateIsNotNull(task, nameof(task));

            return task.ConfigureAwait(false);
        }

        /// <summary>
        /// Signals that the continuation does not have to run using the current synchronization context.
        /// A more human-readable name for the commonly used ConfigureAwait(false) pattern.
        /// </summary>
        public static ConfiguredTaskAwaitable<T> IgnoreContext<T>(this Task<T> task)
        {
            Helpers.Argument.ValidateIsNotNull(task, nameof(task));

            return task.ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronously waits for the task to complete and unwraps any exceptions.
        /// Without using this, you get annoying AggregateExceptions that mess up all your error messages and stack traces.
        /// </summary>
        public static void WaitAndUnwrapExceptions(this Task task)
        {
            Helpers.Argument.ValidateIsNotNull(task, nameof(task));

            task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Synchronously waits for the task to complete and unwraps any exceptions.
        /// Without using this, you get annoying AggregateExceptions that mess up all your error messages and stack traces.
        /// </summary>
        public static T WaitAndUnwrapExceptions<T>(this Task<T> task)
        {
            Helpers.Argument.ValidateIsNotNull(task, nameof(task));

            return task.GetAwaiter().GetResult();
        }

        public static async Task IgnoreExceptionsAsync(this Task t)
        {
            try
            {
                await t;
            }
            catch
            {
            }
        }

        public static async Task IgnoreExceptionsAsync<T>(this Task<T> t)
        {
            try
            {
                await t;
            }
            catch
            {
            }
        }
    }
}