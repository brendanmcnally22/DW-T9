using System;
using System.Threading;

namespace DW_T9.Game
{
    internal sealed class Game
    {
        // Core services
        private readonly UI _ui;
        private readonly Timer _timer;
        private readonly Player _player;

        // Simple parser (Verb + Noun)
        private readonly CommandRouter _router = new CommandRouter();

        // World state
        private string _room = "foyer";     // foyer → living → hallway → (bedroom | kitchen) → escape
        private bool _livingSolved = false; // gate to hallway
        private bool _bedroomSolved = false;
        private bool _clockSolved = false;  // solved IN hallway

        // Kitchen (false puzzle) state
        private bool _kitchenHintGiven = false;
        private bool _kitchenJumpscareUsed = false;

        public Game(GameContext ctx)
        {
            _ui = ctx.UI;
            _timer = ctx.Timer;
            _player = ctx.Player;
        }

        public void Run()
        {
            // Menu first (timer starts AFTER play)
            _ui.Clear();
            _ui.ShowIntro();

            while (true)
            {
                Console.Write("\n[Menu] Type 'play', 'help', or 'quit': ");
                var s = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();

                if (s == "play") break;

                if (s == "help")
                {
                    _ui.Type("Two-player setup:");
                    _ui.Type("- Player 1: terminal (you)");
                    _ui.Type("- Player 2: physical diorama/map");
                    _ui.Type("Talk to each other with the Walkie talkies. Path is linear until the hallway, then choose Bedroom or Kitchen. The hallway clock is solved in-place.");
                    continue;
                }

                if (s == "quit")
                {
                    _ui.Type("Goodbye!");
                    return;
                }

                _ui.Toast("Unknown option. Type 'play', 'help', or 'quit'.");
            }

            // Start systems AFTER 'play'
            _timer.Start();

            // First frame + description
            _ui.Clear();
            _ui.Frame(RoomTitle(), _timer.RemainingSeconds, CollectedCardsCount(), 4);
            DescribeRoom(); // leave this on screen until next input

            // Main loop
            while (true)
            {
                Console.Write("\n> ");
                _router.Parse(Console.ReadLine() ?? "");

                // Clear + draw HUD fresh each input
                _ui.Clear();
                _ui.Frame(RoomTitle(), _timer.RemainingSeconds, CollectedCardsCount(), 4);

                if (_timer.Expired)
                {
                    _ui.Type("⏳ Time’s up! The mansion swallows the light…");
                    break;
                }

                // -------- Global commands --------
                if (_router.Verb == "quit")
                {
                    _ui.Type("Thanks for playing!");
                    break;
                }
                else if (_router.Verb == "help")
                {
                    _ui.Type("Commands: look | inspect <x> | pickup <x> | inventory | cards | use <x> | enter <ans> | set clock h:mm | light candles | go <room> | back | help | quit");
                    continue;
                }
                else if (_router.Verb == "inventory" || _router.Verb == "inv" || _router.Verb == "bag")
                {
                    _ui.Type(_player.Inventory.ListAll());
                    _ui.Type(CollectedCardsCount() == 0 ? "Cards: (none)" : "Cards: " + string.Join(", ", _player.CollectedCards));
                    continue;
                }
                else if (_router.Verb == "cards")
                {
                    _ui.Type(CollectedCardsCount() == 0 ? "Cards: (none)" : "Cards: " + string.Join(", ", _player.CollectedCards));
                    continue;
                }
                else if (_router.Verb == "back")
                {
                    // Walk back along the linear route
                    if (_room == "living") _room = "foyer";
                    else if (_room == "hallway") _room = "living";
                    else if (_room == "bedroom" || _room == "kitchen") _room = "hallway";
                    else _room = "foyer";

                    DescribeRoom();
                    continue;
                }
                else if (_router.Verb == "go")
                {
                    HandleGo();
                    continue;
                }

                // -------- Room-specific handling --------
                switch (_room)
                {
                    case "foyer": HandleFoyer(); break;
                    case "living": HandleLiving(); break;
                    case "hallway": HandleHallway(); break; // clock puzzle lives here
                    case "bedroom": HandleBedroom(); break;
                    case "kitchen": HandleKitchen(); break; // false puzzle + hint
                    case "escape":
                        _ui.Type("Night air floods in—freedom!");
                        return;

                    default:
                        _ui.Toast("You feel… lost? (unknown room)");
                        break;
                }

                // Auto-escape once all 4 cards are held (can happen right after a solve)
                if (CollectedCardsCount() >= 4 && _room != "escape")
                {
                    _room = "escape";
                    _ui.Type("The main door groans open—your cards resonate in the stone. YOU ESCAPE!");
                    break;
                }
            }

            if (_timer.Expired)
            {
                _ui.Clear();
                _ui.Type("GAME OVER");
            }
        }

        // ----------------- Room logic (skeleton) -----------------

        private void HandleFoyer()
        {
            if (_router.Verb == "look")
            {
                _ui.Type("FOYER: You’re a pizza guy who delivered to the wrong house. The air is stale.");
                _ui.Type("Ahead, a Skeleton and a door to what seems to be a \"living\" room.");
                _ui.Type("Try: tutorial | go \"living\"");
                return;
            }

            if (_router.Verb == "tutorial")
            {
                if (!_player.HasCard(CardType.Beetle))
                {
                    _player.AddCard(CardType.Beetle); // first/tutorial card
                    _ui.Type("Player 2 hands you a carved \"beetle\" card.");
                }
                else
                {
                    _ui.Hint("You already took the tutorial card.");
                }
                return;
            }

            _ui.Hint("Try: look | tutorial | go \"living\"");
        }

        private void HandleLiving()
        {
            if (_router.Verb == "look")
            {
                _ui.Type("LIVING ROOM: A scuffed floor \"grid\" (3x3) with a loose \"board\". A dusty \"book\" rests nearby.");
                _ui.Type("Linear step: solve the grid to proceed.");
                _ui.Type("For this skeleton: type 'enter b3' → awards \"rat\" card and unlocks the hallway.");
                return;
            }

            if (_router.Verb == "enter")
            {
                var guess = _router.Noun.Trim();
                if (guess == "b3")
                {
                    if (!_livingSolved)
                    {
                        _livingSolved = true;
                        if (!_player.HasCard(CardType.Rat)) _player.AddCard(CardType.Rat);
                        _ui.Type("You lift the board at B3. A recess holds a \"rat\" card. Doors creak open toward the hallway.");
                    }
                    else _ui.Hint("Already solved. Try: go \"hallway\"");
                    return;
                }
                _ui.Hint("Wrong spot. Hint in the \"book\": B then 3.");
                return;
            }

            _ui.Hint(_livingSolved ? "Try: go \"hallway\"" : "Try: look | enter b3");
        }

        private void HandleHallway()
        {
            if (!_livingSolved)
            {
                _ui.Hint("The hallway latch won’t budge yet. Solve the living room first.");
                _room = "living";
                DescribeRoom();
                return;
            }

            if (_router.Verb == "look")
            {
                _ui.Type("HALLWAY: Portraits line the walls. A wall \"clock\" with loose hands ticks softly.");
                _ui.Type("Doors lead to the \"bedroom\" and the \"kitchen\".");
                _ui.Type("You can solve the clock here (ask Player 2 for the time on the map), or explore the rooms.");
                _ui.Type("Try: set clock 9:15  |  go \"bedroom\"  |  go \"kitchen\"");
                return;
            }

            // Clock puzzle is solved IN the hallway
            if (_router.Verb == "set")
            {
                var n = _router.Noun.Trim();
                if (n.StartsWith("clock "))
                {
                    var time = n.Substring("clock ".Length).Trim();
                    if (time == "9:15")
                    {
                        if (!_clockSolved)
                        {
                            _clockSolved = true;
                            if (!_player.HasCard(CardType.Raven)) _player.AddCard(CardType.Raven);
                            _ui.Type("Gears catch; a narrow recess opens in the hallway panel. You take the \"raven\" card.");
                        }
                        else _ui.Hint("The hallway clock is already set. Maybe check the bedroom or kitchen.");
                        return;
                    }
                    _ui.Hint("Ask Player 2 for the diorama time. (Skeleton expects 9:15)");
                    return;
                }
                _ui.Hint("Usage: set clock h:mm (e.g., set clock 9:15)");
                return;
            }

            _ui.Hint("Try: look | set clock 9:15 | go \"bedroom\" | go \"kitchen\" | back");
        }

        private void HandleBedroom()
        {
            if (_router.Verb == "look")
            {
                _ui.Type("BEDROOM: A marble \"bust\" on a pedestal. The \"head\" rotates. A plaque: \"Greet the first light.\"");
                _ui.Type("Skeleton: type 'rotate head east' → awards \"snake\" card.");
                return;
            }

            if (_router.Verb == "rotate")
            {
                // Expecting noun like: "head east"
                var n = _router.Noun.Trim();
                if (n == "head east")
                {
                    if (!_bedroomSolved)
                    {
                        _bedroomSolved = true;
                        if (!_player.HasCard(CardType.Snake)) _player.AddCard(CardType.Snake);
                        _ui.Type("Stone clicks; an alcove opens. You take the \"snake\" card. (back to hallway)");
                    }
                    else _ui.Hint("Already solved. Type 'back' to the hallway.");
                    return;
                }
                _ui.Hint("Usage: rotate head east");
                return;
            }

            _ui.Hint("Try: look | rotate head east | back");
        }

        private void HandleKitchen()
        {
            if (_router.Verb == "look")
            {
                _ui.Type("KITCHEN: Soot-blackened \"candles\" line the counter. A faint **morse** chart is scratched into the wall.");
                _ui.Type("This puzzle is a red herring; it won’t award a card. It points you toward the bedroom search.");
                _ui.Type("Try: light candles");
                return;
            }

            // False puzzle: lighting candles never gives a card. It delivers a hint (and one-time jumpscare).
            if (_router.Verb == "light")
            {
                var n = _router.Noun.Trim(); // e.g., "candles", "candles 1,3,4", etc.
                if (n.StartsWith("candles"))
                {
                    if (!_kitchenHintGiven)
                    {
                        if (!_kitchenJumpscareUsed)
                        {
                            _kitchenJumpscareUsed = true;
                            _ui.ShowJumpscare(JumpscareId.Whisper, 500);
                        }

                        _kitchenHintGiven = true;
                        _ui.Type("The flames gutter and reveal a greasy message along the backsplash:");
                        _ui.Hint("“SEARCH BENEATH THE BUST—LEFT FRONT.”");
                        _ui.Type("(Head back to the bedroom.)");
                    }
                    else
                    {
                        _ui.Hint("The message is already visible: “SEARCH BENEATH THE BUST—LEFT FRONT.”");
                    }
                    return;
                }
                _ui.Hint("Usage: light candles");
                return;
            }

            _ui.Hint("Try: look | light candles | back");
        }

        private void HandleGo()
        {
            var target = _router.Noun.Trim();

            switch (_room)
            {
                case "foyer":
                    if (target == "living") _room = "living";
                    else _ui.Toast("From the foyer, you can go to \"living\".");
                    break;

                case "living":
                    if (!_livingSolved)
                        _ui.Hint("The way forward is blocked. Solve the living room first.");
                    else if (target == "hallway") _room = "hallway";
                    else _ui.Toast("From here, try: go \"hallway\".");
                    break;

                case "hallway":
                    if (target == "bedroom") _room = "bedroom";
                    else if (target == "kitchen") _room = "kitchen";
                    else _ui.Toast("Choices here: set clock h:mm  |  go \"bedroom\"  |  go \"kitchen\"  |  back");
                    break;

                case "bedroom":
                case "kitchen":
                    if (target == "hallway") _room = "hallway";
                    else _ui.Toast("Try: back (to hallway).");
                    break;

                default:
                    _ui.Toast("You hesitate, unsure of the way.");
                    break;
            }

            DescribeRoom();
        }

        private void DescribeRoom()
        {
            _ui.Type(RoomTitle() + ":");
            switch (_room)
            {
                case "foyer":
                    _ui.Type("You’re a pizza guy who delivered to the wrong house.");
                    _ui.Type("Try: tutorial | go \"living\"");
                    break;

                case "living":
                    _ui.Type("A scuffed floor grid and a loose board.");
                    _ui.Type("Try: look | enter b3 | back");
                    break;

                case "hallway":
                    _ui.Type("Portraits and a ticking clock. Doors lead to the bedroom and the kitchen.");
                    _ui.Type("Try: look | set clock 9:15 | go \"bedroom\" | go \"kitchen\" | back");
                    break;

                case "bedroom":
                    _ui.Type("A marble bust with a rotating head.");
                    _ui.Type("Try: look | rotate head east | back");
                    break;

                case "kitchen":
                    _ui.Type("Soot-blackened candles. A faint morse chart scratched into the wall.");
                    _ui.Type("Try: look | light candles | back");
                    break;

                case "escape":
                    _ui.Type("Night air floods in—freedom! ");
                    break;

                default:
                    _ui.Type("Dust and darkness.");
                    break;
            }
        }

        private string RoomTitle() => _room switch
        {
            "foyer" => "Foyer",
            "living" => "Living Room",
            "hallway" => "Hallway",
            "bedroom" => "Bedroom",
            "kitchen" => "Kitchen",
            "escape" => "Main Door",
            _ => "Unknown"
        };

        private int CollectedCardsCount() => _player.CollectedCards.Count;
    }
}
