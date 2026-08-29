using System;
using System.Collections.Generic;
using System.Linq;
using TheOtherRoles.Patches;
using TheOtherRoles.Utilities;
using UnityEngine;
using static TheOtherRoles.TheOtherRoles;

namespace TheOtherRoles.Roles.Crewmate;

public class Medium : RoleBase
{
    public static Medium Instance;

    public static Color color = new Color32(98, 120, 115, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.Medium);

    public static PlayerControl medium;
    public static DeadPlayer target;
    public static DeadPlayer soulTarget;
    public static List<Tuple<DeadPlayer, Vector3>> deadBodies = new();
    public static List<Tuple<DeadPlayer, Vector3>> futureDeadBodies = new();
    public static List<SpriteRenderer> souls = new();
    public static DateTime meetingStartTime = DateTime.UtcNow;

    public static float cooldown = 30f;
    public static float duration = 3f;
    public static bool oneTimeUse;
    public static float chanceAdditionalInfo;

    private static Sprite soulSprite;

    private static Sprite question;

    public Medium()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Crewmate;
    }

    public static Sprite getSoulSprite()
    {
        if (soulSprite) return soulSprite;
        soulSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Soul.png", 500f);
        return soulSprite;
    }

    public static Sprite getQuestionSprite()
    {
        if (question) return question;
        question = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.MediumButton.png", 115f);
        return question;
    }

    public static void clearAndReload()
    {
        medium = null;
        target = null;
        soulTarget = null;
        deadBodies = new List<Tuple<DeadPlayer, Vector3>>();
        futureDeadBodies = new List<Tuple<DeadPlayer, Vector3>>();
        souls = new List<SpriteRenderer>();
        meetingStartTime = DateTime.UtcNow;
        cooldown = CustomOptionHolder.mediumCooldown.getFloat();
        duration = CustomOptionHolder.mediumDuration.getFloat();
        oneTimeUse = CustomOptionHolder.mediumOneTimeUse.getBool();
        chanceAdditionalInfo = CustomOptionHolder.mediumChanceAdditionalInfo.getSelection() / 10f;
    }

    public static string getInfo(PlayerControl target, PlayerControl killer, DeadPlayer.CustomDeathReason deathReason)
    {
        string msg = "";

        List<SpecialMediumInfo> infos = new List<SpecialMediumInfo>();
        // collect fitting death info types.
        // suicides:
        if (killer == target)
        {
            if ((target == Sheriff.sheriff || target == Sheriff.formerSheriff) && deathReason != DeadPlayer.CustomDeathReason.LoverSuicide) infos.Add(SpecialMediumInfo.SheriffSuicide); if (target == Lovers.lover1 || target == Lovers.lover2) infos.Add(SpecialMediumInfo.PassiveLoverSuicide);
            if (target == Lovers.lover1 || target == Lovers.lover2) infos.Add(SpecialMediumInfo.PassiveLoverSuicide);
            if (target == Thief.thief && deathReason != DeadPlayer.CustomDeathReason.LoverSuicide) infos.Add(SpecialMediumInfo.ThiefSuicide);
            if (target == Warlock.warlock && deathReason != DeadPlayer.CustomDeathReason.LoverSuicide) infos.Add(SpecialMediumInfo.WarlockSuicide);
        }
        else
        {
            if (target == Lovers.lover1 || target == Lovers.lover2) infos.Add(SpecialMediumInfo.ActiveLoverDies);
            if (target.Data.Role.IsImpostor && killer.Data.Role.IsImpostor && Thief.formerThief != killer) infos.Add(SpecialMediumInfo.ImpostorTeamkill);
        }
        if (target == Sidekick.sidekick && (killer == Jackal.jackal || Jackal.formerJackals.Any(x => x.PlayerId == killer.PlayerId))) infos.Add(SpecialMediumInfo.JackalKillsSidekick);
        if (target == Lawyer.lawyer && killer == Lawyer.target) infos.Add(SpecialMediumInfo.LawyerKilledByClient);
        if (Medium.target.wasCleaned) infos.Add(SpecialMediumInfo.BodyCleaned);

        if (infos.Count > 0)
        {
            var selectedInfo = infos[rnd.Next(infos.Count)];
            switch (selectedInfo)
            {
                case SpecialMediumInfo.SheriffSuicide:
                    msg = ModTranslation.GetString("Opt-Medium", 5);
                    break;
                case SpecialMediumInfo.WarlockSuicide:
                    msg = ModTranslation.GetString("Opt-Medium", 6);
                    break;
                case SpecialMediumInfo.ThiefSuicide:
                    msg = ModTranslation.GetString("Opt-Medium", 7);
                    break;
                case SpecialMediumInfo.ActiveLoverDies:
                    msg = ModTranslation.GetString("Opt-Medium", 8);
                    break;
                case SpecialMediumInfo.PassiveLoverSuicide:
                    msg = ModTranslation.GetString("Opt-Medium", 9);
                    break;
                case SpecialMediumInfo.LawyerKilledByClient:
                    msg = ModTranslation.GetString("Opt-Medium", 10);
                    break;
                case SpecialMediumInfo.JackalKillsSidekick:
                    msg = ModTranslation.GetString("Opt-Medium", 11);
                    break;
                case SpecialMediumInfo.ImpostorTeamkill:
                    msg = ModTranslation.GetString("Opt-Medium", 12);
                    break;
                case SpecialMediumInfo.BodyCleaned:
                    msg = ModTranslation.GetString("Opt-Medium", 13);
                    break;
            }
        }
        else
        {
            var randomNumber = rnd.Next(4);
            var typeOfColor = Helpers.isLighterColor(Medium.target.killerIfExisting) ? ModTranslation.GetString("Opt-Medium", 18) : ModTranslation.GetString("Opt-Medium", 19);
            var timeSinceDeath = (float)(meetingStartTime - Medium.target.timeOfDeath).TotalMilliseconds;
            var roleString = CustomRoleManager.GetRolesString(Medium.target.player, false);
            if (randomNumber == 0)
            {
                if (!roleString.Contains(ModTranslation.GetRoleName(RoleId.Impostor).GetString()) && !roleString.Contains(ModTranslation.GetRoleName(RoleId.Crewmate).GetString()))
                    msg = string.Format(ModTranslation.GetString("Opt-Medium", 14), roleString);
                else
                    msg = string.Format(ModTranslation.GetString("Opt-Medium", 15), typeOfColor);
            }
            else if (randomNumber == 1) msg = string.Format(ModTranslation.GetString("Opt-Medium", 20), typeOfColor);
            else if (randomNumber == 2) msg = string.Format(ModTranslation.GetString("Opt-Medium", 16), Math.Round(timeSinceDeath / 1000));
            else msg = string.Format(ModTranslation.GetString("Opt-Medium", 17), CustomRoleManager.GetRolesString(Medium.target.killerIfExisting, false, false, true));
        }

        if (rnd.NextDouble() < chanceAdditionalInfo)
        {
            var count = 0;
            var alivePlayersList = PlayerControl.AllPlayerControls.ToArray().Where(pc => !pc.Data.IsDead);
            string msgTemplate = "";
            switch (rnd.Next(3))
            {
                case 0:
                    count = alivePlayersList.Where(pc =>
                        pc.Data.Role.IsImpostor ||
                        new List<RoleInfo> { Jackal.Info, Sidekick.Info, Sheriff.Info, Thief.Info }.Contains(
                            CustomRoleManager.getRoleInfoForPlayer(pc, false).FirstOrDefault())).Count();
                    msgTemplate = count == 1
                        ? ModTranslation.GetString("Opt-Medium", 21)
                        : ModTranslation.GetString("Opt-Medium", 22);
                    break;
                case 1:
                    count = alivePlayersList.Where(Helpers.roleCanUseVents).Count();
                    msgTemplate = count == 1
                        ? ModTranslation.GetString("Opt-Medium", 23)
                        : ModTranslation.GetString("Opt-Medium", 24);
                    break;
                case 2:
                    count = alivePlayersList.Where(pc =>
                            Helpers.isNeutral(pc) && pc != Jackal.jackal && pc != Sidekick.sidekick &&
                            pc != Thief.thief)
                        .Count();
                    msgTemplate = count == 1
                        ? ModTranslation.GetString("Opt-Medium", 25)
                        : ModTranslation.GetString("Opt-Medium", 26);
                    break;
            }
            msg += "\n" + string.Format(msgTemplate, count);
        }

        return string.Format(ModTranslation.GetString("Opt-Medium", 27) + "\n", Medium.target.player.Data.PlayerName) + msg;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    public override RoleInfo GetRoleInfo()
    {
        return Info;
    }

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (Medium.medium == null || Medium.medium != player || Medium.medium.Data.IsDead ||
            Medium.deadBodies == null || MapUtilities.CachedShipStatus?.AllVents == null) return;
        DeadPlayer target = null;
        var truePosition = player.GetTruePosition();
        var closestDistance = float.MaxValue;
        var usableDistance = MapUtilities.CachedShipStatus.AllVents.FirstOrDefault().UsableDistance;
        foreach (var (dp, ps) in Medium.deadBodies)
        {
            var distance = Vector2.Distance(ps, truePosition);
            if (distance <= usableDistance && distance < closestDistance)
            {
                closestDistance = distance;
                target = dp;
            }
        }
        Medium.target = target;
    }

    private enum SpecialMediumInfo
    {
        SheriffSuicide,
        ThiefSuicide,
        ActiveLoverDies,
        PassiveLoverSuicide,
        LawyerKilledByClient,
        JackalKillsSidekick,
        ImpostorTeamkill,
        SubmergedO2,
        WarlockSuicide,
        BodyCleaned
    }
}