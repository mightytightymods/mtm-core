using System.Collections.Generic;
using HarmonyLib;

namespace MTM
{
    [HarmonyPatch(typeof(Character), nameof(Character.SetupMaxHealth))]
    public static class CharacterSetupMaxHealthPatch
    {
        static void Postfix(Character __instance)
        {
            if (__instance.IsPlayer())
                return; // never touch player health

            // Character.SetupMaxHealth is called by both Awake and SetLevel, so we can reliably know whether the 
            // creature is 0, 1 or 2-stars.                                                                                                                 
            
            // GetMaxHealthBase() applies Valheim's vanilla World difficulty multipliers, so changes made here are made
            // ON TOP of the builtin changes.
            var baseHealth = __instance.GetMaxHealthBase();
            var stars = __instance.GetLevel() - 1;
            var isBoss = __instance.m_boss;

            var mult = isBoss ? Plugin.BossHealthMult.Value : Plugin.CreatureHealthMult.Value;
            // Vanilla health scales at 100% per star
            var starBonus = 1f + (stars * 1.0f * Plugin.PerStarHealthMult.Value);
            var finalHealth = baseHealth * mult * starBonus;

            __instance.SetMaxHealth(finalHealth);

            if (Plugin.VerboseLogging.Value)
                Plugin.Log.LogInfo(
                    $"[HEALTH PATCHED] {__instance.name} | stars={stars} isBoss={isBoss} " +
                    $"baseHealth={baseHealth:F1} mult={mult:F2} starBonus={starBonus:F2} => final={finalHealth:F1}");
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    public static class CharacterDamagePatch
    {
        private struct DamageComponentLog
        {
            public string Name { get; init; }
            public float Before {get; init; }
            public float After { get; init; }
        }
        
        static void Prefix(Character __instance, ref HitData hit)
        {
            var attacker = hit.GetAttacker();
            if (attacker == null || attacker.IsPlayer())
                return;

            var stars = attacker.GetLevel() - 1;
            var isBoss = attacker.m_boss;

            var mult = isBoss ? Plugin.BossDamageMult.Value : Plugin.CreatureDamageMult.Value;
            // Vanilla damage scales at 50% per star, and it has already been applied in the HitData so it needs to be
            // taken into account.
            var vanillaStarMult = 1f + (stars * 0.5f);
            var desiredStarMult = 1f + (stars * 0.5f * Plugin.PerStarDamageMult.Value);
            var totalMult = mult * (desiredStarMult / vanillaStarMult);

            var before = hit.m_damage;
            hit.ApplyModifier(totalMult);
            var after = hit.m_damage;

            if (Plugin.VerboseLogging.Value)
            {
                // Log the individual damage components, particularly to highlight that chop and pickaxe are also
                // multiplied--they contribute to the value of HitData.GetTotalDamage(), but aren't usually
                // relevant.
                var components = new[]
                {
                    new DamageComponentLog { Name = "blunt", Before = before.m_blunt, After = after.m_blunt },
                    new DamageComponentLog { Name = "slash", Before = before.m_slash, After = after.m_slash },
                    new DamageComponentLog { Name = "pierce", Before = before.m_pierce, After = after.m_pierce },
                    new DamageComponentLog { Name = "chop", Before = before.m_chop, After = after.m_chop },
                    new DamageComponentLog { Name = "pickaxe", Before = before.m_pickaxe, After = after.m_pickaxe },
                    new DamageComponentLog { Name = "fire", Before = before.m_fire, After = after.m_fire },
                    new DamageComponentLog { Name = "frost", Before = before.m_frost, After = after.m_frost },
                    new DamageComponentLog { Name = "lightning", Before = before.m_lightning, After = after.m_lightning },
                    new DamageComponentLog { Name = "poison", Before = before.m_poison, After = after.m_poison },
                    new DamageComponentLog { Name = "spirit", Before = before.m_spirit, After = after.m_spirit },
                };
 
                var lines = new List<string>();
                foreach (var c in components)
                {
                    if (c.Before != 0f || c.After != 0f)
                        lines.Add($"  {c.Name}: {c.Before:F1} => {c.After:F1}");
                }
                var breakdown = string.Join("\n", lines);
 
                Plugin.Log.LogInfo(
                    $"[DMG PATCHED] attacker={attacker.name} stars={stars} isBoss={isBoss} " +
                    $"mult={mult:F2} starBonus={desiredStarMult:F2} totalMult={totalMult:F2}\n" +
                    $"{breakdown}");
            }
        }
    }
}