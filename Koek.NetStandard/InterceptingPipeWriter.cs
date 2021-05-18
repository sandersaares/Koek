using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Koek
{
    /// <summary>
    /// Allows us to observe when Complete() is called.
    /// </summary>
    public sealed class InterceptingPipeWriter : PipeWriter
    {
        public Task WriterCompleted => _writerCompleted.Task;

        private readonly TaskCompletionSource<bool> _writerCompleted = new();

        public InterceptingPipeWriter(PipeWriter writer)
        {
            _writer = writer;
        }

        private readonly PipeWriter _writer;

        public override void Complete(Exception? exception = null)
        {
            _writer.Complete(exception);

            _writerCompleted.SetResult(true);
        }

        public override async ValueTask CompleteAsync(Exception? exception = null)
        {
            await _writer.CompleteAsync(exception);

            _writerCompleted.SetResult(true);
        }

        [DebuggerStepThrough]
        public override void Advance(int bytes) => _writer.Advance(bytes);
        [DebuggerStepThrough]
        public override void CancelPendingFlush() => _writer.CancelPendingFlush();
        [DebuggerStepThrough]
        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default) => _writer.FlushAsync(cancellationToken);
        [DebuggerStepThrough]
        public override Memory<byte> GetMemory(int sizeHint = 0) => _writer.GetMemory(sizeHint);
        [DebuggerStepThrough]
        public override Span<byte> GetSpan(int sizeHint = 0) => _writer.GetSpan(sizeHint);
        [DebuggerStepThrough]
        public override Stream AsStream(bool leaveOpen = false) => _writer.AsStream(leaveOpen);
        [DebuggerStepThrough]
        public override ValueTask<FlushResult> WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default) => _writer.WriteAsync(source, cancellationToken);
    }
}
