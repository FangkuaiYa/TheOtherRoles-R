using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using TheOtherRoles.Patches;
using TheOtherRoles.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TheOtherRoles.Modules;

[HarmonyPatch]
internal class RoleDraft
{
    public static bool isRunning;

    public static List<byte> pickOrder = new();
    private static bool picked;
    private static float timer;
    private static readonly List<ActionButton> buttons = new();
    private static TextMeshPro feedText;
    public static List<byte> alreadyPicked = new();
    private static List<byte> initialPickOrder = new();

    public static bool isEnabled => CustomOptionHolder.isDraftMode.getBool() &&
                                    (TORMapOptions.gameMode == CustomGamemodes.Classic ||
                                     TORMapOptions.gameMode == CustomGamemodes.Guesser);

    private static bool canChat => CustomOptionHolder.draftModeCanChat.getBool();

    public static IEnumerator CoSelectRoles(IntroCutscene __instance)
    {
        if (!isEnabled) yield break;

        isRunning = true;
        SoundEffectsManager.play("draft", 1f, true, true);
        alreadyPicked.Clear();
        initialPickOrder.Clear();
        var playedAlert = false;

        // Show chat during draft if enabled
        if (canChat) HudManager.Instance.Chat.SetVisible(true);

        feedText = Object.Instantiate(__instance.TeamTitle, __instance.transform);
        var aspectPosition = feedText.gameObject.AddComponent<AspectPosition>();
        aspectPosition.Alignment = AspectPosition.EdgeAlignments.LeftTop;
        aspectPosition.DistanceFromEdge = new Vector2(1.62f, 1.2f);
        aspectPosition.AdjustPosition();
        feedText.transform.localScale = new Vector3(0.6f, 0.6f, 1);
        feedText.text = $"<size=200%>{ModTranslation.GetString("RoleDraft-Text", 1)}</size>\n\n";
        feedText.alignment = TextAlignmentOptions.TopLeft;
        feedText.autoSizeTextContainer = true;
        feedText.fontSize = 3f;
        feedText.enableAutoSizing = false;
        __instance.TeamTitle.transform.localPosition =
            __instance.TeamTitle.transform.localPosition + new Vector3(1f, 0f);
        __instance.TeamTitle.text = ModTranslation.GetString("RoleDraft-Text", 2);
        __instance.BackgroundBar.enabled = false;
        __instance.TeamTitle.transform.localScale = new Vector3(0.25f, 0.25f, 1f);
        __instance.TeamTitle.autoSizeTextContainer = true;
        __instance.TeamTitle.enableAutoSizing = false;
        __instance.TeamTitle.fontSize = 5;
        __instance.TeamTitle.alignment = TextAlignmentOptions.Top;
        __instance.ImpostorText.gameObject.SetActive(false);
        GameObject.Find("BackgroundLayer")?.SetActive(false);
        foreach (var player in Object.FindObjectsOfType<PoolablePlayer>())
            if (player.name.Contains("Dummy"))
                player.gameObject.SetActive(false);

        __instance.FrontMost.gameObject.SetActive(false);

        if (AmongUsClient.Instance.AmHost) sendPickOrder();

        // Random order animation
        yield return CoShowRandomOrderAnimation(__instance);

        while (pickOrder.Count == 0) yield return null;

        initialPickOrder = new List<byte>(pickOrder);

        foreach (var playerId in pickOrder)
            alreadyPicked.Add((byte)0); // placeholder for tracking

        while (pickOrder.Count > 0)
        {
            picked = false;
            timer = 0;
            var maxTimer = CustomOptionHolder.draftModeTimeToChoose.getFloat();
            var playerText = "";
            while (timer < maxTimer || !picked)
            {
                if (pickOrder.Count == 0)
                    break;
                // wait for pick
                timer += Time.deltaTime;
                if (PlayerControl.LocalPlayer.PlayerId == pickOrder[0])
                {
                    if (!playedAlert)
                    {
                        playedAlert = true;
                        SoundManager.Instance.PlaySound(ShipStatus.Instance.SabotageSound, false);
                    }

                    // Animate beginning of choice, by changing background color
                    var min = 50 / 255f;
                    var backGroundColor = new Color(min, min, min, 1);
                    if (timer < 1)
                    {
                        var max = 230 / 255f;
                        if (timer < 0.5f)
                        {
                            // White flash                              
                            var p = timer / 0.5f;
                            var value = (float)Math.Pow(p, 2f) * max;
                            backGroundColor = new Color(value, value, value, 1);
                        }
                        else
                        {
                            var p = (1 - timer) / 0.5f;
                            var value = (float)Math.Pow(p, 2f) * max + (1 - (float)Math.Pow(p, 2f)) * min;
                            backGroundColor = new Color(value, value, value, 1);
                        }
                    }

                    HudManager.Instance.FullScreen.color = backGroundColor;
                    GameObject.Find("BackgroundLayer")?.SetActive(false);

                    // enable pick, wait for pick
                    var youColor = timer - (int)timer > 0.5 ? Color.red : Color.yellow;
                    playerText = Helpers.cs(youColor, ModTranslation.GetString("RoleDraft-Text", 3));
                    // Available Roles:
                    List<RoleInfo> availableRoles = new();
                    foreach (var roleInfo in CustomRoleManager.Instance.allRoleInfos)
                    {
                        var impostorCount = PlayerControl.AllPlayerControls.ToArray().ToList()
                            .Where(x => x.Data.Role.IsImpostor).Count();
                        if (roleInfo.isModifier) continue;
                        // Remove Impostor Roles
                        if (PlayerControl.LocalPlayer.Data.Role.IsImpostor && !roleInfo.isImpostor) continue;
                        if (!PlayerControl.LocalPlayer.Data.Role.IsImpostor && roleInfo.isImpostor) continue;

                        var roleData = RoleManagerSelectRolesPatch.getRoleAssignmentData();
                        roleData.crewSettings.Add((byte)RoleId.Sheriff,
                            CustomOptionHolder.sheriffSpawnRate.getSelection());
                        if (CustomOptionHolder.sheriffSpawnRate.getSelection() > 0)
                            roleData.crewSettings.Add((byte)RoleId.Deputy,
                                CustomOptionHolder.deputySpawnRate.getSelection());
                        if (roleData.neutralSettings.ContainsKey((byte)roleInfo.roleId) &&
                            roleData.neutralSettings[(byte)roleInfo.roleId] == 0) continue;
                        if (roleData.impSettings.ContainsKey((byte)roleInfo.roleId) &&
                            roleData.impSettings[(byte)roleInfo.roleId] == 0) continue;
                        if (roleData.crewSettings.ContainsKey((byte)roleInfo.roleId) &&
                            roleData.crewSettings[(byte)roleInfo.roleId] == 0) continue;
                        if (new List<RoleId> { RoleId.Janitor, RoleId.Godfather, RoleId.Mafioso }.Contains(
                                roleInfo.roleId) && (CustomOptionHolder.mafiaSpawnRate.getSelection() == 0 ||
                                                     GameOptionsManager.Instance.currentGameOptions.NumImpostors < 3))
                            continue;
                        if (roleInfo.roleId == RoleId.Sidekick) continue;
                        if (roleInfo.roleId == RoleId.Deputy && Sheriff.sheriff == null) continue;
                        if (roleInfo.roleId == RoleId.Pursuer) continue;
                        if (roleInfo.roleId == RoleId.Spy && impostorCount < 2) continue;
                        if (roleInfo.roleId == RoleId.Prosecutor &&
                            (CustomOptionHolder.lawyerIsProsecutorChance.getSelection() == 0 ||
                             CustomOptionHolder.lawyerSpawnRate.getSelection() == 0)) continue;
                        if (roleInfo.roleId == RoleId.Lawyer &&
                            (CustomOptionHolder.lawyerIsProsecutorChance.getSelection() == 10 ||
                             CustomOptionHolder.lawyerSpawnRate.getSelection() == 0)) continue;
                        if (TORMapOptions.gameMode == CustomGamemodes.Guesser &&
                            (roleInfo.roleId == RoleId.EvilGuesser || roleInfo.roleId == RoleId.NiceGuesser)) continue;
                        if (alreadyPicked.Contains((byte)roleInfo.roleId) && roleInfo.roleId != RoleId.Crewmate)
                            continue;
                        if (CustomOptionHolder.crewmateRolesFill.getBool() && roleInfo.roleId == RoleId.Crewmate)
                            continue;

                        var impsPicked = alreadyPicked.Where(x => x != 0 && RoleInfo.roleInfoById.ContainsKey((RoleId)x) && RoleInfo.roleInfoById[(RoleId)x].isImpostor).Count();

                        // Handle forcing of 100% roles for impostors
                        if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
                        {
                            var impsMax = CustomOptionHolder.impostorRolesCountMax.getSelection();
                            var impsMin = CustomOptionHolder.impostorRolesCountMin.getSelection();
                            if (impsMin > impsMax) impsMin = impsMax;
                            var impsLeft = pickOrder.Where(x => Helpers.playerById(x).Data.Role.IsImpostor).Count();
                            var imps100 = roleData.impSettings.Where(x => x.Value == 10).Count();
                            if (imps100 > impsMax) imps100 = impsMax;
                            var imps100Picked = alreadyPicked.Where(x => x != 0 && roleData.impSettings.GetValueSafe(x) == 10)
                                .Count();
                            if (imps100 - imps100Picked >= impsLeft && !(roleData.impSettings
                                    .Where(x => x.Value == 10 && x.Key == (byte)roleInfo.roleId).Count() > 0)) continue;
                            if (impsMin - impsPicked >= impsLeft && roleInfo.roleId == RoleId.Impostor) continue;
                            if (impsPicked >= impsMax && roleInfo.roleId != RoleId.Impostor) continue;
                        }

                        // Player is no impostor! Handle forcing of 100% roles for crew and neutral
                        else
                        {
                            // No more neutrals possible!
                            var neutralsPicked = alreadyPicked.Where(x => x != 0 && RoleInfo.roleInfoById.ContainsKey((RoleId)x) && RoleInfo.roleInfoById[(RoleId)x].isNeutral)
                                .Count();
                            var crewPicked = alreadyPicked.Count - impsPicked - neutralsPicked;
                            var neutralsMax = CustomOptionHolder.neutralRolesCountMax.getSelection();
                            var neutralsMin = CustomOptionHolder.neutralRolesCountMin.getSelection();
                            var neutrals100 = roleData.neutralSettings.Where(x => x.Value == 10).Count();
                            if (neutrals100 > neutralsMin) neutralsMin = neutrals100;
                            if (neutralsMin > neutralsMax) neutralsMin = neutralsMax;

                            var crewLimit = PlayerControl.AllPlayerControls.Count - impostorCount -
                                            (neutralsMin > neutrals100 ? neutralsMin :
                                                neutrals100 > neutralsMax ? neutralsMax : neutrals100);
                            var maxCrew = CustomOptionHolder.crewmateRolesFill.getBool()
                                ? CustomOptionHolder.crewmateRolesCountMax.getSelection()
                                : crewLimit;
                            if (maxCrew > crewLimit)
                                maxCrew = crewLimit;
                            if (crewPicked >= crewLimit && !roleInfo.isNeutral && roleInfo.roleId != RoleId.Crewmate)
                                continue;
                            // Fill roles means no crewmates allowed!
                            if (CustomOptionHolder.crewmateRolesFill.getBool() && roleInfo.roleId == RoleId.Crewmate)
                                continue;

                            var allowAnyNeutral = false;
                            if (neutralsPicked >= neutralsMax && roleInfo.isNeutral) continue;
                            // More neutrals needed? Then no more crewmates! This takes precedence over crew roles set to 100%!
                            var crewmatesLeft = pickOrder.Count -
                                                pickOrder.Where(x => Helpers.playerById(x).Data.Role.IsImpostor)
                                                    .Count();

                            if (crewmatesLeft <= neutralsMin - neutralsPicked && !roleInfo.isNeutral) continue;

                            if (neutralsMin - neutrals100 > neutralsPicked)
                                allowAnyNeutral = true;
                            // Handle 100% Roles PER Faction.

                            var neutrals100Picked = alreadyPicked
                                .Where(x => x != 0 && roleData.neutralSettings.GetValueSafe(x) == 10).Count();
                            if (neutrals100 > neutralsMax) neutrals100 = neutralsMax;

                            var crew100 = roleData.crewSettings.Where(x => x.Value == 10).Count();
                            var crew100Picked = alreadyPicked.Where(x => x != 0 && roleData.crewSettings.GetValueSafe(x) == 10)
                                .Count();
                            if (neutrals100 > neutralsMax) neutrals100 = neutralsMax;

                            if (crew100 > maxCrew) crew100 = maxCrew;
                            if ((neutrals100 - neutrals100Picked >= crewmatesLeft || (roleInfo.isNeutral &&
                                    neutrals100 - neutrals100Picked >= neutralsMax - neutralsPicked)) &&
                                !(neutrals100Picked >= neutralsMax) &&
                                !(roleData.neutralSettings.Where(x => x.Value == 10 && x.Key == (byte)roleInfo.roleId)
                                    .Count() > 0)) continue;
                            if (!(allowAnyNeutral && roleInfo.isNeutral) && crew100 - crew100Picked >= crewmatesLeft &&
                                !(roleData.crewSettings.Where(x => x.Value == 10 && x.Key == (byte)roleInfo.roleId)
                                    .Count() > 0)) continue;

                            if (!(allowAnyNeutral && roleInfo.isNeutral) &&
                                neutrals100 + crew100 - neutrals100Picked - crew100Picked >= crewmatesLeft &&
                                !(roleData.crewSettings.Where(x => x.Value == 10 && x.Key == (byte)roleInfo.roleId)
                                      .Count() > 0 ||
                                  roleData.neutralSettings.Where(x => x.Value == 10 && x.Key == (byte)roleInfo.roleId)
                                      .Count() > 0)) continue;
                        }

                        // Handle role pairings that are blocked, e.g. Vampire Warlock, Cleaner Vulture etc.
                        var blocked = false;
                        foreach (var blockedRoleId in CustomOptionHolder.blockedRolePairings)
                            if (alreadyPicked.Contains(blockedRoleId.Key) &&
                                blockedRoleId.Value.ToList().Contains((byte)roleInfo.roleId))
                            {
                                blocked = true;
                                break;
                            }

                        if (blocked) continue;

                        availableRoles.Add(roleInfo);
                    }

                    // Fallback for if all roles are somehow removed.
                    if (availableRoles.Count == 0)
                    {
                        if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
                            availableRoles.Add(CustomRoleManager.impostor);
                        else
                            availableRoles.Add(CustomRoleManager.crewmate);
                        TheOtherRolesPlugin.Logger.LogWarning(
                            "Draft Mode: Fallback triggered, because no roles were left. Forced addition of basegame Imp/Crewmate");
                    }

                    List<RoleInfo> originalAvailable = new(availableRoles);

                    // remove some roles, so that you can't always get the same roles:
                    if (availableRoles.Count > CustomOptionHolder.draftModeAmountOfChoices.getFloat())
                    {
                        var countToRemove = availableRoles.Count -
                                            (int)CustomOptionHolder.draftModeAmountOfChoices.getFloat();
                        while (countToRemove-- > 0)
                        {
                            var toRemove = availableRoles.OrderBy(_ => Guid.NewGuid()).First();
                            availableRoles.Remove(toRemove);
                        }
                    }

                    if (timer >= maxTimer)
                        sendPick((byte)originalAvailable.OrderBy(_ => Guid.NewGuid()).First().roleId);

                    if (GameObject.Find("RoleButton") == null)
                    {
                        SoundEffectsManager.play("timemasterShield");
                        int i = 0;
                        int totalButtons = availableRoles.Count + 1; // +1 for Random
                        int buttonsPerRow = 4;
                        int lastRow = totalButtons / buttonsPerRow;
                        int buttonsInLastRow = totalButtons % buttonsPerRow;

                        foreach (RoleInfo roleInfo in availableRoles)
                        {
                            float row = i / buttonsPerRow;
                            float col = i % buttonsPerRow;
                            if (buttonsInLastRow != 0 && row == lastRow)
                                col += (buttonsPerRow - buttonsInLastRow) / 2f;
                            row += (4 - lastRow - 1) / 2f;

                            ActionButton actionButton = Object.Instantiate(HudManager.Instance.KillButton,
                                __instance.TeamTitle.transform);
                            actionButton.gameObject.SetActive(true);
                            actionButton.gameObject.name = "RoleButton";
                            actionButton.transform.localPosition = new Vector3(-8.4f + col * 5.5f, -10.2f - row * 3f);
                            actionButton.transform.localScale = new Vector3(2f, 2f);
                            actionButton.SetCoolDown(0, 0);

                            var textHolder = new GameObject("textHolder");
                            var text = textHolder.AddComponent<TextMeshPro>();
                            text.text = roleInfo.name.Replace(" ", "\n");
                            text.horizontalAlignment = HorizontalAlignmentOptions.Center;
                            text.fontSize = 5;
                            textHolder.layer = actionButton.gameObject.layer;
                            text.color = roleInfo.color;
                            textHolder.transform.SetParent(actionButton.transform, false);
                            textHolder.transform.localPosition = new Vector3(0,
                                text.text.Contains("\n") ? -1.975f : -2.2f, -1);

                            var button = actionButton.GetComponent<PassiveButton>();
                            button.OnClick = new Button.ButtonClickedEvent();
                            var capturedRole = roleInfo;
                            button.OnClick.AddListener((Action)(() => { sendPick((byte)capturedRole.roleId); }));
                            HudManager.Instance.StartCoroutine(Effects.Lerp(0.5f,
                                new Action<float>(p => { actionButton.OverrideText(""); })));
                            buttons.Add(actionButton);
                            i++;
                        }

                        // Random button
                        {
                            float row = i / buttonsPerRow;
                            float col = i % buttonsPerRow;
                            if (buttonsInLastRow != 0 && row == lastRow)
                                col += (buttonsPerRow - buttonsInLastRow) / 2f;
                            row += (4 - lastRow - 1) / 2f;

                            ActionButton randomButton = Object.Instantiate(HudManager.Instance.KillButton,
                                __instance.TeamTitle.transform);
                            randomButton.gameObject.SetActive(true);
                            randomButton.gameObject.name = "RandomButton";
                            randomButton.transform.localPosition = new Vector3(-8.4f + col * 5.5f, -10.2f - row * 3f);
                            randomButton.transform.localScale = new Vector3(2f, 2f);
                            randomButton.SetCoolDown(0, 0);
                            randomButton.buttonLabelText.gameObject.SetActive(false);

                            var randomTextHolder = new GameObject("randomTextHolder");
                            var randomText = randomTextHolder.AddComponent<TextMeshPro>();
                            randomText.text = $"<b>{ModTranslation.GetString("RoleDraft-Text", 4)}</b>";
                            randomText.horizontalAlignment = HorizontalAlignmentOptions.Center;
                            randomText.fontSize = 5;
                            randomTextHolder.layer = randomButton.gameObject.layer;
                            randomText.color = Color.green;
                            randomTextHolder.transform.SetParent(randomButton.transform, false);
                            randomTextHolder.transform.localPosition = new Vector3(0, -2.2f, -1);

                            var randomPassiveButton = randomButton.GetComponent<PassiveButton>();
                            randomPassiveButton.OnClick = new Button.ButtonClickedEvent();
                            var capturedRoles = new List<RoleInfo>(availableRoles);
                            randomPassiveButton.OnClick.AddListener((Action)(() =>
                            {
                                var randomRole = capturedRoles.OrderBy(_ => Guid.NewGuid()).First();
                                sendPick((byte)randomRole.roleId);
                            }));
                            HudManager.Instance.StartCoroutine(Effects.Lerp(0.5f,
                                new Action<float>(p => { randomButton.OverrideText(""); })));
                            buttons.Add(randomButton);
                        }
                    }
                }
                else
                {
                    var currentPick = PlayerControl.AllPlayerControls.Count - pickOrder.Count + 1;
                    playerText = string.Format(ModTranslation.GetString("RoleDraft-Text", 5), currentPick);
                    HudManager.Instance.FullScreen.color = Color.black;
                }

                __instance.TeamTitle.text =
                    $"{Helpers.cs(Color.white, $"<size=280%>{ModTranslation.GetString("RoleDraft-Text", 6)}</size>")}\n\n\n<size=200%> {ModTranslation.GetString("RoleDraft-Text", 2)}</size>\n\n\n<size=250%>{playerText}</size>";
                var waitMore = pickOrder.IndexOf(PlayerControl.LocalPlayer.PlayerId);
                var waitMoreText = "";
                if (waitMore > 0) waitMoreText = " " + string.Format(ModTranslation.GetString("RoleDraft-Text", 7), waitMore);
                __instance.TeamTitle.text +=
                    $"\n\n{waitMoreText}\n{ModTranslation.GetString("RoleDraft-Text", 8)} {(int)(maxTimer + 1 - timer)}\n {(SoundManager.MusicVolume > -80 ? "♫ Music: Ultimate Superhero 3 - Kenët & Rez ♫" : "")}";
                yield return null;
            }
        }

        HudManager.Instance.FullScreen.color = Color.black;
        __instance.FrontMost.gameObject.SetActive(true);
        GameObject.Find("BackgroundLayer")?.SetActive(true);
        if (AmongUsClient.Instance.AmHost)
        {
            RoleManagerSelectRolesPatch.assignRoleTargets(null); // Assign targets for Lawyer & Prosecutor
            if (RoleManagerSelectRolesPatch.isGuesserGamemode) RoleManagerSelectRolesPatch.assignGuesserGamemode();
            RoleManagerSelectRolesPatch.assignModifiers(); // Assign modifier
        }

        var myTimer = 0f;
        while (myTimer < 3f)
        {
            myTimer += Time.deltaTime;
            var c = new Color(0, 0, 0, myTimer / 3.0f);
            __instance.FrontMost.color = c;
            yield return null;
        }

        foreach (var button in buttons) if (button != null) Object.Destroy(button.gameObject);
        buttons.Clear();
        if (feedText != null) Object.Destroy(feedText.gameObject);
        feedText = null;

        SoundEffectsManager.stop("draft");
        isRunning = false;

        // Restore chat state after draft
        if (canChat && !(PlayerControl.LocalPlayer.isLover() && Lovers.enableChat))
            HudManager.Instance.Chat.SetVisible(false);
    }

    private static IEnumerator CoShowRandomOrderAnimation(IntroCutscene __instance)
    {
        var titleText = Object.Instantiate(__instance.TeamTitle, __instance.transform);
        titleText.text = $"<color=red>{ModTranslation.GetString("RoleDraft-Text", 13)}</color>";
        titleText.transform.localPosition = new Vector3(0, 2f, -10f);
        titleText.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
        titleText.alignment = TextAlignmentOptions.Center;

        var numberText = Object.Instantiate(__instance.TeamTitle, __instance.transform);
        numberText.text = "0";
        numberText.transform.localPosition = new Vector3(0, 0.5f, -10f);
        numberText.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
        numberText.alignment = TextAlignmentOptions.Center;
        numberText.color = Color.white;

        while (pickOrder.Count == 0)
        {
            yield return null;
        }

        int playerIndex = pickOrder.IndexOf(PlayerControl.LocalPlayer.PlayerId);
        int playerNumber = playerIndex + 1;

        float animationTime = 3.5f;
        float animTimer = 0f;
        System.Random random = new System.Random();
        float lastChangeTime = 0f;
        float changeInterval = 0.15f;

        while (animTimer < animationTime)
        {
            animTimer += Time.deltaTime;

            if (animTimer < animationTime - 1.0f)
            {
                if (animTimer - lastChangeTime > changeInterval)
                {
                    lastChangeTime = animTimer;
                    int randomNumber = random.Next(1, PlayerControl.AllPlayerControls.Count + 1);
                    numberText.text = randomNumber.ToString();
                }
            }
            else
            {
                float progress = (animTimer - (animationTime - 1.0f)) / 1.0f;
                if (progress > 0.7f)
                {
                    numberText.text = playerNumber.ToString();
                }
                else if (progress > 0.3f)
                {
                    if (animTimer - lastChangeTime > changeInterval * 2f)
                    {
                        lastChangeTime = animTimer;
                        int randomNumber = random.Next(
                            Math.Max(1, playerNumber - 2),
                            Math.Min(PlayerControl.AllPlayerControls.Count + 1, playerNumber + 3));
                        numberText.text = randomNumber.ToString();
                    }
                }
            }

            yield return null;
        }

        numberText.text = playerNumber.ToString();
        titleText.text = $"<color=red>{ModTranslation.GetString("RoleDraft-Text", 14)}</color>";

        yield return new WaitForSeconds(2f);

        titleText.gameObject.Destroy();
        numberText.gameObject.Destroy();
    }

    public static void receivePick(byte playerId, byte roleId)
    {
        if (!isEnabled) return;
        RPCProcedure.setRole(roleId, playerId);
        try
        {
            pickOrder.Remove(playerId);
            timer = 0;
            picked = true;
            var roleInfo = CustomRoleManager.Instance.allRoleInfos.First(x => (byte)x.roleId == roleId);
            var roleString = Helpers.cs(roleInfo.color, roleInfo.name);
            if (!CustomOptionHolder.draftModeShowRoles.getBool() && !(playerId == PlayerControl.LocalPlayer.PlayerId))
            {
                roleString = ModTranslation.GetString("RoleDraft-Text", 9);
            }
            else if (CustomOptionHolder.draftModeHideImpRoles.getBool() && roleInfo.isImpostor &&
                     !(playerId == PlayerControl.LocalPlayer.PlayerId))
            {
                roleString = Helpers.cs(Palette.ImpostorRed, ModTranslation.GetString("RoleDraft-Text", 10));
            }
            else if (CustomOptionHolder.draftModeHideNeutralRoles.getBool() && roleInfo.isNeutral &&
                     !(playerId == PlayerControl.LocalPlayer.PlayerId))
            {
                roleString = Helpers.cs(Palette.Blue, ModTranslation.GetString("RoleDraft-Text", 11));
            }

            var line = $"{(playerId == PlayerControl.LocalPlayer.PlayerId ? ModTranslation.GetString("RoleDraft-Text", 12) : alreadyPicked.Count)}:";
            line = line + string.Concat(Enumerable.Repeat(" ", 6 - line.Length)) + roleString;
            feedText.text += line + "\n";
            SoundEffectsManager.play("select");
        }
        catch (Exception e)
        {
            TheOtherRolesPlugin.Logger.LogError(e);
        }
    }

    public static void sendPick(byte RoleId)
    {
        SoundEffectsManager.stop("timeMasterShield");
        var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
            (byte)CustomRPC.DraftModePick, SendOption.Reliable);
        writer.Write(PlayerControl.LocalPlayer.PlayerId);
        writer.Write(RoleId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        receivePick(PlayerControl.LocalPlayer.PlayerId, RoleId);

        // destroy all the buttons:
        foreach (var button in buttons) button?.gameObject?.Destroy();
        buttons.Clear();
    }


    public static void sendPickOrder()
    {
        pickOrder = PlayerControl.AllPlayerControls.ToArray().Select(x => x.PlayerId).OrderBy(_ => Guid.NewGuid())
            .ToList();
        var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
            (byte)CustomRPC.DraftModePickOrder, SendOption.Reliable);
        writer.Write((byte)pickOrder.Count);
        foreach (var item in pickOrder) writer.Write(item);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }


    public static void receivePickOrder(int amount, MessageReader reader)
    {
        pickOrder.Clear();
        for (var i = 0; i < amount; i++) pickOrder.Add(reader.ReadByte());
    }
}
