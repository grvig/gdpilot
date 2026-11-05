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

    /// <summary>
    /// How long after a tap the game's own reports are ignored. They are read
    /// about ten times a second, so one sent just after a tap still describes
    /// the game from before it.
    /// </summary>
    public static readonly TimeSpan ReportSettle = TimeSpan.FromMilliseconds(300);

    private readonly ModeStack stack = new();
    private readonly IReadOnlyDictionary<Mode, KeyCode> panelKeys;
    private readonly Func<Mode, ButtonLayers> layersFor;
    private readonly Queue<KeyCode> taps = new();
    private KeyCode? tapping;
    private TimeSpan tapHeldFor;
    private TimeSpan panicHeldFor;
    private bool panicFired;
    private GamepadButtons previous;
    private TimeSpan sinceTap = TimeSpan.MaxValue;
    private TimeSpan? devotionTabIn;
    private GamepadButtons slotListClick;

    /// <summary>How long after the skill window's key its tabs can be clicked: the tap, and the window opening.</summary>
    private static readonly TimeSpan DevotionTabDelay = TimeSpan.FromMilliseconds(350);

    private const int DevotionTab = 1;

    private readonly GridNavigator? grids;
    private Mode navigatedFor = Mode.Combat;

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

    /// <summary>The game's list of what a hotbar slot can hold has opened; nothing the game reports says so.</summary>
    public void OpenSlotList()
    {
        if (stack.Current == Mode.Combat)
        {
            stack.Toggle(Mode.Assign);
            slotListClick = GamepadButtons.None;
        }
    }

    public List<InputIntent> Tick(GamepadSnapshot snapshot, TimeSpan elapsed)
    {
        List<InputIntent> intents = new();
        ReleaseFinishedTap(elapsed, intents);

        if (sinceTap != TimeSpan.MaxValue)
        {
            sinceTap += elapsed;
        }

        GamepadButtons pressed = snapshot.Buttons & ~previous;
        previous = snapshot.Buttons;
        ButtonLayers layers = layersFor(stack.Current);

        // A window reopened may show something else, even with no press made in another mode between.
        if (stack.Current != navigatedFor)
        {
            navigatedFor = stack.Current;
            grids?.Forget();
        }

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

                if (binding is CameraToggleBinding)
                {
                    intents.Add(new GameCommandIntent(new ToggleCameraCommand()));
                }

                Act(binding);
            }
        }

        WatchPanic(snapshot, elapsed);
        ClickDevotionTab(elapsed, intents);
        WatchSlotList(pressed, snapshot, layers);

        if (tapping is null && taps.Count > 0)
        {
            tapping = taps.Dequeue();
            tapHeldFor = TimeSpan.Zero;
            sinceTap = TimeSpan.Zero;
            intents.Add(new KeyDown(tapping.Value));
        }

        return intents;
    }

    /// <summary>
    /// Brings the tracked mode in line with the panels the game itself says are
    /// open, which ends any disagreement: a panel closed with the mouse, or
    /// opened by clicking something in the world, is followed. Ignored while a
    /// tap is on its way, since the report may predate it.
    /// </summary>
    public void Follow(IReadOnlyList<string> open)
    {
        if (tapping is not null || taps.Count > 0 || sinceTap < ReportSettle)
        {
            return;
        }

        List<Mode> modes = new();

        foreach (string name in open)
        {
            if (Enum.TryParse(name, out Mode mode) && mode is not (Mode.Combat or Mode.Suspended))
            {
                modes.Add(mode);
            }
        }

        if (Agrees(modes))
        {
            return;
        }

        stack.ClearToCombat();

        if (modes.Count == 0)
        {
            return;
        }

        // The menu and a conversation sit over everything else, and a question over those, so they are what the buttons should drive.
        Mode shown = modes[0];

        foreach (Mode mode in modes)
        {
            if (mode is Mode.Menu or Mode.Dialog && shown != Mode.Confirm)
            {
                shown = mode;
            }

            if (mode == Mode.Confirm)
            {
                shown = mode;
            }
        }

        // What a question was asked over stays beneath it, so answering returns there at once.
        if (shown == Mode.Confirm)
        {
            foreach (Mode mode in modes)
            {
                if (mode != Mode.Confirm)
                {
                    stack.Toggle(mode);
                }
            }
        }

        stack.Toggle(shown);
    }

    /// <summary>
    /// Whether the tracked mode matches the game's panels. The game raises its
    /// inventory flag for the stash, a trader and the character sheet as well,
    /// so any of those agrees with an open inventory.
    /// </summary>
    private bool Agrees(List<Mode> open)
    {
        // The game reports nothing for a slot's list, and a question could not arrive over one.
        if (stack.Current == Mode.Assign)
        {
            return true;
        }

        if (open.Count == 0)
        {
            return stack.Current == Mode.Combat;
        }

        // A question takes the buttons from whatever it was asked over.
        if (open.Contains(Mode.Confirm))
        {
            return stack.Current == Mode.Confirm;
        }

        if (open.Contains(stack.Current))
        {
            return true;
        }

        return open.Contains(Mode.Inventory) && stack.Current is Mode.Stash or Mode.Vendor or Mode.Crafting or Mode.Character;
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

    /// <summary>A click on the list closes it, once the button that clicked has come back up.</summary>
    private void WatchSlotList(GamepadButtons pressed, GamepadSnapshot snapshot, ButtonLayers layers)
    {
        if (stack.Current != Mode.Assign)
        {
            return;
        }

        foreach (GamepadButtons button in Enum.GetValues<GamepadButtons>())
        {
            if (button != GamepadButtons.None && (pressed & button) != 0 && layers.BindingFor(button, snapshot.LeftTrigger) is PointerBinding)
            {
                slotListClick = button;
            }
        }

        if (slotListClick != GamepadButtons.None && (snapshot.Buttons & slotListClick) == 0)
        {
            slotListClick = GamepadButtons.None;
            stack.Pop();
        }
    }

    private void ClickDevotionTab(TimeSpan elapsed, List<InputIntent> intents)
    {
        if (devotionTabIn is not TimeSpan left)
        {
            return;
        }

        left -= elapsed;
        devotionTabIn = left;

        if (left > TimeSpan.Zero)
        {
            return;
        }

        devotionTabIn = null;

        if (grids is not null)
        {
            intents.AddRange(grids.ClickTab(Mode.Skills, DevotionTab));
        }
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
            else if (panel.Mode == Mode.Devotion && panelKeys.TryGetValue(Mode.Skills, out KeyCode skills) && stack.Current == Mode.Devotion)
            {
                // No key opens the devotion map: it is the skill window's middle tab, clicked once the window is up.
                taps.Enqueue(skills);
                devotionTabIn = DevotionTabDelay;
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
