using System;
using System.Threading;

namespace DW_T9.Game
{
    internal sealed class Game
    {
        // Core services (provided via GameContext for easy testing/injection)
        private readonly UI _ui;
        private readonly Timer _timer;
        private readonly Player _player;

        // Simple parser (Verb + Noun)
        private readonly CommandRouter _router = new CommandRouter();

        // World state
        private string _room = "foyer";     // foyer → living → hallway → (bedroom | clock) → escape
        private bool _livingSolved = false; // gate to hallway
        private bool _bedroomSolved = false;
        private bool _clockSolved = false;

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
                    _ui.Type("Talk to each other. Path is linear until the hallway, then choose Bedroom or Clock.");
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
                    _ui.Type("Commands: look | inspect <x> | pickup <x> | inventory | cards | use <x> | enter <ans> | go <room> | back | help | quit");
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
                    else if (_room == "bedroom" || _room == "clock") _room = "hallway";
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
                    case "hallway": HandleHallway(); break;
                    case "bedroom": HandleBedroom(); break;
                    case "clock": HandleClock(); break;
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
                _ui.Type("Ahead, a central \"living\" room.");
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
                _ui.Type("HALLWAY: Portraits line the walls. You can choose left or right.");
                _ui.Type("Choices: go \"bedroom\" (bust → snake)  |  go \"clock\" (time → raven)");
                return;
            }

            _ui.Hint("Try: look | go \"bedroom\" | go \"clock\"");
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

        private void HandleClock()
        {
            if (_router.Verb == "look")
            {
                _ui.Type("CLOCK ROOM: A wall \"clock\" with loose hands. Note: \"Match the map.\"");
                _ui.Type("Skeleton: type 'set clock 9:15' → awards \"raven\" card.");
                return;
            }

            if (_router.Verb == "set")
            {
                // Expecting noun like: "clock 9:15"
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
                            _ui.Type("Gears catch; a recess opens. You take the \"raven\" card. (back to hallway)");
                        }
                        else _ui.Hint("Already solved. Type 'back' to the hallway.");
                        return;
                    }
                    _ui.Hint("Ask Player 2 for the diorama time. (Skeleton expects 9:15)");
                    return;
                }
                _ui.Hint("Usage: set clock h:mm (e.g., set clock 9:15)");
                return;
            }

            _ui.Hint("Try: look | set clock 9:15 | back");
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
                    else if (target == "clock") _room = "clock";
                    else _ui.Toast("Choices here: go \"bedroom\" or go \"clock\".");
                    break;

                case "bedroom":
                case "clock":
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
                    _ui.Type("Portraits line the walls.");
                    _ui.Type("Choices: go \"bedroom\" | go \"clock\" | back");
                    break;

                case "bedroom":
                    _ui.Type("A marble bust with a rotating head.");
                    _ui.Type("Try: look | rotate head east | back");
                    break;

                case "clock":
                    _ui.Type("A wall clock with loose hands.");
                    _ui.Type("Try: look | set clock 9:15 | back");
                    break;

                case "escape":
                    _ui.Type("Night air floods in—freedom!");
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
            "clock" => "Clock Room",
            "escape" => "Main Door",
            _ => "Unknown"
        };

        private int CollectedCardsCount() => _player.CollectedCards.Count;
    }
}
