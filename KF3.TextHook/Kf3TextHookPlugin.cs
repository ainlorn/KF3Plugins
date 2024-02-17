using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace KF3.TextHook
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Kf3TextHookPlugin : BaseUnityPlugin
    {
        private int waitFrames = -1;

        private static string Clipboard
        {
            set => GUIUtility.systemCopyBuffer = Regex.Replace(
                Regex.Replace(
                    value, @"\[([^:]*):[^\]]*\]", "$1"
                ), "<[^>]*>", "");
        }

        private void Awake()
        {
            try
            {
                Harmony.CreateAndPatchAll(typeof(Kf3TextHookPlugin));
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void Update()
        {
            // copy hovered text on ctrl
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                var mousePos = Input.mousePosition;
                var allText = FindObjectsOfType<Text>();
                foreach (var text in allText)
                {
                    var corners = new Vector3[4];
                    text.rectTransform.GetWorldCorners(corners);
                    for (var i = 0; i < corners.Length; i++)
                        corners[i] = RectTransformUtility.WorldToScreenPoint(text.canvas.worldCamera, corners[i]);
                    var screenRect = new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x,
                        corners[2].y - corners[0].y);
                    if (screenRect.Contains(mousePos))
                    {
                        Clipboard = text.text;
                        return;
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ScenarioScene), nameof(ScenarioScene.SetSerifCommon))]
        public static void Hook_ScenarioScene_SetSerifCommon(ScenarioScene __instance)
        {
            Clipboard = __instance.GUIs.mSerifText.currentText;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PguiOpenWindowCtrl), nameof(PguiOpenWindowCtrl.Setup))]
        [HarmonyPatch(typeof(PguiOpenWindowCtrl), nameof(PguiOpenWindowCtrl.SetupTerms))]
        [HarmonyPatch(typeof(PguiOpenWindowCtrl), nameof(PguiOpenWindowCtrl.SetupButtonOnly))]
        [HarmonyPatch(typeof(PguiOpenWindowCtrl), nameof(PguiOpenWindowCtrl.SetupCheckBox))]
        [HarmonyPatch(typeof(PguiOpenWindowCtrl), nameof(PguiOpenWindowCtrl.SetupItemInfoInternal))]
        [HarmonyPatch(typeof(PguiOpenWindowCtrl), nameof(PguiOpenWindowCtrl.SetupTitleGraphic))]
        [HarmonyPatch(typeof(PguiOpenWindowCtrl), nameof(PguiOpenWindowCtrl.SetupByMonthlyPack))]
        [HarmonyPatch(typeof(PguiOpenWindowCtrl), nameof(PguiOpenWindowCtrl.SetupByNoStone))]
        [HarmonyPatch(typeof(PguiOpenWindowCtrl), nameof(PguiOpenWindowCtrl.SetupByQuestSkip))]
        [HarmonyPatch(typeof(PguiOpenWindowCtrl), nameof(PguiOpenWindowCtrl.SetupByPurchaseStone))]
        [HarmonyPatch(typeof(PguiOpenWindowCtrl), nameof(PguiOpenWindowCtrl.SetupByStaminaSelect))]
        [HarmonyPatch(typeof(PguiOpenWindowCtrl), nameof(PguiOpenWindowCtrl.SetupByStaminaSetting))]
        [HarmonyPatch(typeof(PguiOpenWindowCtrl), nameof(PguiOpenWindowCtrl.SetupByStaminaUse))]
        [HarmonyPatch(typeof(PguiOpenWindowCtrl), nameof(PguiOpenWindowCtrl.SetupByUseItem))]
        public static void Hook_PguiOpenWindowCtrl_Setup(PguiOpenWindowCtrl __instance)
        {
            if (__instance.m_MassageText != null
                && __instance.m_MassageText.text != null
                && !__instance.m_MassageText.text.StartsWith("あいうえおかきくけこ"))
            {
                Clipboard = __instance.m_MassageText.text;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GachaAuthCtrl), nameof(GachaAuthCtrl.PlayGreeting))]
        public static void Hook_GachaAuthCtrl_PlayGreeting(GachaAuthCtrl __instance)
        {
            Clipboard = __instance.gachaAeGreeting.Txt_Serif.text;
        }

        private static String lastText = "";

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SceneHome), nameof(SceneHome.Update))]
        public static void Hook_SceneHome_Update(SceneHome __instance)
        {
            if (__instance.friendsMenu.gameObject.activeSelf)
            {
                var newText = __instance.friendsMenu.transform.Find("SerifWindow/Txt")
                    .GetComponent<PguiTextCtrl>().text;
                if (lastText == newText)
                    return;
                Clipboard = newText;
                lastText = newText;
            }
        }
    }
}