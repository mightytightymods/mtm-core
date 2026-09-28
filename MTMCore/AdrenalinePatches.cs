using HarmonyLib;

namespace MTM
{
    // Ported from a third-party AdrenalineModifier mod. Verify against current
    // Player.AddAdrenaline / m_adrenalineDegenTimer before relying on this in a live build.
    [HarmonyPatch(typeof(Player), nameof(Player.AddAdrenaline))]
    internal static class AdrenalinePatches
    {
        private static void Prefix(Player __instance, ref float v)
        {
            if (v > 0f)
            {
                v *= Plugin.AdrenalineMultiplier.Value;
            }
            else
            {
                v *= Plugin.AdrenalineDecayMultiplier.Value;
            }

            if (Plugin.VerboseLogging.Value)
            {
                Plugin.Log.LogInfo($"[Adrenaline] Prefix adjusted v to {v}");
            }
        }

        private static void Postfix(Player __instance, ref float ___m_adrenalineDegenTimer, ref float v)
        {
            float maxAdrenaline = __instance.GetMaxAdrenaline();
            if (v > 0f && maxAdrenaline > 0f)
            {
                ___m_adrenalineDegenTimer *= Plugin.AdrenalineDecayDelayMultiplier.Value;

                if (Plugin.VerboseLogging.Value)
                {
                    Plugin.Log.LogInfo($"[Adrenaline] Postfix degen timer scaled to {___m_adrenalineDegenTimer}");
                }
            }
        }
    }
}