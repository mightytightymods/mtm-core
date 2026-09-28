using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Utils;

namespace MTM
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "mightytightymods.mtm-core";
        public const string PluginName = "MTMCore";
        public const string PluginVersion = "1.0.0";
        
        // Difficulty
        public static ConfigEntry<float> CreatureHealthMult;
        public static ConfigEntry<float> CreatureDamageMult;
        public static ConfigEntry<float> BossHealthMult;
        public static ConfigEntry<float> BossDamageMult;
        public static ConfigEntry<float> PerStarHealthMult;
        public static ConfigEntry<float> PerStarDamageMult;

        // Adrenaline
        public static ConfigEntry<float> AdrenalineMultiplier;
        public static ConfigEntry<float> AdrenalineDecayMultiplier;
        public static ConfigEntry<float> AdrenalineDecayDelayMultiplier;
        
        public static ConfigEntry<bool> VerboseLogging;
        
        // Split Buttons
        public static ConfigEntry<int> UpperLeftSplitAmount;
        public static ConfigEntry<int> UpperRightSplitAmount;
        public static ConfigEntry<int> LowerLeftSplitAmount;
        public static ConfigEntry<int> LowerRightSplitAmount;
        
        // Skill Book Limits
        public static ConfigEntry<int> SkillBookBreakpoint;
        public static ConfigEntry<int> SkillBookCap;
        
        readonly Harmony harmony = new Harmony(PluginGUID);
        
        public static ManualLogSource Log;
        
        private void Awake()
        {
            Log = base.Logger;
            
            // When running a server, IsAdminOnly enforces server-authoritative configuration.
            var adminOnly = new ConfigurationManagerAttributes { IsAdminOnly = true };

            // Difficulty
            var difficultyRange = new AcceptableValueRange<float>(0.1f, 5.0f);
            
            CreatureHealthMult = Config.Bind("Creatures", "HealthMultiplier", 1.0f,
                new ConfigDescription("Multiplier applied to all non-boss creature hitpoints. 1.0 = vanilla.", difficultyRange, adminOnly));
            CreatureDamageMult = Config.Bind("Creatures", "DamageMultiplier", 1.0f,
                new ConfigDescription("Multiplier applied to all non-boss creature damage output. 1.0 = vanilla.", difficultyRange, adminOnly));
            PerStarHealthMult = Config.Bind("Creatures", "PerStarHealthMultiplier", 1.0f,
                new ConfigDescription("Multiplier to creature health added per star level. 1.0 = vanilla star health scaling -> +100% health per star." +
                    "NOTE: This is multiplicative with HealthMultiplier.", difficultyRange, adminOnly));
            PerStarDamageMult = Config.Bind("Creatures", "PerStarDamageMultiplier", 1.0f,
                new ConfigDescription("Multiplier to creature damage added per star level. 1.0 = vanilla star damage scaling -> +50% damage per star." +
                    "NOTE: This is multiplicative with DamageMultiplier.", difficultyRange, adminOnly));
            BossHealthMult = Config.Bind("Bosses", "BossHealthMultiplier", 1.0f,
                new ConfigDescription("Multiplier applied to boss health. 1.0 = vanilla.", difficultyRange, adminOnly));
            BossDamageMult = Config.Bind("Bosses", "BossDamageMultiplier", 1.0f,
                new ConfigDescription("Multiplier applied to all boss damage output. 1.0 = vanilla.", difficultyRange, adminOnly));

            // Adrenaline
            AdrenalineMultiplier = Config.Bind("Adrenaline", "AdrenalineMultiplier", 1.0f,
                new ConfigDescription("Multiplier applied to adrenaline gained. Lower = harder (e.g. 0.5), higher = easier (e.g. 1.5). 1.0 = vanilla.", difficultyRange, adminOnly));
            AdrenalineDecayMultiplier = Config.Bind("Adrenaline", "AdrenalineDecayMultiplier", 1.0f,
                new ConfigDescription("Multiplier applied to adrenaline lost. Higher = harder (e.g. 1.5), lower = easier (e.g. 0.5). 1.0 = vanilla.", difficultyRange, adminOnly));
            AdrenalineDecayDelayMultiplier = Config.Bind("Adrenaline", "AdrenalineDecayDelayMultiplier", 1.0f,
                new ConfigDescription("Multiplier applied to the delay before adrenaline starts decaying. Lower = harder (e.g. 0.5), higher = easier (e.g. 1.5). 1.0 = vanilla.", difficultyRange, adminOnly));

            // client-side
            #if DEBUG
            VerboseLogging = Config.Bind("Diagnostics", "VerboseLogging", true,
                new ConfigDescription("Logs detailed per-hit and per-creature scaling information."));
            #else
            VerboseLogging = Config.Bind("Diagnostics", "VerboseLogging", false,
                new ConfigDescription("Logs detailed per-hit and per-creature scaling information."));
            #endif
            
            // Split Buttons -- all client-side
            var splitAmountRange = new AcceptableValueRange<int>(1, 50);
            UpperLeftSplitAmount = Config.Bind("Split Buttons", "UpperLeftAmount", 1,
                new ConfigDescription("Split Amount", splitAmountRange));
            UpperRightSplitAmount = Config.Bind("Split Buttons", "UpperRightAmount", 2,
                new ConfigDescription("Split Amount", splitAmountRange));
            LowerLeftSplitAmount = Config.Bind("Split Buttons", "LowerLeftAmount", 10,
                new ConfigDescription("Split Amount", splitAmountRange));
            LowerRightSplitAmount = Config.Bind("Split Buttons", "LowerRightAmount", 20,
                new ConfigDescription("Split Amount", splitAmountRange));
            
            // Skill Book Limits
            // Custom tweaks to trainers in the More_World_Location_AIO mod.
            var skillRange = new AcceptableValueRange<int>(1, 100);
            SkillBookBreakpoint = Config.Bind("Skill Book Limits", "SkillBookBreakpoint", 100,
                new ConfigDescription("Skill level past which costs double", skillRange, adminOnly));
            SkillBookCap  = Config.Bind("Skill Book Limits", "SkillBookCap", 100,
                new ConfigDescription("Max skill level trained with a skill book", skillRange, adminOnly));
            
            harmony.PatchAll();
            
            Logger.LogInfo("MTM Difficulty initialized.");
        }
    }
}