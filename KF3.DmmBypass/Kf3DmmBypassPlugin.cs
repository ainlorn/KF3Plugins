using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using DMMHelper;
using HarmonyLib;
using SGNFW.Common.Json;

namespace KF3.DmmBypass
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Kf3DmmBypassPlugin : BaseUnityPlugin
    {
        private const string redirect = "https://dmm.com/";
        private static string userAgent1;
        private static string userAgent2;
        private static ConfigEntry<string> dmmVersion;
        private static ConfigEntry<string> email;
        private static ConfigEntry<string> password;
        private static ConfigEntry<string> mac;
        private static ConfigEntry<string> hdd;
        private static ConfigEntry<string> motherboard;
        private static ConfigEntry<string> proxy;
        private static ManualLogSource _logger;

        private void Awake()
        {
            dmmVersion = Config.Bind("General", "DMMVersion", "5.2.38");
            email = Config.Bind("General", "Email", "");
            password = Config.Bind("General", "Password", "");
            mac = Config.Bind("General", "MACAddress", "02:7e:5d:95:8e:a6");
            hdd = Config.Bind("General", "HDDSerial", "e2ee5295cab1ac94615b0369192981b5794244d585738604df8032e8a4f82d4d");
            motherboard = Config.Bind("General", "MotherboardSerial", "0c9ecd7b6f403947abbee4b967ba24d038244f003953e7a62e1a19aec8a31b44");
            proxy = Config.Bind("General", "ProxyURL", "");
            
            userAgent1 = 
                $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) dmmgameplayer5/{dmmVersion.Value} Chrome/120.0.6099.276 Electron/28.2.2 Safari/537.36";
            userAgent2 = $"DMMGamePlayer5-Win/{dmmVersion.Value} Electron/28.2.2";
            try
            {
                Harmony.CreateAndPatchAll(typeof(Kf3DmmBypassPlugin));
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }

            _logger = Logger;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DMMInitializer), nameof(DMMInitializer.Start))]
        public static void HookPre_DMMInitializer_Start(DMMInitializer __instance)
        {
            __instance.failedFunc = null;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(DMMInitializer), nameof(DMMInitializer.Start))]
        public static void HookPost_DMMInitializer_Start(DMMInitializer __instance)
        {
            try
            {
                var task = Task.Run(AuthInternal);
                task.Wait();
                __instance.loginResult = task.Result;
            }
            catch (Exception e)
            {
                _logger.LogError(e);
            }
        }

        private static async Task<LoginResult> AuthInternal()
        {
            var cookieContainer = new CookieContainer();
            var clientHandler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                UseCookies = true,
                CookieContainer = cookieContainer,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            };
            if (!string.IsNullOrEmpty(proxy.Value))
                clientHandler.Proxy = new WebProxy(proxy.Value);
            var httpClient = new HttpClient(clientHandler);
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent1);

            var loginPage =
                await httpClient.GetStringAsync(
                    $"https://accounts.dmm.com/service/login/password/=/path={redirect}?device=games-player");
            var token = Regex.Match(loginPage, """name="token" value="([^"]*)"/>""").Groups[1].Value;

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["token"] = token;
            query["login_id"] = email.Value;
            query["save_login_id"] = "1";
            query["password"] = password.Value;
            query["save_password"] = "1";
            query["use_auto_login"] = "1";
            query["prompt"] = "";
            query["path"] = redirect;
            query["device"] = "games-player";
            query["recaptchaToken"] = "";
            await httpClient.PostAsync("https://accounts.dmm.com/service/login/password/authenticate",
                new StringContent(query.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded"));

            httpClient.DefaultRequestHeaders.UserAgent.Clear();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent2);
            httpClient.DefaultRequestHeaders.Add("Client-App", "DMMGamePlayer5");
            httpClient.DefaultRequestHeaders.Add("Client-version", dmmVersion.Value);

            var request = new LaunchRequest(
                "kfp2g",
                "GCL",
                "win",
                "LIB",
                mac.Value,
                hdd.Value,
                motherboard.Value,
                "win"
            );
            var resp = await httpClient.PostAsync(
                "https://apidgp-gameplayer.games.dmm.com/v5/launch/cl",
                new StringContent(PrjJson.ToJson(request), Encoding.UTF8, "application/json"));
            var response = PrjJson.FromJson<LaunchResponse>(await resp.Content.ReadAsStringAsync());
            if (response.result_code != 100)
            {
                _logger.LogError($"Auth failed: {response.result_code} {response.error}");
                return new LoginResult();
            }

            var executeArgs = (response.data["execute_args"] as string)!.Split(' ');
            var loginResult = new LoginResult();
            foreach (var arg in executeArgs)
            {
                var spl = arg.Split('=');
                var key = spl[0].Substring(1);
                var value = spl[1];

                switch (key)
                {
                    case "viewer_id":
                        loginResult.viewer_id = value;
                        break;
                    case "onetime_token":
                        loginResult.onetime_token = value;
                        break;
                    case "access_token":
                        loginResult.access_token = value;
                        break;
                }
            }

            return loginResult;
        }
    }

    public class LaunchRequest
    {
        public LaunchRequest(string productID, string gameType, string gameOS, string launchType, string macAddress, string hddSerial, string motherboard, string userOS)
        {
            product_id = productID;
            game_type = gameType;
            game_os = gameOS;
            launch_type = launchType;
            mac_address = macAddress;
            hdd_serial = hddSerial;
            this.motherboard = motherboard;
            user_os = userOS;
        }

        public string product_id;
        public string game_type;
        public string game_os;
        public string launch_type;
        public string mac_address;
        public string hdd_serial;
        public string motherboard;
        public string user_os;
    }

    public class LaunchResponse
    {
        public int result_code;
        public Dictionary<string, object> data;
        public string error;
    }
}