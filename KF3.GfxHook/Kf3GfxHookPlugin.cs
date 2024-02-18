using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SGNFW.Touch;
using UnityEngine;

namespace KF3.GfxHook
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Kf3GfxHookPlugin : BaseUnityPlugin
    {
        private static ConfigEntry<float> renderTextureScaleFactor;
        private static ConfigEntry<int> targetFps;
        private static ConfigEntry<bool> vsync;
        private static ConfigEntry<int> qualityLevel;
        private static RenderTexture dummyTexture;
        private static RenderTexture oTexture;
        private int oWidth = 1280;
        private int oHeight = 720;
        
        private void Awake()
        {
            targetFps = Config.Bind("General", "FPSTarget", -1);
            vsync = Config.Bind("General", "VSync", true);
            qualityLevel = Config.Bind("General", "QualityLevel", 5);
            renderTextureScaleFactor = Config.Bind("General", "RenderTextureScaleFactor", 2.0f);
            
            try
            {
                Harmony.CreateAndPatchAll(typeof(Kf3GfxHookPlugin));
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Return))
            {
                if (!Screen.fullScreen)
                {
                    oWidth = Screen.width;
                    oHeight = Screen.height;
                    Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
                }
                else
                {
                    Screen.SetResolution(oWidth, oHeight, false);
                }
            }
        }

        private void Start()
        {
            dummyTexture = new RenderTexture(1280, 720, 0);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SceneHome), nameof(SceneHome.OnCreateScene))]
        public static void Hook_SceneHome_OnCreateScene()
        {
            Application.targetFrameRate = targetFps.Value;
            QualitySettings.vSyncCount = vsync.Value ? 1 : 0;
            QualitySettings.SetQualityLevel(qualityLevel.Value, true);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(RenderTextureChara), nameof(RenderTextureChara.SetupRenderTexture))]
        public static void HookPre_RenderTextureChara_SetupRenderTexture(ref int w, ref int h)
        {
            w = (int)(w * renderTextureScaleFactor.Value);
            h = (int)(h * renderTextureScaleFactor.Value);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(RenderTextureChara), nameof(RenderTextureChara.SetupRenderTexture))]
        public static void HookPost_RenderTextureChara_SetupRenderTexture(RenderTextureChara __instance)
        {
            var scl = 1.0f / renderTextureScaleFactor.Value;
            var newComerObj = GameObject.Find("NewComer");
            var heartLvUpObj = GameObject.Find("Auth_HeartLvUp");
            if (newComerObj != null && newComerObj.activeSelf
                || heartLvUpObj != null && heartLvUpObj.activeSelf)
                scl /= renderTextureScaleFactor.Value;
            __instance.dispTexture.transform.localScale = new Vector3(scl, scl, scl);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(RenderTextureChara), nameof(RenderTextureChara.OnTouchTap))]
        public static void HookPre_RenderTextureChara_OnTouchTap(RenderTextureChara __instance)
        {
            oTexture = __instance.dispCamera.targetTexture;
            dummyTexture.width = (int)(oTexture.width / renderTextureScaleFactor.Value);
            dummyTexture.height = (int)(oTexture.height / renderTextureScaleFactor.Value);
            __instance.dispCamera.targetTexture = dummyTexture;
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(RenderTextureChara), nameof(RenderTextureChara.OnTouchTap))]
        public static void HookPost_RenderTextureChara_OnTouchTap(RenderTextureChara __instance)
        {
            __instance.dispCamera.targetTexture = oTexture;
        }
    }
}
