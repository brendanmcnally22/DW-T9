using System;
using System.Collections.Generic;
using System.Threading;

// Single-room console escape: pickup / inspect / inventory, typewriter text, 5-min timer.
// "back" always returns to the main page; command menu is always visible.
// Enhanced keyhole puzzle: "use key" -> reveals hidden panel; "inspect panel" / "reach" -> a coin drops.

/// <summary>
/// Simple, self-contained console escape in a Dusty Library.
/// </summary>
class Program
{
    // ---------------------------
    //        CONFIG / TUNING
    // ---------------------------

    // Milliseconds between characters for the typewriter effect (set 0 for instant text).
    static int TYPE_DELAY_MS = 10;

    // Global time limit for the whole session (seconds).
    static int TIME_LIMIT_SEC = 300; // 5 minutes

    // ---------------------------
    //          TIMER STATE
    // ---------------------------

    // Counts how many elapsed seconds since the game began.
    static int elapsedSeconds = 0;

    // Flip to true when time is up; marked volatile so other threads see changes immediately.
    static volatile bool timeUp = false;

    // ---------------------------
    //           UI STATE
    // ---------------------------

    // Lightweight "page" state. "main" = hub; other strings represent transient screens.
    // Typing "back" always returns this to "main".
    static string page = "main";

    // Entry point
    static void Main()
    {
        // ---------------------------
        //         WORLD SETUP
        // ---------------------------

        // Create a single room with a couple of items scattered about.
        Room library = new("Dusty Library");
        library.Items.Add(new Item("Rusty Key", "An old iron key, slightly bent.", 50));
        library.Items.Add(new Item("Crumpled Note", "It reads: 'The bookshelf breathes...'", 20));

        // Create the player (just an inventory container for now).
        Player player = new();

        // ---------------------------
        //        START TIMER
        // ---------------------------

        // Background thread increments a counter every second and flips timeUp when limit is reached.
        new Thread(TimerLoop) { IsBackground = true }.Start();

        // ---------------------------
        //       INITIAL RENDER
        // ---------------------------

        Clear();
        RenderFrame(library);
        Say("You awaken in a dusty library. Something feels off.");

        // ---------------------------
        //         MAIN LOOP
        // ---------------------------
        // Reads commands until timeUp is true (or user types "quit").
        while (!timeUp)
        {
            Console.Write("\n> ");
            string? input = Console.ReadLine();
            if (input == null) continue;

            // Normalize input for simpler matching (e.g., "Look", "LOOK", " look " → "look").
            input = input.Trim().ToLowerInvariant();

            // SPECIAL: "back" always succeeds first and returns us to the main hub.
            if (input == "back")
            {
                page = "main";
                Clear();
                RenderFrame(library);
                continue;
            }

            // Always reprint header + command menu before processing results, so options never "disappear".
            Clear();
            RenderFrame(library);
            if (timeUp) break; // If we ran out of time while drawing, bail.

            // ---------------------------
            //     COMMAND HANDLERS
            // ---------------------------

            if (input == "look")
            {
                page = "look"; // transient screen
                if (library.Items.Count == 0)
                {
                    Say("Nothing else catches your eye.");
                }
                else
                {
                    Say("You see:");
                    foreach (var it in library.Items) Say(" - " + it.Name);
                }
            }
            else if (input.StartsWith("inspect "))
            {
                page = "inspect";
                string q = input[8..].Trim(); // substring after "inspect "
                if (!Inspect(library, q)) Say("You see nothing special.");
            }
            else if (input.StartsWith("pickup "))
            {
                page = "pickup";
                string q = input[7..].Trim(); // substring after "pickup "
                var it = Find(library.Items, q);
                if (it != null)
                {
                    library.Items.Remove(it);
                    player.Inventory.Add(it);
                    Say($"You picked up {it.Name}.");
                }
                else Say("Nothing like that here.");
            }
            else if (input == "inventory" || input == "inv")
            {
                page = "inventory";
                if (player.Inventory.Count == 0) Say("Inventory empty.");
                else
                {
                    Say("You carry:");
                    foreach (var it in player.Inventory) Say(" - " + it.Name);
                }
            }
            else if (input == "use key")
            {
                page = "use";

                // Puzzle step: if you have the rusty key, you can open the hidden shelf panel.
                if (Has(player, "rusty key"))
                {
                    if (!library.PanelOpen)
                    {
                        library.PanelOpen = true;
                        Say("You slide the key into a hairline slot. A soft click… the shelf shudders.");
                        Say("A slim panel slides aside, revealing a narrow, dust-choked cavity.");
                        Say("(Try 'inspect panel' or 'reach')");
                    }
                    else
                    {
                        Say("The hidden panel is already open. A cold draft seeps out.");
                    }
                }
                else Say("You pat your pockets—no key.");
            }
            else if (input == "inspect panel")
            {
                page = "inspect-panel";

                // Feedback depends on whether the panel is open and whether the prize has dropped yet.
                if (library.PanelOpen && !library.PrizeDropped)
                {
                    Say("The cavity is too dark to see the back. Something metallic chinks faintly when you tap the wood.");
                    Say("(You could try 'reach'.)");
                }
                else if (library.PanelOpen && library.PrizeDropped)
                {
                    Say("The panel gapes, the cavity now empty. Whatever was inside has fallen out.");
                }
                else
                {
                    Say("You don't see any panel—just uneven books and dust.");
                }
            }
            else if (input == "reach")
            {
                page = "reach";

                // Reaching only matters if the panel is open. First time dislodges a coin.
                if (!library.PanelOpen)
                {
                    Say("You grope at random gaps in the shelves. Just splinters and dust.");
                }
                else if (!library.PrizeDropped)
                {
                    Say("You slide an arm into the cavity. Cobwebs cling. Your fingers brush cold metal…");
                    TryBeep(700, 60);  // little 'found something' audio stingers (best-effort)
                    TryBeep(1000, 80);
                    Say("Something dislodges and drops to the floor at your feet.");
                    library.PrizeDropped = true;
                    library.Items.Add(new Item("Silver Coin", "An old coin, tarnished but weighty.", 100));
                    Say("(Try 'look' or 'pickup coin')");
                }
                else
                {
                    Say("There's nothing else within reach.");
                }
            }
            else if (input == "quit")
            {
                Clear();
                Say("Thanks for playing!");
                return; // Hard exit by user.
            }
            else if (input == "help")
            {
                page = "help";
                Say("Try: look | inspect <name> | pickup <name> | inventory | use key | inspect panel | reach | back | quit");
            }
            else
            {
                // Unknown input: gently nudge back to main hub and show help.
                page = "main";
                Say("Unknown command. Type 'help'.");
            }
        }

        // ---------------------------
        //         TIME EXPIRED
        // ---------------------------

        Clear();
        Say("⏳ Time’s up! The room fades to black...");
        Say("GAME OVER");
    }

    // -------------------------------------------------------------
    //                    BACKGROUND TIMER LOOP
    // -------------------------------------------------------------
    // Sleeps 1 second at a time until TIME_LIMIT_SEC is reached.
    // When done, flips the global 'timeUp' flag read by the main loop.
    static void TimerLoop()
    {
        while (elapsedSeconds < TIME_LIMIT_SEC)
        {
            Thread.Sleep(1000);
            elapsedSeconds++;
        }
        timeUp = true;
    }

    // -------------------------------------------------------------
    //                     RENDERING HELPERS
    // -------------------------------------------------------------

    // Prints the room header and a persistent command menu.
    // If we're on the "main" page, also prints a short blurb and any panel hint.
    static void RenderFrame(Room room)
    {
        Header(room.Name);

        // Persistent command list: never disappears, so players always know what to try.
        Console.WriteLine("Commands: look | inspect <name> | pickup <name> | inventory | use key | inspect panel | reach | back | quit\n");

        // Main hub gets a simple description + dynamic hint if the panel is open.
        if (page == "main")
        {
            Say("Shelves loom around you. Dust swirls in the lantern glow.");
            if (room.PanelOpen && !room.PrizeDropped)
                Say("A slim panel stands ajar in the shelf, breathing a cold draft.");
        }
    }

    // Prints a nice header block with the game title and current room.
    static void Header(string roomName)
    {
        Console.WriteLine("========================================");
        Console.WriteLine($"   ESCAPE THE MANSION - {roomName}");
        Console.WriteLine("========================================\n");
    }

    // Attempts to clear the console; if not supported (or in some IDEs), prints a separator instead.
    static void Clear()
    {
        try { Console.Clear(); }
        catch { Console.WriteLine("\n----------------------------------------\n"); }
    }

    // Typewriter print helper; honors TYPE_DELAY_MS. If 0, prints instantly.
    static void Say(string text)
    {
        if (TYPE_DELAY_MS <= 0) { Console.WriteLine(text); return; }
        foreach (char c in text) { Console.Write(c); Thread.Sleep(TYPE_DELAY_MS); }
        Console.WriteLine();
    }

    // Best-effort beep; ignored if the platform doesn't support Beep.
    static void TryBeep(int f, int ms)
    {
        try { Console.Beep(f, ms); } catch { /* not supported everywhere */ }
    }

    // -------------------------------------------------------------
    //                   INTERACTION HELPERS
    // -------------------------------------------------------------

    // Finds an item in a list by exact or contains match (case-insensitive).
    static Item? Find(List<Item> items, string q)
    {
        q = q.ToLowerInvariant();
        return items.Find(i =>
            i.Name.ToLowerInvariant() == q ||
            i.Name.ToLowerInvariant().Contains(q));
    }

    // Checks if the player has an item by name (exact or contains).
    static bool Has(Player p, string q)
    {
        q = q.ToLowerInvariant();
        return p.Inventory.Exists(i =>
            i.Name.ToLowerInvariant() == q ||
            i.Name.ToLowerInvariant().Contains(q));
    }

    // Handles "inspect <target>" logic: tries items first, then special hotspots (bookshelf/panel).
    // Returns true if we printed something meaningful; false if nothing matched.
    static bool Inspect(Room room, string target)
    {
        target = target.ToLowerInvariant();

        // 1) Try to inspect a loose item in the room.
        var it = Find(room.Items, target);
        if (it != null) { Say(it.Description); return true; }

        // 2) Special hotspot: bookshelf
        if (target.Contains("shelf") || target.Contains("bookshelf"))
        {
            if (room.PanelOpen)
            {
                Say("The shelf sits crooked with the panel ajar. A thin draft threads from the dark seam.");
                Say("(Try 'inspect panel' or 'reach')");
            }
            else
            {
                Say("The bookshelf seems to breathe... a faint draft slips through the gaps.");
                Say("Feeling along the edge, you find a tiny hidden keyhole.");
            }
            return true;
        }

        // 3) Special hotspot: panel (mirrors dedicated 'inspect panel' handler)
        if (target.Contains("panel"))
        {
            if (room.PanelOpen && !room.PrizeDropped)
                Say("A narrow, soot-dark cavity hides behind the panel. You can't see the back.");
            else if (room.PanelOpen && room.PrizeDropped)
                Say("The cavity is empty now. Only cold air whispers out.");
            else
                Say("You don't see any panel—just uneven books and dust.");
            return true;
        }

        // Nothing matched.
        return false;
    }
}

// -------------------------------------------------------------
//                       DATA MODELS
// -------------------------------------------------------------

// Tracks what the player carries.
class Player
{
    public List<Item> Inventory = new();
}

// Describes the single environment for this vignette.
// PanelOpen: after "use key" succeeds.
// PrizeDropped: after "reach" dislodges the coin.
class Room
{
    public string Name;
    public List<Item> Items = new();
    public bool PanelOpen = false;    // becomes true after 'use key'
    public bool PrizeDropped = false; // becomes true after 'reach'
    public Room(string name) { Name = name; }
}

// Generic item: a name, a description for "inspect", and an optional points value for scoring hooks.
class Item
{
    public string Name;
    public string Description;
    public int Points;
    public Item(string name, string description, int points)
    {
        Name = name; Description = description; Points = points;
    }
}
