using System.Collections.Generic;

namespace EnableCheats
{
    public static class ChatWordLibrary
    {
        public static Dictionary<string, ChatWord> chatWords = new Dictionary<string, ChatWord>()
        {
            { "give", new ChatWord() },
            { "set", new ChatWord() 
                {
                    chatWords = new Dictionary<string, ChatWord>()
                    {
                        { "hunger", new ChatWord() },
                        { "thirst", new ChatWord() },
                        { "blockhealth", new ChatWord() },
                        { "frequency", new ChatWord() },
                        { "fireworklife", new ChatWord() },
                        { "anchor", new ChatWord() },
                        { "bonushunger", new ChatWord() },
                        { "bonushealth", new ChatWord() },
                        { "health", new ChatWord() },
                        { "time", new ChatWord() },
                        { "oxygen", new ChatWord() },
                        { "timescale", new ChatWord() },
                        { "durability", new ChatWord() },
                        { "gametime", new ChatWord() },
                        { "gamemode", new ChatWord() },
                        { "bonusthirst", new ChatWord() },
                        { "fps", new ChatWord() }                    }
                }
            },
            { "shift", new ChatWord() },
            { "reset", new ChatWord()
                {
                    chatWords = new Dictionary<string, ChatWord>()
                    {
                        { "achievements", new ChatWord() },
                        { "interactables", new ChatWord() },
                        { "characters", new ChatWord() },
                        { "buff", new ChatWord() },
                        { "timescale", new ChatWord() }
                    }
                }
            },
            { "landmark", new ChatWord()
                {
                    chatWords = new Dictionary<string, ChatWord>()
                    {
                        { "clear", new ChatWord()
                            { 
                                chatWords = new Dictionary<string, ChatWord>()
                                {
                                    { "all", new ChatWord() },
                                    { "Landmark_Small", new ChatWord() },
                                    { "Landmark_Big", new ChatWord() },
                                    { "Landmark_Pilot", new ChatWord() },
                                    { "Landmark_RadioTower", new ChatWord() },
                                    { "Landmark_FloatingRaft", new ChatWord() },
                                    { "Landmark_Boat", new ChatWord() },
                                    { "Landmark_Test", new ChatWord() },
                                    { "Landmark_Vasagatan", new ChatWord() },
                                    { "Landmark_Balboa", new ChatWord() },
                                    { "Landmark_CaravanIsland", new ChatWord() },
                                    { "Landmark_Tangaroa", new ChatWord() },
                                    { "Landmark_VarunaPoint", new ChatWord() },
                                    { "Landmark_Temperance", new ChatWord() },
                                    { "Landmark_Utopia", new ChatWord() }
                                }
                            } 
                        },
                        { "Landmark_Small", new ChatWord() },
                        { "Landmark_Big", new ChatWord() },
                        { "Landmark_Pilot", new ChatWord() },
                        { "Landmark_RadioTower", new ChatWord() },
                        { "Landmark_FloatingRaft", new ChatWord() },
                        { "Landmark_Boat", new ChatWord() },
                        { "Landmark_Test", new ChatWord() },
                        { "Landmark_Vasagatan", new ChatWord() },
                        { "Landmark_Balboa", new ChatWord() },
                        { "Landmark_CaravanIsland", new ChatWord() },
                        { "Landmark_Tangaroa", new ChatWord() },
                        { "Landmark_VarunaPoint", new ChatWord() },
                        { "Landmark_Temperance", new ChatWord() },
                        { "Landmark_Utopia", new ChatWord() }
                    }
                }
            },
            { "spawn", new ChatWord()
                {
                    chatWords = new Dictionary<string, ChatWord>()
                    {
                        { "StoneBird", new ChatWord() },
                        { "PufferFish", new ChatWord() },
                        { "Llama", new ChatWord() },
                        { "Goat", new ChatWord() },
                        { "Chicken", new ChatWord() },
                        { "Boar", new ChatWord() },
                        { "Rat", new ChatWord() },
                        { "Shark", new ChatWord() },
                        { "TEST", new ChatWord() },
                        { "Bear", new ChatWord() },
                        { "MamaBear", new ChatWord() },
                        { "BugSwarm_Bee", new ChatWord() },
                        { "Pig", new ChatWord() },
                        { "StoneBird_Caravan", new ChatWord() },
                        { "ButlerBot", new ChatWord() },
                        { "Rat_Tangaroa", new ChatWord() },
                        { "Boss_Varuna", new ChatWord() },
                        { "AnglerFish", new ChatWord() },
                        { "PolarBear", new ChatWord() },
                        { "Roach", new ChatWord() },
                        { "Puffin", new ChatWord() },
                        { "Dolphin", new ChatWord() },
                        { "Whale", new ChatWord() },
                        { "BirdPack", new ChatWord() },
                        { "Turtle", new ChatWord() },
                        { "Stingray", new ChatWord() },
                        { "Hyena", new ChatWord() },
                        { "HyenaBoss", new ChatWord() },
                        { "NPC_Annisa", new ChatWord() },
                        { "NPC_Citra", new ChatWord() },
                        { "NPC_Ika", new ChatWord() },
                        { "NPC_Isac", new ChatWord() },
                        { "NPC_Johan", new ChatWord() },
                        { "NPC_Kartika", new ChatWord() },
                        { "NPC_Larry", new ChatWord() },
                        { "NPC_Max", new ChatWord() },
                        { "NPC_Noah", new ChatWord() },
                        { "NPC_Oliver", new ChatWord() },
                        { "NPC_Timur", new ChatWord() },
                        { "NPC_Toshiro", new ChatWord() },
                        { "NPC_Ulla", new ChatWord() },
                        { "NPC_Zayana", new ChatWord() },
                        { "NPC_Vanessa", new ChatWord() }
                    }
                }
            },
            { "tp", new ChatWord()
                {
                    chatWords = new Dictionary<string, ChatWord>()
                    {
                        { "raft", new ChatWord() },
                        { "treasure", new ChatWord() },
                        { "MamaBear_Lure", new ChatWord() },
                        { "MamaBear_Cave", new ChatWord() },
                        { "Varuna_Boss_Floor_1", new ChatWord() },
                        { "Varuna_Boss_Floor_2", new ChatWord() },
                        { "Varuna_Boss_Floor_3", new ChatWord() },
                        { "Varuna_Boss_WaypointHandler_1", new ChatWord() },
                        { "Varuna_Boss_WaypointHandler_2", new ChatWord() },
                        { "Varuna_Boss_WaypointHandler_3", new ChatWord() },
                        { "Temperance_FightChallenge_RallyPoint", new ChatWord() },
                        { "Utopia_Cogwheel_LiftJumpPuzzle", new ChatWord() },
                        { "Utopia_Cogwheel_SmallJusticeScale", new ChatWord() },
                        { "Utopia_Cogwheel_BigJusticeScale", new ChatWord() },
                        { "Utopia_Cogwheel_BrokenElevator", new ChatWord() },
                        { "Utopia_Cogwheel_DrawBridge", new ChatWord() },
                        { "Utopia_Cogwheel_Stairs", new ChatWord() },
                        { "Utopia_Waypoint_1", new ChatWord() },
                        { "Utopia_Waypoint_2", new ChatWord() },
                        { "Utopia_Waypoint_3", new ChatWord() },
                        { "Utopia_JusticeScaleWeight", new ChatWord() },
                        { "Utopia_BossOlofPart2", new ChatWord() },
                        { "Utopia_HyenaBossAreaZone", new ChatWord() },
                        { "Varuna_Boss_AreaZone_AllowedTarget", new ChatWord() },
                        { "Varuna_Boss_HelperScript", new ChatWord() }
                    }
                } 
            },
            { "show", new ChatWord()
                {
                    chatWords = new Dictionary<string, ChatWord>()
                    {
                        { "treasure", new ChatWord() }
                    }
                }
            },
            { "animals", new ChatWord()
                {
                    chatWords = new Dictionary<string, ChatWord>()
                    {
                        { "starve", new ChatWord() },
                        { "feed", new ChatWord() },
                        { "changematerial", new ChatWord() }
                    }
                }
            },
            { "questitem", new ChatWord()
                {
                    chatWords = new Dictionary<string, ChatWord>()
                    {
                        { "clear", new ChatWord() },
                        { "check", new ChatWord() },
                        { "Vasagatan_Crowbar", new ChatWord() },
                        { "Vasagatan_GreenKey", new ChatWord() },
                        { "Vasagatan_BlueKey", new ChatWord() },
                        { "Vasagatan_RedKey", new ChatWord() },
                        { "Vasagatan_BoltCutter", new ChatWord() },
                        { "Vasagatan_GasTank", new ChatWord() },
                        { "Vasagatan_Lighter", new ChatWord() },
                        { "Vasagatan_Bullet", new ChatWord() },
                        { "Vasagatan_MechanicalPart", new ChatWord() },
                        { "Vasagatan_BombWire", new ChatWord() },
                        { "Vasagatan_FourDigitCode", new ChatWord() },
                        { "Vasagatan_KeyCard", new ChatWord() },
                        { "Vasagatan_Bomb", new ChatWord() },
                        { "Vasagatan_Recorder", new ChatWord() },
                        { "Vasagatan_CarJack", new ChatWord() },
                        { "Balboa_WildBerries", new ChatWord() },
                        { "Balboa_BrunosTool_Saw", new ChatWord() },
                        { "Balboa_BrunosTool_Hammer", new ChatWord() },
                        { "Balboa_BrunosTool_Wrench", new ChatWord() },
                        { "Balboa_LightBulb", new ChatWord() },
                        { "Caravan_ZiplinePart", new ChatWord() },
                        { "Caravan_BatteryChargerPart", new ChatWord() },
                        { "Caravan_KeyInfirmary", new ChatWord() },
                        { "Caravan_KeyMayor = 24", new ChatWord() },
                        { "Tangaroa_Tape", new ChatWord() },
                        { "Tangaroa_KeyCard", new ChatWord() },
                        { "Tangaroa_GeneratorPart", new ChatWord() },
                        { "Tangaroa_Token", new ChatWord() },
                        { "Varuna_MotherlodeKey", new ChatWord() },
                        { "Varuna_CraneKey", new ChatWord() },
                        { "Varuna_SpotlightPart", new ChatWord() },
                        { "Temperance_Blowtorch", new ChatWord() },
                        { "Temperance_ElectricalCable", new ChatWord() },
                        { "Temperance_ControlRod", new ChatWord() },
                        { "Temperance_ReactorKey", new ChatWord() },
                        { "Temperance_SeleneGateKey", new ChatWord() },
                        { "Utopia_KeyEntrance", new ChatWord() },
                        { "Utopia_KeyPhase1", new ChatWord() },
                        { "Utopia_KeyPrison", new ChatWord() },
                        { "Utopia_Hammer", new ChatWord() },
                        { "Utopia_DettoCode", new ChatWord() },
                        { "Utopia_Cable", new ChatWord() },
                        { "Utopia_CarbonDioxideCanister", new ChatWord() },
                        { "Utopia_Harpoon", new ChatWord() }
                    }
                }
            },
            { "quest", new ChatWord()
                {
                    chatWords = new Dictionary<string, ChatWord>()
                    {
                        { "treasure", new ChatWord() }
                    }
                }
            },
            { "raft", new ChatWord()
                {
                    chatWords = new Dictionary<string, ChatWord>()
                    {
                        { "unstuck", new ChatWord() },
                        { "target", new ChatWord() }
                    }
                }
            },
            { "weather", new ChatWord() },
            { "unlock", new ChatWord()
                {
                    chatWords = new Dictionary<string, ChatWord>()
                    {
                        { "character", new ChatWord() },
                        { "all", new ChatWord() }
                    }
                }
            },
            { "notebook", new ChatWord()
                {
                    chatWords = new Dictionary<string, ChatWord>()
                    {
                        { "all", new ChatWord() }
                    }
                }
            },
            { "nuke", new ChatWord() },
            { "build", new ChatWord()
                {
                    chatWords = new Dictionary<string, ChatWord>()
                    {
                        { "foundation", new ChatWord() }
                    }
                }
            },
            { "camera", new ChatWord()
                {
                    chatWords = new Dictionary<string, ChatWord>()
                    {
                        { "acceleration", new ChatWord() },
                        { "deacceleration", new ChatWord() },
                        { "maxspeed", new ChatWord() },
                        { "mouselerp", new ChatWord() },
                        { "target", new ChatWord() },
                        { "targetoffset", new ChatWord() }
                    }
                }
            },
            { "weight", new ChatWord() },
            { "reciever", new ChatWord()
                {
                    chatWords = new Dictionary<string, ChatWord>()
                    {
                        { "force", new ChatWord() }
                    }
                }
            },
            { "reflection", new ChatWord()
                {
                    chatWords = new Dictionary<string, ChatWord>()
                    {
                        { "on", new ChatWord() },
                        { "off", new ChatWord() },
                        { "follow", new ChatWord() },
                        { "sphere", new ChatWord() }
                    }
                }
            },
            { "kit", new ChatWord()
                {
                    chatWords = new Dictionary<string, ChatWord>()
                    {
                        { "treasure", new ChatWord() },
                        { "painting_redbeet", new ChatWord() },
                        { "painting_tangaroa", new ChatWord() },
                        { "paintbrush", new ChatWord() },
                        { "resources", new ChatWord() },
                        { "seedflower", new ChatWord() },
                        { "flower", new ChatWord() },
                        { "seed", new ChatWord() },
                        { "armor", new ChatWord() },
                        { "tiki", new ChatWord() },
                        { "radio", new ChatWord() },
                        { "cassette", new ChatWord() },
                        { "hat", new ChatWord() },
                        { "head", new ChatWord() },
                        { "utopia", new ChatWord() },
                        { "temperance", new ChatWord() },
                        { "recipebowl", new ChatWord() },
                        { "recipepaper", new ChatWord() },
                        { "foodraw", new ChatWord() },
                        { "foodcooked", new ChatWord() },
                        { "buff", new ChatWord() },
                        { "battery", new ChatWord() },
                        { "playtest", new ChatWord() },
                        { "fishing", new ChatWord() },
                        { "trophyfish", new ChatWord() },
                        { "blueprint", new ChatWord() }
                    }
                }
            },
            { "environmentlight", new ChatWord() },
            { "simulate", new ChatWord()
                {
                    chatWords = new Dictionary<string, ChatWord>()
                    {
                        { "pickup", new ChatWord() },
                        { "motorweight", new ChatWord() },
                        { "interact", new ChatWord()
                            {
                                chatWords = new Dictionary<string, ChatWord>()
                                {
                                    { "force", new ChatWord() }
                                }
                            }
                        }
                    }
                }
            },
            { "cook", new ChatWord() },
            { "mark", new ChatWord() },
            { "recall", new ChatWord() },
            { "worldeventstop", new ChatWord()
                {
                    chatWords = new Dictionary<string, ChatWord>()
                    {
                        { "Dolphins", new ChatWord() },
                        { "Whale", new ChatWord() },
                        { "Birdswarm", new ChatWord() },
                        { "Turtle", new ChatWord() },
                        { "Stingray", new ChatWord() }
                    }
                }
            },
            { "worldevent", new ChatWord()
                {
                    chatWords = new Dictionary<string, ChatWord>()
                    {
                        { "Dolphins", new ChatWord() },
                        { "Whale", new ChatWord() },
                        { "Birdswarm", new ChatWord() },
                        { "Turtle", new ChatWord() },
                        { "Stingray", new ChatWord() }
                    }
                }
            },
            { "print", new ChatWord()
                {
                    chatWords = new Dictionary<string, ChatWord>()
                    {
                        { "worldevent", new ChatWord() },
                        { "gametime", new ChatWord() }
                    }
                }
            },
            { "hazmat", new ChatWord()
                {
                    chatWords = new Dictionary<string, ChatWord>()
                    {
                        { "true", new ChatWord() },
                        { "false", new ChatWord() }
                    }
                }
            },
            { "oldwater", new ChatWord() },
            { "bakeblocks", new ChatWord()
                {
                    chatWords = new Dictionary<string, ChatWord>()
                    {
                        { "true", new ChatWord() },
                        { "false", new ChatWord() }
                    }
                }
            },
            { "honk", new ChatWord() },
            { "DestroyBlock", new ChatWord() }
        };
    }
    public class ChatWord
    {
        public Dictionary<string, ChatWord> chatWords = new Dictionary<string, ChatWord>();

    }
}