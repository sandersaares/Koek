using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Koek
{
    public static class ExtensionsForPipe
    {
        public static async Task CopyToUntilCanceledOrCompletedAsync(this PipeReader reader, PipeWriter writer, CancellationToken cancel)
        {
            using var cancelRegistration = cancel.Register(delegate
            {
                // If we get canceled, indicate operation cancellation on both pipes to break out of the loop.
                // The purpose of this here is to avoid throwing exceptions for cancellation, instead using the graceful signal.
                // Just because exceptions cost extra CPU time that we want to avoid when handling load spikes (such as mass disconnects).
                reader.CancelPendingRead();
                writer.CancelPendingFlush();
            });

            // We copy until we encounter either a read cancellation (upstream reached end of stream) or a write
            // completion (downstream reached end of stream) or a write cancellation (downstream requested graceful stop).

            while (true)
            {
                var readResult = await reader.ReadAsync(CancellationToken.None);

                if (readResult.IsCanceled)
                    break;

                try
                {
                    if (!readResult.Buffer.IsEmpty)
                    {
                        foreach (var segment in readResult.Buffer)
                        {
                            var memory = writer.GetMemory(segment.Length);
                            segment.CopyTo(memory);
                            writer.Advance(segment.Length);
                        }

                        var flushResult = await writer.FlushAsync(CancellationToken.None);

                        if (flushResult.IsCanceled || flushResult.IsCompleted)
                            break;
                    }

                    if (readResult.IsCompleted)
                        break;
                }
                finally
                {
                    reader.AdvanceTo(readResult.Buffer.End);
                }
            }
        }

        public static void CancelPendingFlushEvenIfClosed(this PipeWriter writer)
        {
            // CancelPendingFlush() will throw if the pipe is already closed.
            // This behavior is not useful to us, so eat the exception.
            try
            {
                writer.CancelPendingFlush();
            }
            catch
            {
            }
        }

        public static void CancelPendingReadEvenIfClosed(this PipeReader reader)
        {
            // CancelPendingRead() will throw if the pipe is already closed.
            // This behavior is not useful to us, so eat the exception.
            try
            {
                reader.CancelPendingRead();
            }
            catch
            {
            }
        }
    }
}
