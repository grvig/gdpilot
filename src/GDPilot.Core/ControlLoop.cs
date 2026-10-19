namespace GDPilot.Core;

/// <summary>
/// One pass of the main loop, kept apart from the timing that drives it so a
/// test can run it tick by tick against a fake pad and a recording sink.
/// </summary>
public sealed class ControlLoop
{
    private readonly IGamepadSource source;
    private readonly Mapper mapper;
    private readonly IInputSink sink;
    private readonly StallDetector stall;
    private readonly FocusWatcher focus;
    private readonly Suspension suspension;

    public ControlLoop(IGamepadSource source, Mapper mapper, IInputSink sink, StallDetector stall, FocusWatcher focus, Suspension suspension)
    {
        this.source = source;
        this.mapper = mapper;
        this.sink = sink;
        this.stall = stall;
        this.focus = focus;
        this.suspension = suspension;
    }

    public void Tick(TimeSpan elapsed)
    {
        stall.Beat();
        focus.Poll();
        GamepadSnapshot snapshot = source.Read();

        if (suspension.Reasons != SuspendReason.None)
        {
            // The guarded sink released everything when the suspension began.
            // Forgetting it here means whatever is still held gets pressed
            // again on resume, instead of the mapper believing it never stopped.
            mapper.Reset();
            return;
        }

        foreach (InputIntent intent in mapper.Tick(snapshot, elapsed))
        {
            sink.Send(intent);
        }
    }
}
