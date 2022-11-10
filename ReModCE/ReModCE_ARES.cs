﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MelonLoader;
using Newtonsoft.Json;
using System.IO;
using NEKOClient.Utils;
using System.Net;
using ABI_RC.Core.Player;
using ABI_RC.Core.UI;
using ABI_RC.Core;
using Harmony;
using UnityEngine;
using System.Collections;
using ABI_RC.Core.Base.Jobs;
using UnityEngine.UI;
using System.Security.Cryptography;
using System.Reflection;

[assembly: MelonInfo(typeof(NEKOClient.NEKOClient), "NEKOClientCVR", "1.0.1", "ChisVR")]
[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]

namespace NEKOClient
{

    public static class BuildInfo
    {
        public const string Name = "NEKOClient";
        public const string Author = "ChisVR";
        public const string Company = null;
        public const string Version = "1.0.0.1";
        public const string DownloadLink = "https://github.com/NEKO-Client/NEKOClientCVR/releases/latest/";
    }

    internal static class GitHubInfo
    {
        public const string Author = "NEKO-Client";
        public const string Repository = "NEKOClientCVR";
        public const string Version = "latest";
    }

    public class NEKOClient : MelonMod
    {

        private static MelonPreferences_Entry<bool> _paranoidMode;

        public static bool NocturnalTagsLoaded { get; private set; }

        private static float a;
        private static float b;
        private static float g;
        private static float r;

        private static List<Json.User> _userArr { get; set; }
        private static GameObject s_namePlate { get; set; }
        private static GameObject s_dev { get; set; }
        private static GameObject s_MainPlateHolder { get; set; }
        private static GameObject s_BigPlateHolder { get; set; }
        private static GameObject s_textMeshProGmj { get; set; }
        private static GameObject s_textMeshProGmj2 { get; set; }
        private static Transform s_plateTransform { get; set; }
        private static TMPro.TextMeshProUGUI s_Logo { get; set; }

        private HarmonyInstance _hInstance { get; } = new HarmonyInstance(Guid.NewGuid().ToString());


        private static void ShowLogo()
        {
            Console.Title = "NEKO CLIENT";
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine(@"=============================================================================================================");
            Console.WriteLine(@"  /$$   /$$ /$$$$$$$$ /$$   /$$  /$$$$$$         /$$$$$$  /$$       /$$$$$$ /$$$$$$$$ /$$   /$$ /$$$$$$$$    ");
            Console.WriteLine(@" | $$$ | $$| $$_____/| $$  /$$/ /$$__  $$       /$$__  $$| $$      |_  $$_/| $$_____/| $$$ | $$|__  $$__/    ");
            Console.WriteLine(@" | $$$$| $$| $$      | $$ /$$/ | $$  \ $$      | $$  \__/| $$        | $$  | $$      | $$$$| $$   | $$       ");
            Console.WriteLine(@" | $$ $$ $$| $$$$$   | $$$$$/  | $$  | $$      | $$      | $$        | $$  | $$$$$   | $$ $$ $$   | $$       ");
            Console.WriteLine(@" | $$  $$$$| $$__/   | $$  $$  | $$  | $$      | $$      | $$        | $$  | $$__/   | $$  $$$$   | $$       ");
            Console.WriteLine(@" | $$\  $$$| $$      | $$\  $$ | $$  | $$      | $$    $$| $$        | $$  | $$      | $$\  $$$   | $$       ");
            Console.WriteLine(@" | $$ \  $$| $$$$$$$$| $$ \  $$|  $$$$$$/      |  $$$$$$/| $$$$$$$$ /$$$$$$| $$$$$$$$| $$ \  $$   | $$       ");
            Console.WriteLine(@" | __/  \__/|________/|__/  \__/ \______/        \______/ |________/|______/|________/|__/  \__/   |__/      ");
            Console.WriteLine("                                                                                                              ");
            Console.WriteLine(@"                                                     /\__ /\                                                 ");
            Console.WriteLine(@"                                                    /`     '\                                                ");
            Console.WriteLine(@"                                                    === 0  0 ===                                             ");
            Console.WriteLine(@"                                                     \   --  /                                               ");
            Console.WriteLine(@"                                                     /       \                                               ");
            Console.WriteLine(@"                                                    /         \                                              ");
            Console.WriteLine(@"                                                   |           |                                             ");
            Console.WriteLine(@"                                                   \   ||  ||  /                                             ");
            Console.WriteLine(@"                                                    \_oo__oo_ /#######o                                      ");
            Console.WriteLine("                                                                                                              ");
            Console.WriteLine(@"                          NEKOClient is Nametag MOD (OLD VRC MOD)- By ChisVR, Bison, Aries                    ");
            Console.WriteLine(@"=============================================================================================================");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public override void OnApplicationStart()
        {
            DownloadFromGitHub("NEKOClient", out var assembly);

            if (assembly == null)
                return;

            ShowLogo();
            NocturnalTagsLoaded = MelonHandler.Mods.Any(m => m.Info.Name == "Nocturnal Plates");
            MelonLogger.Msg("Initializing.");
            MelonLogger.Msg("NEKOClient Loaded. Press Slash To Reload Tags");
            DownloadString();
            _hInstance.Patch(typeof(PlayerNameplate).GetMethod(nameof(PlayerNameplate.UpdateNamePlate)), null, typeof(Main).GetMethod(nameof(OnPlayerJoin), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).ToNewHarmonyMethod());
            MelonCoroutines.Start(WaitForNamePlate());

        }

        private static IEnumerator WaitForNamePlate()
        {
            while (Resources.FindObjectsOfTypeAll<PuppetMaster>() == null)
                yield return null;
            s_namePlate = Resources.FindObjectsOfTypeAll<PuppetMaster>().FirstOrDefault(x => x.name == "_NetworkedPlayerObject").transform.Find("[NamePlate]/Canvas/Content").gameObject;
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Slash))
            {
                ReloadString();
                MelonLogger.Msg("Reloaded Tags, Please Rejoin World.");
                CohtmlHud.Instance.ViewDropText("NEKOClient", "Connected", "Connected To NEKOClient");
            }
        }

        private void ReloadString()
        {
            _userArr.Clear();
            DownloadString();
        }

        private static string s_uId { get; set; }
        private static Json.User s_user { get; set; }

        private static void OnPlayerJoin(PlayerNameplate __instance)
        {
            s_uId = __instance.transform.parent.name;
            s_user = _userArr.FirstOrDefault(x => x.UserId == s_uId);
            if (s_user == null) return;
            for (int i = 0; i < s_user.NamePlatesText.Length; i++)
                GeneratePlate(s_uId, s_user.NamePlatesText[i], i, new Color32(byte.Parse(s_user.Color[0].ToString()), byte.Parse(s_user.Color[1].ToString()), byte.Parse(s_user.Color[2].ToString()), byte.Parse(s_user.Color[3].ToString())));
            for (int i = 0; i < s_user.BigPlatesText.Length; i++)
                GenerateBigPlate(s_uId, s_user.BigPlatesText[i], i);
            //CreateLogo(s_uId);
        }

        private static float s_textCount { get; set; }
        private static GameObject s_imageHolder { get; set; }

        private static void GeneratePlate(string uid, string plateText, int multiplier, Color32 color)
        {
            //MelonLogger.Msg("---PlateText---");
            //MelonLogger.Msg(plateText);
            //MelonLogger.Msg("---PlateText Length---");
            //MelonLogger.Msg(plateText.Length);
            try
            {
                s_textCount = plateText.Contains("<color=") ? plateText.Length - (Regex.Matches(plateText, "<color=").Count != 1 ? Regex.Matches(plateText, "<color=").Count * 23 - 3 : 20) : plateText.Length;
                s_MainPlateHolder = GameObject.Instantiate(s_namePlate, GameObject.Find("/" + uid + "[NamePlate]/Canvas").transform);
                s_MainPlateHolder.transform.localPosition = new Vector3(0, -0.155f - (multiplier) * 0.0778f, 0);
                s_imageHolder = s_MainPlateHolder.transform.Find("Image").gameObject;
                s_imageHolder.GetComponent<UnityEngine.UI.Image>().color = color;
                GameObject.Destroy(s_MainPlateHolder.transform.Find("Image/FriendsIndicator").gameObject);
                GameObject.Destroy(s_MainPlateHolder.transform.Find("Image/ObjectMaskSlave").gameObject);
                GameObject.Destroy(s_MainPlateHolder.transform.Find("Disable with Menu").gameObject);
                s_MainPlateHolder.transform.localScale = new Vector3(0.3f, 0.3f, 1);
                s_imageHolder.transform.localScale = new Vector3(1, 0.5f, 1);
                s_imageHolder.GetComponent<RectTransform>().sizeDelta = new Vector2(s_textCount / 10, 0.5f);
                s_textMeshProGmj = s_MainPlateHolder.transform.Find("TMP:Username").gameObject;
                s_textMeshProGmj.transform.localScale = new Vector3(0.58f, 0.58f, 1);
                s_textMeshProGmj.transform.localPosition = Vector3.zero;
                s_textMeshProGmj.GetComponent<TMPro.TextMeshProUGUI>().text = plateText;
                s_textMeshProGmj.GetComponent<TMPro.TextMeshProUGUI>().alignment = TMPro.TextAlignmentOptions.Center;
                s_textMeshProGmj.GetComponent<TMPro.TextMeshProUGUI>().autoSizeTextContainer = true;
                s_textMeshProGmj.gameObject.GetComponent<UnityEngine.RectTransform>().anchoredPosition = new Vector2(-0.05f, 0f);


                //Done Just For Removing The Text Under Devs/Mods ect
                s_dev = GameObject.Find("/" + uid + "[NamePlate]/Canvas/Content/Disable with Menu").gameObject.GetComponent<RectTransform>().gameObject;
                s_dev.transform.gameObject.SetActive(false);
            }
            catch { }
        }

        //Just Gonna Duplicate It For Big Text Because Im Lazy Asf
        private static void GenerateBigPlate(string uid, string plateText, int multiplier)
        {
            try
            {
                s_BigPlateHolder = GameObject.Instantiate(s_namePlate, GameObject.Find("/" + uid + "[NamePlate]/Canvas").transform);
                string[] splited = plateText.Split(new string[] { "<size=" }, StringSplitOptions.None);
                string sizeString = string.Empty;
                for (int i = 0; i < splited[1].Length; i++)
                {
                    if (!char.IsDigit(splited[1][i])) break;
                    sizeString += splited[1][i];
                }
                s_BigPlateHolder.transform.localPosition = NocturnalTagsLoaded ? new Vector3(0, 0.758f + (int.Parse(sizeString)) * 0.0075f, 0) : new Vector3(0, 0.45f + (int.Parse(sizeString)) * 0.0035f, 0);
                GameObject.Destroy(s_BigPlateHolder.transform.Find("Image").gameObject.GetComponent<UnityEngine.UI.Image>());
                GameObject.Destroy(s_BigPlateHolder.transform.Find("Image/FriendsIndicator").gameObject);
                GameObject.Destroy(s_BigPlateHolder.transform.Find("Image/ObjectMaskSlave").gameObject);
                GameObject.Destroy(s_BigPlateHolder.transform.Find("Disable with Menu").gameObject);
                s_textMeshProGmj2 = s_BigPlateHolder.transform.Find("TMP:Username").gameObject;
                s_textMeshProGmj2.transform.localPosition = Vector3.zero;
                s_textMeshProGmj2.GetComponent<TMPro.TextMeshProUGUI>().text = plateText;
                s_textMeshProGmj2.GetComponent<TMPro.TextMeshProUGUI>().autoSizeTextContainer = true;
                s_textMeshProGmj2.GetComponent<TMPro.TextMeshProUGUI>().color = new Color(r, g, b, 0.55f);
                s_textMeshProGmj2.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
            }
            catch { }
        }

        //At Some Point Will Make This Show Up For Each Person Running The Mod If I Figure Out How To Since I Don't Have A Server
        public static void CreateLogo(string uid)
        {
            s_plateTransform = GameObject.Find("/" + uid + "[NamePlate]/Canvas").transform;
            s_Logo = GameObject.Instantiate(s_plateTransform.transform.Find("Content/TMP:Username").gameObject, s_plateTransform.transform.Find("Content").transform).gameObject.GetComponent<TMPro.TextMeshProUGUI>();
            s_Logo.text = "<b><i><color=#00FFFF>NC</color>";
            s_Logo.outlineWidth = 0.23f;
            s_Logo.outlineColor = new Color32(0, 0, 0, 255);
            s_Logo.autoSizeTextContainer = true;
            s_Logo.enableAutoSizing = false;
            s_Logo.fontSize = 0.19f;
            s_Logo.transform.localPosition = new Vector3(-1.35f, -0.45f);
            GameObject.Find("/" + uid + "[NamePlate]/Canvas").transform.localScale = new Vector3(0.45f, 0.45f, 1);
        }

        private static void DownloadString()
        {
            using (WebClient wc = new WebClient())
                _userArr = JsonConvert.DeserializeObject<List<Json.User>>(wc.DownloadString("https://api.chisdealhd.co.uk/v2/games/api/chilloutvrclient/nekoclientcvr/assets/NameTags"));
        }









        private void DownloadFromGitHub(string fileName, out Assembly loadedAssembly)
        {
            if (fileName == "NEKOClient")
            {

                using var sha256 = SHA256.Create();

                byte[] bytes = null;
                if (File.Exists($"Mods/{fileName}.dll"))
                {
                    bytes = File.ReadAllBytes($"Mods/{fileName}.dll");
                }

                using var wc = new WebClient
                {
                    Headers =
                {
                    ["User-Agent"] =
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:87.0) Gecko/20100101 Firefox/87.0"
                }
                };

                byte[] latestBytes = null;
                try
                {
                    latestBytes = wc.DownloadData($"https://api.chisdealhd.co.uk/v2/games/api/vrchatclient/nekoclientcvr/assets/{fileName}");
                }
                catch (WebException e)
                {
                    MelonLogger.Error($"Unable to download latest version of ReModCE: {e}");
                }

                if (bytes == null)
                {
                    if (latestBytes == null)
                    {
                        MelonLogger.Error($"No local file exists and unable to download latest version from GitHub. {fileName} will not load!");
                        loadedAssembly = null;
                        return;
                    }
                    MelonLogger.Warning($"Couldn't find {fileName}.dll on disk. Saving latest version from GitHub.");
                    bytes = latestBytes;
                    try
                    {
                        File.WriteAllBytes($"Mods/{fileName}.dll", bytes);
                    }
                    catch (IOException)
                    {
                        ReLogger.Warning($"Failed writing {fileName} to disk. You may encounter errors while using ReModCE.");
                    }
                }

                if (latestBytes != null)
                {
                    var latestHash = ComputeHash(sha256, latestBytes);
                    var currentHash = ComputeHash(sha256, bytes);

                    if (latestHash != currentHash)
                    {
                        if (_paranoidMode.Value)
                        {
                            MelonLogger.Msg(ConsoleColor.Cyan,
                                $"There is a new version of ReModCE available. You can either delete the \"{fileName}.dll\" from your VRChat directory or go to https://github.com/{GitHubInfo.Author}/{GitHubInfo.Repository}/releases/latest/ and download the latest version.");
                        }
                        else
                        {
                            bytes = latestBytes;
                            try
                            {
                                File.WriteAllBytes($"Mods/{fileName}.dll", bytes);
                            }
                            catch (IOException)
                            {
                                ReLogger.Warning($"Failed writing {fileName} to disk. You may encounter errors while using ReModCE.");
                            }
                            MelonLogger.Msg(ConsoleColor.Green, $"Updated {fileName} to latest version.");
                        }
                    }
                }


                try
                {
                    loadedAssembly = Assembly.Load(bytes);
                }
                catch (BadImageFormatException e)
                {
                    MelonLogger.Error($"Couldn't load specified image: {e}");
                    loadedAssembly = null;
                }

            }
            else
            {

                using var sha256 = SHA256.Create();

                byte[] bytes = null;
                if (File.Exists($"Mods/{fileName}.dll"))
                {
                    bytes = File.ReadAllBytes($"Mods/{fileName}.dll");
                }

                using var wc = new WebClient
                {
                    Headers =
                {
                    ["User-Agent"] =
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:87.0) Gecko/20100101 Firefox/87.0"
                }
                };

                byte[] latestBytes = null;
                try
                {
                    latestBytes = wc.DownloadData($"https://api.chisdealhd.co.uk/v2/games/api/vrchatclient/nekoclientcvr/assets/{fileName}");
                }
                catch (WebException e)
                {
                    MelonLogger.Error($"Unable to download latest version of ReModCE: {e}");
                }

                if (bytes == null)
                {
                    if (latestBytes == null)
                    {
                        MelonLogger.Error($"No local file exists and unable to download latest version from GitHub. {fileName} will not load!");
                        loadedAssembly = null;
                        return;
                    }
                    MelonLogger.Warning($"Couldn't find {fileName}.dll on disk. Saving latest version from GitHub.");
                    bytes = latestBytes;
                    try
                    {
                        File.WriteAllBytes($"Mods/{fileName}.dll", bytes);
                    }
                    catch (IOException)
                    {
                        ReLogger.Warning($"Failed writing {fileName} to disk. You may encounter errors while using ReModCE.");
                    }
                }

                if (latestBytes != null)
                {
                    var latestHash = ComputeHash(sha256, latestBytes);
                    var currentHash = ComputeHash(sha256, bytes);

                    if (latestHash != currentHash)
                    {
                        if (_paranoidMode.Value)
                        {
                            MelonLogger.Msg(ConsoleColor.Cyan,
                                $"There is a new version of ReModCE available. You can either delete the \"{fileName}.dll\" from your VRChat directory or go to https://github.com/{GitHubInfo.Author}/{GitHubInfo.Repository}/releases/latest/ and download the latest version.");
                        }
                        else
                        {
                            bytes = latestBytes;
                            try
                            {
                                File.WriteAllBytes($"Mods/{fileName}.dll", bytes);
                            }
                            catch (IOException)
                            {
                                ReLogger.Warning($"Failed writing {fileName} to disk. You may encounter errors while using ReModCE.");
                            }
                            MelonLogger.Msg(ConsoleColor.Green, $"Updated {fileName} to latest version.");
                        }
                    }
                }


                try
                {
                    loadedAssembly = Assembly.Load(bytes);
                }
                catch (BadImageFormatException e)
                {
                    MelonLogger.Error($"Couldn't load specified image: {e}");
                    loadedAssembly = null;
                }
            }
        }


        private static string ComputeHash(HashAlgorithm sha256, byte[] data)
        {
            var bytes = sha256.ComputeHash(data);
            var sb = new StringBuilder();
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }






    }
}
