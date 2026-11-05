namespace GDPilot.Core;

/// <summary>A grid action from a binding: "grid:up", "grid:down", "grid:left", "grid:right" or "grid:next".</summary>
public sealed record GridBinding(string Action) : ButtonBinding;

/// <summary>"tab:next" or "tab:previous": clicks the neighbouring stash tab. Step is +1 or -1.</summary>
public sealed record TabBinding(int Step) : ButtonBinding;

/// <summary>"camera:toggle": switches the in-game part between the game's camera and third person.</summary>
public sealed record CameraToggleBinding : ButtonBinding;

/// <summary>
/// Moves the cursor over the grids calibrated for the current mode. A mode can
/// have several surfaces, such as the main bag and the second bag, named
/// "Inventory" and "Inventory.Bag2"; "grid:next" moves between them. With an
/// occupancy source, presses jump from item to item over empty cells; without
/// one, or when a reading is in doubt, they step one cell at a time.
/// </summary>
public sealed class GridNavigator
{
    private const string TabsSuffix = ".Tabs";

    /// <summary>The surface a mode's spots make, first among its grids; no grid name is empty.</summary>
    private const string SpotsSurface = "";

    private readonly GridReadiness readiness;
    private readonly IOccupancySource? occupancy;
    private readonly ISpotSource? spotSource;
    private readonly Dictionary<string, OccupancyMap> known = new();
    private readonly Dictionary<Mode, int> tabs = new();
    private Mode mode = Mode.Suspended;
    private List<string> surfaces = new();
    private int surface;
    private GridCell cell;
    private bool placed;
    private ScreenPoint spot;
    private SpotArea? snapping;
    private readonly List<ScreenPoint> seen = new();

    public GridNavigator(GridReadiness readiness, IOccupancySource? occupancy = null, ISpotSource? spotSource = null)
    {
        this.readiness = readiness;
        this.occupancy = occupancy;
        this.spotSource = spotSource;
    }

    /// <summary>Whether the last press jumped between items, rather than stepping a cell.</summary>
    public bool Jumping { get; private set; }

    /// <summary>Whether jumping is on but the last press in this mode had to step, because the screen could not be trusted.</summary>
    public bool FellBack => occupancy is not null && placed && !Jumping;

    /// <summary>Why the last grid press did nothing, or null when it worked.</summary>
    public string? Notice { get; private set; }

    /// <summary>Where the cursor should go for this press, or null when navigation is unavailable.</summary>
    public PointerMoveTo? Act(Mode current, string action)
    {
        if (!readiness.Ready)
        {
            Notice = readiness.Message;
            return null;
        }

        if (current != mode)
        {
            // Each mode starts fresh on its first surface, since its panel may sit elsewhere on screen.
            mode = current;

            // A tab strip is a one-row grid too, but it is clicked through, not walked.
            surfaces = readiness.Grids.Keys
                .Where(name => name == current.ToString() || name.StartsWith(current + ".", StringComparison.Ordinal))
                .Where(name => !name.EndsWith(TabsSuffix, StringComparison.Ordinal))
                .Order()
                .ToList();

            // A crafter lists recipes beside the bags, so spots and grids can share a mode. Things
            // to read come first; fixed places, such as the equipment beside the bags, come last.
            if (spotSource is not null)
            {
                AddSpotsSurface(current);
            }

            surface = 0;
            placed = false;
            snapping = null;
            seen.Clear();
        }

        if (surfaces.Count == 0)
        {
            Notice = $"No grid is calibrated for {current}.";
            return null;
        }

        if (action == "next")
        {
            surface = (surface + 1) % surfaces.Count;
            placed = false;
        }

        if (surfaces[surface] == SpotsSurface)
        {
            SpotArea? area = AreaShowing(current);

            if (area is null)
            {
                Notice = $"Nothing in {current} is known to snap to on this screen.";
                return null;
            }

            return Snap(area, action);
        }

        GridSpec grid = readiness.Grids[surfaces[surface]];
        OccupancyMap? map = Occupancy(surfaces[surface], grid);
        Jumping = map is not null;

        if (!placed)
        {
            // The first press lands on the first item, or the top-left cell, rather than stepping from wherever the cursor was.
            cell = map?.FirstItem() ?? new GridCell(0, 0);
        }
        else if (map is null)
        {
            cell = grid.Step(cell, Direction(action));
        }
        else
        {
            cell = map.NextItem(cell, Direction(action)) ?? cell;
        }

        placed = true;
        Notice = null;
        (int x, int y) = grid.Centre(cell);
        return new PointerMoveTo(x, y);
    }

    /// <summary>
    /// Clicks the next or previous tab, wrapping round, for a mode whose profile
    /// has a tab strip calibrated as a one-row grid named "Stash.Tabs". The game
    /// switches stash tabs only by clicking them, so a tab change is a move and a click.
    /// </summary>
    public List<InputIntent> Tab(Mode current, int step)
    {
        List<InputIntent> intents = new();

        if (!readiness.Ready)
        {
            Notice = readiness.Message;
            return intents;
        }

        if (!readiness.Grids.TryGetValue(current + TabsSuffix, out GridSpec? strip))
        {
            Notice = $"No tabs are calibrated for {current}.";
            return intents;
        }

        // The stash draws its tabs as a column down its left edge; the skill window, as a row along its top.
        int count = Math.Max(strip.Columns, strip.Rows);
        Mode window = TabbedWindow(current);
        tabs.TryGetValue(window, out int tab);

        // The game remembers each window's tab, and the cursor can change it, so the screen is believed first.
        if (spotSource?.SelectedTab(strip) is int selected)
        {
            tab = selected;
        }

        tab = ((tab + step) % count + count) % count;
        tabs[window] = tab;
        GridCell cell = new(tab, 0);

        if (strip.Columns == 1)
        {
            cell = new GridCell(0, tab);
        }

        // Another tab shows other buttons, so the ones seen on this one no longer apply.
        seen.Clear();
        (int x, int y) = strip.Centre(cell);
        intents.Add(new PointerMoveTo(x, y));
        intents.Add(new PointerDown(PointerButton.Left));
        intents.Add(new PointerUp(PointerButton.Left));
        Notice = null;
        return intents;
    }

    /// <summary>Starts over, as when a window has closed and whatever opens next may show something else.</summary>
    public void Forget()
    {
        mode = Mode.Suspended;
    }

    private void AddSpotsSurface(Mode current)
    {
        List<SpotArea> areas = AreasOf(current);

        if (areas.Count == 0)
        {
            return;
        }

        if (areas.All(area => area.Kind == SpotKind.Places) && surfaces.Count > 0)
        {
            surfaces.Add(SpotsSurface);
            return;
        }

        surfaces.Insert(0, SpotsSurface);
    }

    /// <summary>A mode's own spots and any others it shows, named like "Menu.Options".</summary>
    private List<SpotArea> AreasOf(Mode current)
    {
        return readiness.Spots
            .Where(pair => pair.Key == current.ToString() || pair.Key.StartsWith(current + ".", StringComparison.Ordinal))
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => pair.Value)
            .ToList();
    }

    /// <summary>The spots for what the mode has on screen: those whose frame shows, else those that need none.</summary>
    private SpotArea? AreaShowing(Mode current)
    {
        List<SpotArea> areas = AreasOf(current);

        foreach (SpotArea area in areas)
        {
            if (area.When is FrameLine line && spotSource!.Shows(line))
            {
                return area;
            }
        }

        return areas.FirstOrDefault(area => area.When is null);
    }

    /// <summary>
    /// Snaps to the menu's spots as the screen shows them at this press, since a
    /// skill tree changes with the tab showing. "grid:next" starts over from the
    /// first spot.
    /// </summary>
    private PointerMoveTo? Snap(SpotArea area, string action)
    {
        // Another screen of the same mode starts over, as its spots have nothing to do with the last one's.
        if (!ReferenceEquals(area, snapping))
        {
            snapping = area;
            placed = false;
            seen.Clear();
        }

        List<ScreenPoint>? found = spotSource!.Read(area);

        if (found is null)
        {
            Notice = "The screen could not be read to find this menu's buttons.";
            return null;
        }

        // Buttons stay put while a tooltip hides them; a conversation's replies change with every step.
        if (area.Kind == SpotKind.Buttons && area.Look is not null)
        {
            Remember(found, area.Look.Spacing);
        }
        else
        {
            seen.Clear();
            seen.AddRange(found);
        }

        List<ScreenPoint> spots = new(seen);
        spots.AddRange(area.Fixed);
        ScreenPoint? target;

        if (action == "next" || !placed)
        {
            target = SpotNavigator.First(spots);
        }
        else
        {
            target = SpotNavigator.Next(spots, spot, Direction(action));

            if (target is null)
            {
                target = spot;
            }
        }

        if (target is null)
        {
            Notice = "Nothing to snap to in this menu.";
            return null;
        }

        spot = target.Value;
        placed = true;
        Jumping = true;
        Notice = null;
        return new PointerMoveTo(spot.X, spot.Y);
    }

    /// <summary>
    /// Keeps every spot seen since the menu or its tab last changed. The tooltip
    /// of the button under the cursor covers its neighbours, which must not
    /// vanish from reach because of it.
    /// </summary>
    private void Remember(List<ScreenPoint> found, int spacing)
    {
        // A tooltip hides a few; most gone means another tree is showing, as after a tab clicked with the cursor.
        int stillThere = seen.Count(old => found.Any(point => Near(old, point, spacing)));

        if (found.Count > 0 && stillThere * 2 < seen.Count)
        {
            seen.Clear();
        }

        foreach (ScreenPoint point in found)
        {
            bool known = seen.Any(old => Near(old, point, spacing));

            if (!known)
            {
                seen.Add(point);
            }
        }
    }

    private static bool Near(ScreenPoint first, ScreenPoint second, int spacing)
    {
        int dx = first.X - second.X;
        int dy = first.Y - second.Y;
        return dx * dx + dy * dy <= spacing * spacing;
    }

    /// <summary>
    /// Whose tab count a mode moves. The devotion map is the skill window's
    /// middle tab, drawn with the window's tabs along the top of the screen
    /// instead, so the two count their tabs together.
    /// </summary>
    private static Mode TabbedWindow(Mode current)
    {
        if (current == Mode.Devotion)
        {
            return Mode.Skills;
        }

        return current;
    }

    /// <summary>A fresh, trusted reading of a surface, or null to step instead.</summary>
    private OccupancyMap? Occupancy(string name, GridSpec grid)
    {
        if (occupancy is null)
        {
            return null;
        }

        known.TryGetValue(name, out OccupancyMap? previous);
        OccupancyMap? map = occupancy.Read(name, grid, previous);

        if (map is null || !map.IsConfident)
        {
            return null;
        }

        known[name] = map;
        return map;
    }

    private static GridDirection Direction(string action)
    {
        switch (action)
        {
            case "up":
                return GridDirection.Up;
            case "down":
                return GridDirection.Down;
            case "left":
                return GridDirection.Left;
            default:
                return GridDirection.Right;
        }
    }
}
