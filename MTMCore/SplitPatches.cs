using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace MTM
{
    [HarmonyPatch(typeof(SplitDialog), "OnEnable")]
    internal static class SplitDialogOnEnablePatch
    {
        private static void Postfix(SplitDialog __instance)
        {
            StackSplitButtons.Setup(__instance);
        }
    }
    
    // Idea, make the four buttons configurable, literal or literal with an op prefix. Add config listener, 
    // find the GO->Rect->Buttons and update on change.
    internal static class StackSplitButtons
    {
        internal static void Setup(SplitDialog splitDialog)
        {
            const string groupName = "StackSplitButtonGroup";
            // OnEnable fires every time the dialog is shown,
            if (splitDialog.m_panel.Find(groupName) != null)
                return;

            const float buttonWidth = 60f;
            const float buttonHeight = 30f;
            const float spacing = 4f; // gap between buttons within the grid
            
            const float groupWidth = 2f * buttonWidth + spacing;
            const float groupHeight = 2f * buttonHeight + spacing;

            var groupGO = new GameObject(groupName, typeof(RectTransform));
            var groupRect = groupGO.GetComponent<RectTransform>();

            groupRect.SetParent(splitDialog.m_panel, false);
            
            // Negative x moves left (into the panel), negative y moves down. A SplitDialog has a bit of a margin
            // on the right side, hence the positive value to push the button group further right.
            var groupOffset = new Vector2(10f, -63f);
            // Anchor and pivot both at the panel's top-right corner, so groupOffset is a simple nudge.
            groupRect.anchorMin = groupRect.anchorMax = new Vector2(1f, 1f);
            groupRect.pivot = new Vector2(1f, 1f);
            groupRect.sizeDelta = new Vector2(groupWidth, groupHeight);
            groupRect.anchoredPosition = groupOffset;

            GameObject MakeButton(string label, Vector2 position, System.Action onClick)
            {
                var go = GUIManager.Instance.CreateButton(
                    text: label,
                    parent: groupRect,
                    anchorMin: new Vector2(0f, 1f),
                    anchorMax: new Vector2(0f, 1f),
                    position: position,
                    width: buttonWidth,
                    height: buttonHeight
                );
                go.GetComponent<Button>().onClick.AddListener(() => onClick());
                return go;
            }

            // Upper-left: split off 1
            MakeButton(Plugin.UpperLeftSplitAmount.Value.ToString(), new Vector2(0f, 0f), () =>
            {
                splitDialog.SliderValue = Plugin.UpperLeftSplitAmount.Value;
                splitDialog.SplitOk();
            });

            // Upper-right: split off 2
            MakeButton(Plugin.UpperRightSplitAmount.Value.ToString(), new Vector2(buttonWidth + spacing, 0f), () =>
            {
                splitDialog.SliderValue = Plugin.UpperRightSplitAmount.Value;
                splitDialog.SplitOk();
            });

            // Lower-left: split off 10
            MakeButton(Plugin.LowerLeftSplitAmount.Value.ToString(), new Vector2(0f, -(buttonHeight + spacing)), () =>
            {
                splitDialog.SliderValue = Plugin.LowerLeftSplitAmount.Value;
                splitDialog.SplitOk();
            });

            // Lower-right: split off 20
            MakeButton(Plugin.LowerRightSplitAmount.Value.ToString(), new Vector2(buttonWidth + spacing, -(buttonHeight + spacing)), () =>
            {
                splitDialog.SliderValue = Plugin.LowerRightSplitAmount.Value;
                splitDialog.SplitOk();
            });
        }
    }
}