namespace GDPilot.Core;

/// <summary>
/// Acts on the presses that change mode: panel bindings, back, and the panic
/// resync. Unlike held bindings these act once per press, and what they send
/// is a tap of a key rather than a key held for as long as the button is.
/// </summary>
public sealed class ModeController
{
    /// <summary>
    /// A tap's key is held this long. Pressed and released in the same instant,
    /// it could fall between two game frames and never be seen.
    /// </summary>
    public static readonly TimeSpan TapLength = TimeSpan.FromMilliseconds(40);

    /// <summary>Start and Back held together this long clear everything back to Combat.</summary>
    public static readonly TimeSpan PanicHold = TimeSpan.FromMilliseconds(500);

    private const GamepadButtons PanicChord = GamepadButtons.Start | GamepadButtons.Back;

    private readonly ModeStack stack = new();
    private readonly IReadOnlyDictionary<Mode, KeyCode> panelKeys;
    private readonly Func<Mode, ButtonLayers> layersFor;
    private readonly Queue<KeyCode> taps = new();
    private KeyCode? tapping;
    private TimeSpan tapHeldFor;
    private TimeSpan panicHeldFor;
    private bool panicFired;
    private GamepadButtons previous;

    private readonly GridNavigator? grids;

    /// <summary>True while grid presses are stepping because the screen could not be read; shown on the overlay.</summary>
    public bool GridFellBack => grids is not null && grids.FellBack;

    public ModeController(IReadOnlyDictionary<Mode, KeyCode> panelKeys, Func<Mode, ButtonLayers> layersFor, GridNavigator? grids = null)
    {
        this.panelKeys = panelKeys;
        this.layersFor = layersFor;
        this.grids = grids;
    }

    public Mode Current => stack.Current;

    public int Depth => stack.Depth;

    /// <summary>The binding tables for the mode the game is believed to be in.</summary>
    public ButtonLayers CurrentLayers => layersFor(stack.Current);

    public List<InputIntent> Tick(GamepadSnapshot snapshot, TimeSpan elapsed)
    {
        List<InputIntent> intents = new();
        ReleaseFinishedTap(elapsed, intents);

        GamepadButtons pressed = snapshot.Buttons & ~previous;
        previous = snapshot.Buttons;
        ButtonLayers layers = layersFor(stack.Current);

        foreach (GamepadButtons button in Enum.GetValues<GamepadButtons>())
        {
            if (button != GamepadButtons.None && (pressed & button) != 0)
            {
                ButtonBinding? binding = layers.BindingFor(button, snapshot.LeftTrigger);

                if (binding is GridBinding grid && grids?.Act(stack.Current, grid.Action) is PointerMoveTo move)
                {
                    intents.Add(move);
                }

                if (binding is TabBinding tab && grids is not null)
                {
                    intents.AddRange(grids.Tab(stack.Current, tab.Step));
                }

                Act(binding);
            }
        }

        WatchPanic(snapshot, elapsed);

        if (tapping is null && taps.Count > 0)
        {
            tapping = taps.Dequeue();
            tapHeldFor = TimeSpan.Zero;
            intents.Add(new KeyDown(tapping.Value));
        }

        return intents;
    }

    /// <summary>
    /// Drops queued and in-flight taps after a suspension has released
    /// everything. The panels stay as they were, since suspension closes none.
    /// </summary>
    public void Reset()
    {
        taps.Clear();
        tapping = null;
        previous = GamepadButtons.None;
    }

    private void ReleaseFinishedTap(TimeSpan elapsed, List<InputIntent> intents)
    {
        if (tapping is not KeyCode held)
        {
            return;
        }

        tapHeldFor += elapsed;

        if (tapHeldFor >= TapLength)
        {
            intents.Add(new KeyUp(held));
            tapping = null;
        }
    }

    private void Act(ButtonBinding? binding)
    {
        if (binding is PanelBinding panel)
        {
            stack.Toggle(panel.Mode);

            if (panelKeys.TryGetValue(panel.Mode, out KeyCode key))
            {
                taps.Enqueue(key);
            }
        }
        else if (binding is BackBinding)
        {
            // Escape with nothing open brings up the game's menu.
            if (!stack.Pop())
            {
                stack.Toggle(Mode.Menu);
            }

            taps.Enqueue(KeyCode.Escape);
        }
    }

    /// <summary>
    /// The recovery path for when tracked and real state disagree: one Escape
    /// per tracked panel, then Combat. Fires once per hold.
    /// </summary>
    private void WatchPanic(GamepadSnapshot snapshot, TimeSpan elapsed)
    {
        if (!snapshot.IsPressed(PanicChord))
        {
            panicHeldFor = TimeSpan.Zero;
            panicFired = false;
            return;
        }

        panicHeldFor += elapsed;

        if (panicFired || panicHeldFor < PanicHold)
        {
            return;
        }

        panicFired = true;

        for (int panel = 0; panel < stack.Depth; panel++)
        {
            taps.Enqueue(KeyCode.Escape);
        }

        stack.ClearToCombat();
    }
}
