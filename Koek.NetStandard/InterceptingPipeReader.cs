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
    public sealed class InterceptingPipeReader : PipeReader
    {
        public Task ReaderCompleted => _readerCompleted.Task;

        private readonly TaskCompletionSource<bool> _readerCompleted = new();

        public InterceptingPipeReader(PipeReader reader)
        {
            _reader = reader;
        }

        private readonly PipeReader _reader;

        public override void Complete(Exception? exception = null)
        {
            _reader.Complete(exception);

            _readerCompleted.SetResult(true);
        }

        public override async ValueTask CompleteAsync(Exception? exception = null)
        {
            await _reader.CompleteAsync(exception);

            _readerCompleted.SetResult(true);
        }

        [DebuggerStepThrough]
        public override void AdvanceTo(SequencePosition consumed) => _reader.AdvanceTo(consumed);
        [DebuggerStepThrough]
        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined) => _reader.AdvanceTo(consumed, examined);
        [DebuggerStepThrough]
        public override void CancelPendingRead() => _reader.CancelPendingRead();
        [DebuggerStepThrough]
        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default) => _reader.ReadAsync(cancellationToken);
        [DebuggerStepThrough]
        public override bool TryRead(out ReadResult result) => _reader.TryRead(out result);
        [DebuggerStepThrough]
        public override Stream AsStream(bool leaveOpen = false) => _reader.AsStream(leaveOpen);
        [DebuggerStepThrough]
        public override Task CopyToAsync(PipeWriter destination, CancellationToken cancellationToken = default) => _reader.CopyToAsync(destination, cancellationToken);
        [DebuggerStepThrough]
        public override Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default) => _reader.CopyToAsync(destination, cancellationToken);
    }
}
