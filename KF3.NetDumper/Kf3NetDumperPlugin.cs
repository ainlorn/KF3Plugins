using System;
using System.IO;
using System.Text;
using BepInEx;
using HarmonyLib;
using SGNFW.Http;
using File = System.IO.File;

namespace KF3.NetDumper
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Kf3NetDumperPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            try
            {
                Harmony.CreateAndPatchAll(typeof(Kf3NetDumperPlugin));
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }

            Directory.CreateDirectory("netdump");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Command), nameof(Command.getRequestJson))]
        public static void Hook_Command_getRequestJson(Command __instance, string __result)
        {
            File.WriteAllText(
                $"netdump/{GetCurrentTimeString()}_req_{__instance.Url.Replace('/', '_')}.json",
                __result);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Command), nameof(Command.Execute))]
        public static void Hook_Command_Parse(Command __instance)
        {
            if (__instance.phase == Command.Phase.ReqFinished && !__instance.connection.IsError)
            {
                var @string = Encoding.UTF8.GetString(__instance.connection.Bytes);
                __instance.connection.Dispose();
                __instance.connection = null;
                __instance.response = __instance.Parse(@string);
                
                File.WriteAllText(
                    $"netdump/{GetCurrentTimeString()}_resp_{__instance.Url.Replace('/', '_')}.json",
                    @string);
                
                if (__instance.response.error_code == null || __instance.response.error_code.id == 0)
                    __instance.phase = Command.Phase.ResSuccess;
                else
                    __instance.phase = Command.Phase.ResError;
            }
        }

        private static string GetCurrentTimeString()
        {
            return DateTime.Now.ToString("yyyyMMdd_HHmmss_ffff");
        }
    }
}