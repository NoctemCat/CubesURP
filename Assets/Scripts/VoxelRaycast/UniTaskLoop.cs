using System;
using System.Threading;
using Cysharp.Threading.Tasks;

public struct UniTaskLoop
{
    private readonly Action _action;
    public PlayerLoopTiming DelayTiming;
    public float LoopDelay;
    private bool _running;
    private CancellationTokenSource _cts;

    public UniTaskLoop(Action action, float loopDelay, PlayerLoopTiming delayTiming = PlayerLoopTiming.Update)
    {
        _action = action;
        LoopDelay = loopDelay;
        _running = false;
        DelayTiming = delayTiming;
        _cts = null;
    }

    public void Start()
    {
        if (!_running)
        {
            _running = true;
            _cts = new CancellationTokenSource();
            Loop(_cts.Token).Forget();
        }
    }

    public void Stop()
    {
        if (_running)
        {
            _running = false;
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }

    private async readonly UniTaskVoid Loop(CancellationToken token)
    {
        while (true)
        {
            _action.Invoke();
            bool isCanceled = await UniTask.WaitForSeconds(LoopDelay, false, DelayTiming, token).SuppressCancellationThrow();
            if (isCanceled) return;
        }
    }
}