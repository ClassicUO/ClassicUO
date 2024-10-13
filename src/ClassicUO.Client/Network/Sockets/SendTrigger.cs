using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace ClassicUO.Network.Sockets;

#nullable enable

internal sealed class SendTrigger : IValueTaskSource<int>
{
    private readonly Pipe _sendPipe;
    private ManualResetValueTaskSourceCore<int> _source;
    private bool _awaiting;

    public SendTrigger(Pipe sendPipe)
    {
        _source.RunContinuationsAsynchronously = true;
        _sendPipe = sendPipe;
    }

    public void Trigger(int value)
    {
        if (!_awaiting)
            return;

        ValueTaskSourceStatus status = _source.GetStatus(_source.Version);
        if (status == ValueTaskSourceStatus.Pending)
            _source.SetResult(value);
    }

    public void Reset()
    {
        _awaiting = false;
        _source.Reset();
    }

    public void Cancel()
    {
        if (_source.GetStatus(_source.Version) != ValueTaskSourceStatus.Pending)
            _source.Reset();

        _source.SetException(new OperationCanceledException());
    }

    public ValueTask<int> Wait()
    {
        int length = _sendPipe.Length;
        if (length > 0)
            return new(length);

        if (_source.GetStatus(_source.Version) == ValueTaskSourceStatus.Succeeded)
            _source.Reset();

        _awaiting = true;

        return new(this, _source.Version);
    }

    public int GetResult(short token)
    {
        return _source.GetResult(token);
    }

    public ValueTaskSourceStatus GetStatus(short token)
    {
        return _source.GetStatus(token);
    }

    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        _source.OnCompleted(continuation, state, token, flags);
    }
}
