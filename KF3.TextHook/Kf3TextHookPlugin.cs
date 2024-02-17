using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace KF3.TextHook
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Kf3TextHookPlugin : BaseUnityPlugin
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);
        
        [DllImport("kernel32.dll")]
        static extern IntPtr GlobalLock(IntPtr hMem);
        
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GlobalUnlock(IntPtr hMem);
        
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);
        private static string Clipboard
        {
            set
            {
                var text = Regex.Replace(
                    Regex.Replace(
                        value, @"\[([^:]*):[^\]]*\]", "$1"
                    ), "<[^>]*>", "") + "\0";
                byte[] strBytes = Encoding.Unicode.GetBytes(text);
                var globalMem = GlobalAlloc(2, (UIntPtr)strBytes.Length);
                var buffer = GlobalLock(globalMem);
                Marshal.Copy(strBytes, 0, buffer, strBytes.Length);
                GlobalUnlock(globalMem);
                OpenClipboard(IntPtr.Zero);
                EmptyClipboard();
                SetClipboardData(13, globalMem);
                CloseClipboard();
            }
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