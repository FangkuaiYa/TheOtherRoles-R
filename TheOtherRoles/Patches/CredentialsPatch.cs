using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using HarmonyLib;
using InnerNet;
using TheOtherRoles.CustomGameModes;
using TheOtherRoles.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace TheOtherRoles.Patches;

[HarmonyPatch]
public static class CredentialsPatch
{
    [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
    internal static class PingTrackerPatch
    {
        private static void Postfix(PingTracker __instance)
        {
            var gameModeText = "";
            if (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
            {
                if (TORMapOptions.gameMode == CustomGamemodes.HideNSeek) gameModeText = "Hide 'N Seek";
                else if (TORMapOptions.gameMode == CustomGamemodes.Guesser) gameModeText = "Guesser";
                else if (TORMapOptions.gameMode == CustomGamemodes.PropHunt) gameModeText = "Prop Hunt";

                try
                {
                    var GameModeText = GameObject.Find("GameModeText")?.GetComponent<TextMeshPro>();
                    GameModeText.text = gameModeText == ""
                        ? GameOptionsManager.Instance.currentGameOptions.GameMode == GameModes.HideNSeek
                            ? "Van. HideNSeek"
                            : "Classic"
                        : gameModeText;
                    var ModeLabel = GameObject.Find("ModeLabel")?.GetComponentInChildren<TextMeshPro>();
                    ModeLabel.text = "Game Mode";
                }
                catch
                {
                }
            }
            else
            {
                if (HideNSeek.isHideNSeekGM) gameModeText = "Hide 'N Seek";
                else if (HandleGuesser.isGuesserGm) gameModeText = "Guesser";
                else if (PropHunt.isPropHuntGM) gameModeText = "Prop Hunt";
            }

            if (gameModeText != "")
                gameModeText = "- " + Helpers.cs(Color.yellow, gameModeText);

            var myText =
                $"<align=center><size=60%><space=3em><color=#ff351f>TheOtherRoles</color>v{TheOtherRolesPlugin.Version + (TheOtherRolesPlugin.isBeta ? "-BETA" : "")} {gameModeText}</size></align>";

            if (!__instance.text.text.EndsWith("\n"))
                __instance.text.text += "\n";

            __instance.text.text += myText;
        }
    }

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public static class LogoPatch
    {
        public static SpriteRenderer renderer;
        public static Sprite bannerSprite;
        public static Sprite horseBannerSprite;
        public static Sprite banner2Sprite;
        private static PingTracker instance;

        public static GameObject motdObject;
        public static TextMeshPro motdText;

        private static void Postfix(PingTracker __instance)
        {
            var torLogo = new GameObject("bannerLogo_TOR");
            torLogo.transform.SetParent(GameObject.Find("RightPanel").transform, false);
            torLogo.transform.localPosition = new Vector3(-0.4f, 1f, 5f);

            renderer = torLogo.AddComponent<SpriteRenderer>();
            instance = __instance;
            renderer.sprite = EventUtility.isEnabled
                ? Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Banner2.png", 300f)
                : Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Banner.png", 300f);
            var credentialObject = new GameObject("credentialsTOR");
            var credentials = credentialObject.AddComponent<TextMeshPro>();
            credentials.SetText(
                $"v{TheOtherRolesPlugin.Version + (TheOtherRolesPlugin.isBeta ? "-BETA" : "")}\n<size=30f%>\n</size>{ModTranslation.GetString("Credentials-Text", 1)}\n<size=30%>\n</size>{ModTranslation.GetString("Credentials-Text", 2)}");
            credentials.alignment = TextAlignmentOptions.Center;
            credentials.fontSize *= 0.05f;

            credentials.transform.SetParent(torLogo.transform);
            credentials.transform.localPosition = Vector3.down * 1.25f;
            motdObject = new GameObject("torMOTD");
            motdText = motdObject.AddComponent<TextMeshPro>();
            motdText.alignment = TextAlignmentOptions.Center;
            motdText.fontSize *= 0.04f;

            motdText.transform.SetParent(torLogo.transform);
            motdText.enableWordWrapping = true;
            var rect = motdText.gameObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(5.2f, 0.25f);

            motdText.transform.localPosition = Vector3.down * 2.25f;
            motdText.color = new Color(1, 53f / 255, 31f / 255);
            var mat = motdText.fontSharedMaterial;
            mat.shaderKeywords = new[] { "OUTLINE_ON" };
            motdText.SetOutlineColor(Color.white);
            motdText.SetOutlineThickness(0.025f);
        }
    }

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.LateUpdate))]
    public static class MOTD
    {
        public static List<string> motds = new();
        private static float timer;
        private static readonly float maxTimer = 5f;
        private static int currentIndex;

        public static void Postfix()
        {
            if (motds.Count == 0)
            {
                timer = maxTimer;
                return;
            }

            if (motds.Count > currentIndex && LogoPatch.motdText != null)
                LogoPatch.motdText.SetText(motds[currentIndex]);
            else return;

            // fade in and out:
            var alpha = Mathf.Clamp01(Mathf.Min(new[] { timer, maxTimer - timer }));
            if (motds.Count == 1) alpha = 1;
            LogoPatch.motdText.color = LogoPatch.motdText.color.SetAlpha(alpha);
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                timer = maxTimer;
                currentIndex = (currentIndex + 1) % motds.Count;
            }
        }

        public static async Task loadMOTDs()
        {
            var request = UnityWebRequest.Get("https://api.amongusclub.cn/TheOtherRoles-R/motd.txt");
            request.SendWebRequest();
            // Wait for the request to complete
            while (!request.isDone)
            {
            }

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                TheOtherRolesPlugin.Logger.LogError($"Couldn't fetch mod news from Server: {request.error}");
                return;
            }

            var motdsText = request.downloadHandler.text;
            foreach (var line in motdsText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                motds.Add(line.Trim());
        }
    }
}