using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Command;
using ACE.Server.Factories;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ValHeelMirraCommands
{
    [HarmonyPatch]
    public class PatchClass
    {
        #region Settings
        const int RETRIES = 10;

        public static Settings Settings = new();
        static string settingsPath => Path.Combine(Mod.ModPath, "Settings.json");
        private FileInfo settingsInfo = new(settingsPath);

        private JsonSerializerOptions _serializeOptions = new()
        {
            WriteIndented = true,
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private void SaveSettings()
        {
            string jsonString = JsonSerializer.Serialize(Settings, _serializeOptions);

            if (!settingsInfo.RetryWrite(jsonString, RETRIES))
            {
                ModManager.Log($"Failed to save settings to {settingsPath}...", ModManager.LogLevel.Warn);
                Mod.State = ModState.Error;
            }
        }

        private void LoadSettings()
        {
            if (!settingsInfo.Exists)
            {
                ModManager.Log($"Creating {settingsInfo}...");
                SaveSettings();
            }
            else
                ModManager.Log($"Loading settings from {settingsPath}...");

            if (!settingsInfo.RetryRead(out string jsonString, RETRIES))
            {
                Mod.State = ModState.Error;
                return;
            }

            try
            {
                Settings = JsonSerializer.Deserialize<Settings>(jsonString, _serializeOptions);
            }
            catch (Exception)
            {
                ModManager.Log($"Failed to deserialize Settings: {settingsPath}", ModManager.LogLevel.Warn);
                Mod.State = ModState.Error;
                return;
            }
        }
        #endregion

        #region Start/Shutdown
        public void Start()
        {
            //Need to decide on async use
            Mod.State = ModState.Loading;
            LoadSettings();

            if (Mod.State == ModState.Error)
            {
                ModManager.DisableModByPath(Mod.ModPath);
                return;
            }

            Mod.State = ModState.Running;
        }

        public void Shutdown()
        {
            //if (Mod.State == ModState.Running)
            // Shut down enabled mod...

            //If the mod is making changes that need to be saved use this and only manually edit settings when the patch is not active.
            SaveSettings();

            if (Mod.State == ModState.Error)
                ModManager.Log($"Improper shutdown: {Mod.ModPath}", ModManager.LogLevel.Error);
        }
        #endregion

        #region Patches
        
        [CommandHandler("genmirra", AccessLevel.Admin, CommandHandlerFlag.RequiresWorld, 0)]

        //This is the command handler for the /genmirra command. It stores all the Mirra types and their respective IDs in a dictionary.
        //It then checks if the player has entered the correct parameters and sends a message to the player if they haven't.
        public static void HandleGenMirra(Session session, string[] parameters)
        {
            if (parameters.Length != 2)
            {
                session.Player.SendMessage("Usage: /genmirra <Mirra Type> <Purity>");
                return;
            }
            
            //This is our dictionary that stores all the Mirra types and their respective IDs.
            Dictionary<string, uint> mirra = new()
            {
                { "st", 801966 }, //Steel Mirra
                { "ir", 801967 }, //Iron Mirra
                { "ws", 801968 }, //White Saphire Mirra
                { "to", 801969 }, //Topaz Mirra
                { "bg", 801970 }, //Balck Garnet Mirra
                { "am", 801971 }, //Aquamarine Mirra
                { "rg", 801972 }, //Red Garnet Mirra
                { "em", 801973 }, //Emerald Mirra
                { "je", 801974 }, //Jet Mirra
                { "gg", 801975 }, //Green Garnet Mirra
                { "ma", 801976 }, //Mahogany Mirra
                { "al", 801977 }, //Alabaster Mirra
            };

            //This dictionary stores the names of the Mirra types.
            Dictionary <string, string> mirraNames = new()
            {
                { "st", "Steel" },
                { "ir", "Iron" },
                { "ws", "White Saphire" },
                { "to", "Topaz" },
                { "bg", "Balck Garnet" },
                { "am", "Aquamarine" },
                { "rg", "Red Garnet" },
                { "em", "Emerald" },
                { "je", "Jet" },
                { "gg", "Green Garnet" },
                { "ma", "Mahogany" },
                { "al", "Alabaster" },
            };

            //This checks if the player has entered a valid Purity. If they haven't, it will send a message to the player.
            if (int.Parse(parameters[1]) < 1 || int.Parse(parameters[1]) > 5 || !int.TryParse(parameters[1], out int pur))
            {
                session.Player.SendMessage("Invalid Purity");
                return;
            }

            var purity = int.Parse(parameters[1]);

            //If the player has entered an invalid Mirra type, if not, it will send a message to the player.
            //If the player has entered "all" as the Mirra type, it will generate all the Mirra types.
            if (!mirra.ContainsKey(parameters[0]))
            {
                if (parameters[0] == "all")
                {
                    foreach (var m in mirra)
                    {
                        _ = int.TryParse(parameters[1], out int p);
                        var wo = GenerateMirra(m.Value, p);
                        session.Player.TryCreateInInventoryWithNetworking(wo);
                        session.Player.SendMessage($"Generated {mirraNames[m.Key]} Mirra with a purity of {p}");
                    }
                }
                else 
                {
                    session.Player.SendMessage("Invalid Mirra Type");
                    return;
                }
            }
            else 
            { 
                foreach (var m in mirra)
                {
                    if (parameters[0] == m.Key)
                    {
                        var wo = GenerateMirra(m.Value, purity);
                        session.Player.TryCreateInInventoryWithNetworking(wo);
                        session.Player.SendMessage($"Generated {mirraNames[m.Key]} Mirra with a purity of {purity}");
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Generate a Mirra with the given wcid and purity
        /// </summary>
        /// <param name="wcid"></param>
        /// <param name="purity"></param>
        /// <returns></returns>
        public static WorldObject GenerateMirra(uint wcid, int purity)
        {
            //Create the worldobject
            var wo = WorldObjectFactory.CreateNewWorldObject(wcid);

            //Set the purity level
            wo.Level = purity;

            //Send it off to the loot generation factory to mutate the Mirra
            LootGenerationFactory.MutatePolishedMirra(wo);

            //Return the worldobject
            return wo;
        }
        #endregion
    }
}