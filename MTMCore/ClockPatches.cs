using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace MTM
{
    /// <summary>
    /// Adds an English-word time-of-day label to the lower-left corner of the minimap.
    /// Postfixes Minimap.Awake to create the text as a child of the minimap's own panel,
    /// so it inherits the minimap's show/hide behavior for free.
    /// </summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Start))]
    public static class Minimap_Start_TextClockPatch
    {
        private const float OffsetX = -5f;
        private const float OffsetY = 0f;
        private const int FontSize = 16;
        private static readonly Color TextColor = new Color(1f, 1f, 1f, 0.9f);
        private const float UpdateIntervalSeconds = 2f;

        private static void Postfix(Minimap __instance)
        {
            var parent = __instance.m_smallRoot.transform;

            var biomeFont = __instance?.m_biomeNameLarge?.font;
            if (biomeFont is not null) 
                Plugin.Log.LogDebug(biomeFont.name);
            
            GameObject textGo = GUIManager.Instance.CreateText(
                text: "",
                parent: parent,
                anchorMin: new Vector2(1f, 0f),
                anchorMax: new Vector2(1f, 0f),
                position: new Vector2(OffsetX, OffsetY),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: FontSize,
                color: TextColor,
                outline: true,
                outlineColor: Color.black,
                width: 200f,
                height: 30f,
                addContentSizeFitter: true);

            // Pivot top-right so the text grows away from the map (down/left).
            RectTransform rt = textGo.GetComponent<RectTransform>();
            rt.pivot = new Vector2(1f, 1f);

            Text label = textGo.GetComponent<Text>();
            label.alignment = TextAnchor.LowerRight;

            textGo.AddComponent<TimeOfDayLabelUpdater>().Bind(label);
        }

        private class TimeOfDayLabelUpdater : MonoBehaviour
        {
            private Text _label;
            private float _timer;
            private string _lastText = string.Empty;

            public void Bind(Text label)
            {
                _label = label;
            }

            private void Update()
            {
                _timer -= Time.deltaTime;
                if (_timer > 0f)
                    return;
                _timer = UpdateIntervalSeconds;

                // Assumes EnvMan.GetDayFraction() returns 0-1, where 0/1 = midnight, 0.5 = midday.
                float fraction = EnvMan.instance.GetDayFraction();
                string text = TimeOfDay.GetLabel(fraction);
                var label = $"{text} ({fraction})";
                if (label != _lastText)
                {
                    _label.text = label;
                    _lastText = label;
                }
            }
        }
    }

    /// <summary>
    /// Maps a 0-1 day fraction (0/1 = midnight, 0.5 = midday) to an English time-of-day label.
    /// Boundaries are hardcoded to an even 24-hour spread; adjust the constants below if a
    /// transition doesn't match what you see in-game.
    /// </summary>
    public static class TimeOfDay
    {
        // There isn't a linear correlation of dayFraction to the in-game passage of time. Night is 9 minutes
        // real-time, and day is 21 minutes.
        
        private const float WeeHoursStart = 0.15f;
        private const float DawnStart = 0.2f; 
        private const float MorningStart = 0.25f; // Cold ends and Day starts
        private const float MidDayStart = 0.35f;
        private const float EveningStart = 0.60f;
        private const float DuskStart = 0.70f; 
        private const float NightfallStart = 0.75f; // Cold starts and Night begins
        private const float MidnightStart = 0.85f;

        public static string GetLabel(float dayFraction)
        {
            return dayFraction switch
            {
                >= MidnightStart => "Midnight", // .85 to 1
                >= NightfallStart => "Nightfall",
                >= DuskStart => "Dusk",
                >= EveningStart => "Evening",
                >= MidDayStart => "Mid-day",
                >= MorningStart => "Morning",
                >= DawnStart => "Dawn",
                >= WeeHoursStart => "Jiig O'Clock",
                _ => "Midnight" // 0 to 0.1499999
            };
        }
    }
}