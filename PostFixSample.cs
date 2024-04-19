using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.WorldObjects;

namespace ValHeelMirraCommands;

[HarmonyPatchCategory(nameof(PostFixSample))]
internal class PostFixSample
{
    #region Settings
    public static Settings Settings = new();
    static string settingsPath => Path.Combine(Mod.ModPath, "Settings.json");
    private FileInfo settingsInfo = new(settingsPath);
    #endregion

    #region Patch

    /// <summary>
    /// This is a smaple of a postfix patch that modifies the damage dealt by the player and the damage dealt to the player if the player is in hardcore mode.
    /// player.GetProperty((PropertyBool)31000) is a custom property declared in PatchClass.cs that is set to true when the player is in hardcore mode.
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="defender"></param>
    /// <param name="damageSource"></param>
    /// <param name="__instance"></param>
    /// <param name="__result"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(DamageEvent), "DoCalculateDamage", new Type[] { typeof(Creature), typeof(Creature), typeof(WorldObject) })]
    public static void PostDoCalculateDamage(Creature attacker, Creature defender, WorldObject damageSource, ref DamageEvent __instance, ref float __result)
    {
        if (attacker is Player player && defender is Creature creature)
        {
            if (player.GetProperty((PropertyBool)31000) == null)
            {
                player.SetProperty((PropertyBool)31000, false);
                return;
            }
            if (player.GetProperty((PropertyBool)31000) != true)
                return;

            //player.SendMessage("You are in hardcore mode!", ACE.Entity.Enum.ChatMessageType.Broadcast);
            var moddedDamage = __result + (__result * Settings.HardcoreDamageBonus); 

            __result += 1 * moddedDamage;
            return;
        }
        if (attacker is Creature creature1 && defender is Player player1)
        {
            if (player1.GetProperty((PropertyBool)31000) == null)
            {
                player1.SetProperty((PropertyBool)31000, false);
                return;
            }
            if (player1.GetProperty((PropertyBool)31000) != true)
                return;

            //player1.SendMessage("You are in hardcore mode!", ACE.Entity.Enum.ChatMessageType.Broadcast);
            var moddedDamage = __result + (__result * Settings.HcMobDamageBoost);

            __result += 1 * moddedDamage;
            return;
        }
    }

    #endregion
}

