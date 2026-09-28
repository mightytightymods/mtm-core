using HarmonyLib;
using More_World_Locations_AIO.Traders;
using UnityEngine;

namespace MTM
{
    /// <summary>
    /// Prevents a skill book from being consumed at all once the target
    /// skill is already at the cap, by hooking the same gate the game
    /// itself uses (Player.CanConsumeItem) rather than letting the item
    /// get eaten and then doing nothing.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.CanConsumeItem))]
    internal static class SkillBookConsumeGatePatch
    {
        private static void Postfix(Player __instance, ItemDrop.ItemData item, ref bool __result)
        {
            if (!__result) return; // already blocked for some other vanilla reason

            if (item?.m_shared?.m_consumeStatusEffect is not SkillBook_SE bookEffect)
                return; // not one of our skill books

            Skills.Skill skill = __instance.GetSkills().GetSkill(bookEffect.skillType);
            if (skill.m_level >= Plugin.SkillBookCap.Value)
            {
                __instance.Message(MessageHud.MessageType.Center,
                    "$skill_" + bookEffect.skillType.ToString().ToLower() + " is already capped at " + Plugin.SkillBookCap.Value);
                __result = false;
            }
        }
    }

    /// <summary>
    /// Patches MWL's SkillBook_SE.ApplySkillBook() to:
    ///   a) hard-cap skill training from books to a configured value (belt-and-braces backstop;
    ///      SkillBookConsumeGatePatch above should prevent reaching this at all
    ///      once capped, since the item won't be consumable)
    ///   b) make training in the breakpoint range cost 2x as many "book points"
    ///   c) preserve fractional skill progress correctly across level-ups
    ///
    /// Requires a Jotunn/BepInEx publicized reference to
    /// More_World_Locations_AIO.dll and assembly_valheim.dll
    /// </summary>
    [HarmonyPatch(typeof(SkillBook_SE), "ApplySkillBook")]
    internal static class SkillBookCapPatch
    {
        private const float FullRateCredit = 1f;   // below breakpoint: 1 point = 1 full level
        private const float HalfRateCredit = 0.5f; // over breakpoint: 1 point = half a level (2x cost)

        private static bool Prefix(SkillBook_SE __instance, Player ___player, ref bool ___shouldRemove)
        {
            if (___player == null)
            {
                ___shouldRemove = true;
                return false; // skip original
            }

            Skills.Skill skill = ___player.GetSkills().GetSkill(__instance.skillType);

            if (skill.m_level >= Plugin.SkillBookCap.Value)
            {
                ___player.Message(MessageHud.MessageType.Center,
                    "$skill_" + __instance.skillType.ToString().ToLower() + " is capped at " + Plugin.SkillBookCap.Value);
                ___shouldRemove = true;
                return false;
            }

            int pointsToSpend = __instance.bookTier switch
            {
                1 => 1,
                2 => 3,
                3 => 5,
                _ => 1,
            };

            bool leveledUpFullRate = false;
            bool leveledUpDiscountRate = false;

            for (int i = 0; i < pointsToSpend && skill.m_level < Plugin.SkillBookCap.Value; i++)
            {
                bool inDiscountZone = skill.m_level >= Plugin.SkillBookBreakpoint.Value;
                float credit = inDiscountZone ? HalfRateCredit : FullRateCredit;

                bool leveledUpThisPoint = ApplyCredit(skill, ___player, __instance.skillType, credit);

                if (leveledUpThisPoint)
                {
                    if (inDiscountZone) leveledUpDiscountRate = true;
                    else leveledUpFullRate = true;
                }
            }

            string skillKey = "$skill_" + __instance.skillType.ToString().ToLower();

            if (leveledUpDiscountRate)
            {
                ___player.Message(MessageHud.MessageType.Center,
                    skillKey + " increased to " + (int)skill.m_level + " — advanced training yields less",
                    0, skill.m_info?.m_icon);
            }
            else if (leveledUpFullRate)
            {
                ___player.Message(MessageHud.MessageType.Center,
                    skillKey + " increased to " + (int)skill.m_level,
                    0, skill.m_info?.m_icon);
            }
            else
            {
                ___player.Message(MessageHud.MessageType.Center,
                    skillKey + " training progress recorded");
            }

            ___shouldRemove = true;
            return false; // we fully replaced the original logic
        }

        /// <summary>
        /// Adds `credit` worth of effective level-progress (1.0 = one full
        /// level, 0.5 = half a level) to the skill, preserving the existing
        /// fractional progress (as a percentage, not a raw accumulator value)
        /// across any level-up that results. Returns true if the skill's
        /// integer level increased.
        /// </summary>
        private static bool ApplyCredit(Skills.Skill skill, Player player, Skills.SkillType skillType, float credit)
        {
            if (skill.m_level >= Plugin.SkillBookCap.Value) return false;

            float oldRequirement = skill.GetNextLevelRequirement();
            float fraction = oldRequirement > 0f ? skill.m_accumulator / oldRequirement : 0f;

            fraction += credit;

            bool leveledUp = false;

            while (fraction >= 1f && skill.m_level < Plugin.SkillBookCap.Value)
            {
                fraction -= 1f;
                skill.m_level = Mathf.Clamp(skill.m_level + 1f, 0f, Plugin.SkillBookCap.Value);
                player.OnSkillLevelup(skillType, skill.m_level);
                leveledUp = true;
            }

            float newRequirement = skill.GetNextLevelRequirement();
            skill.m_accumulator = fraction * newRequirement;

            return leveledUp;
        }
    }
}
