using System.Collections.Generic;
using Hazel;
using TheOtherRoles.Objects;
using TheOtherRoles.Patches;
using TheOtherRoles.Utilities;
using TMPro;
using UnityEngine;

namespace TheOtherRoles.Roles.Impostor;

public class BountyHunter : RoleBase
{
    public static BountyHunter Instance;

    public static Color color = Palette.ImpostorRed;

    public static RoleInfo Info = new(color, RoleId.BountyHunter);

    public static PlayerControl bountyHunter;
    public static Arrow arrow;
    public static float bountyDuration = 30f;
    public static bool showArrow = true;
    public static float bountyKillCooldown;
    public static float punishmentTime = 15f;
    public static float arrowUpdateIntervall = 10f;

    public static float arrowUpdateTimer;
    public static float bountyUpdateTimer;
    public static PlayerControl bounty;
    public static TextMeshPro cooldownText;

    public BountyHunter()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Impostor;
    }

    public static void clearAndReload()
    {
        arrow = new Arrow(color);
        bountyHunter = null;
        bounty = null;
        arrowUpdateTimer = 0f;
        bountyUpdateTimer = 0f;
        if (arrow != null && arrow.arrow != null) Object.Destroy(arrow.arrow);
        arrow = null;
        if (cooldownText != null && cooldownText.gameObject != null) Object.Destroy(cooldownText.gameObject);
        cooldownText = null;
        foreach (var p in TORMapOptions.playerIcons.Values)
            if (p != null && p.gameObject != null)
                p.gameObject.SetActive(false);

        bountyDuration = CustomOptionHolder.bountyHunterBountyDuration.getFloat();
        bountyKillCooldown = CustomOptionHolder.bountyHunterReducedCooldown.getFloat();
        punishmentTime = CustomOptionHolder.bountyHunterPunishmentTime.getFloat();
        showArrow = CustomOptionHolder.bountyHunterShowArrow.getBool();
        arrowUpdateIntervall = CustomOptionHolder.bountyHunterArrowUpdateIntervall.getFloat();
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
        if (BountyHunter.bountyHunter == null || player != BountyHunter.bountyHunter) return;
        if (BountyHunter.bountyHunter.Data.IsDead)
        {
            if (BountyHunter.arrow != null || BountyHunter.arrow.arrow != null)
                Object.Destroy(BountyHunter.arrow.arrow);
            BountyHunter.arrow = null;
            if (BountyHunter.cooldownText != null && BountyHunter.cooldownText.gameObject != null)
                Object.Destroy(BountyHunter.cooldownText.gameObject);
            BountyHunter.cooldownText = null;
            BountyHunter.bounty = null;
            foreach (var p in TORMapOptions.playerIcons.Values)
                if (p != null && p.gameObject != null) p.gameObject.SetActive(false);
            return;
        }
        BountyHunter.arrowUpdateTimer -= Time.fixedDeltaTime;
        BountyHunter.bountyUpdateTimer -= Time.fixedDeltaTime;
        if (BountyHunter.bounty == null || BountyHunter.bountyUpdateTimer <= 0f)
        {
            BountyHunter.bounty = null;
            BountyHunter.arrowUpdateTimer = 0f;
            BountyHunter.bountyUpdateTimer = BountyHunter.bountyDuration;
            var possibleTargets = new List<PlayerControl>();
            foreach (var p in PlayerControl.AllPlayerControls)
                if (!p.Data.IsDead && !p.Data.Disconnected && p != p.Data.Role.IsImpostor && p != Spy.spy &&
                    (p != Sidekick.sidekick || !Sidekick.wasTeamRed) && (p != Jackal.jackal || !Jackal.wasTeamRed) &&
                    (p != Mini.mini || Mini.isGrownUp()) && (Lovers.getPartner(BountyHunter.bountyHunter) == null ||
                                                             p != Lovers.getPartner(BountyHunter.bountyHunter)))
                    possibleTargets.Add(p);
            BountyHunter.bounty = possibleTargets[TheOtherRoles.rnd.Next(0, possibleTargets.Count)];
            if (BountyHunter.bounty == null) return;
            var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                (byte)CustomRPC.ShareGhostInfo, SendOption.Reliable);
            writer.Write(PlayerControl.LocalPlayer.PlayerId);
            writer.Write((byte)RPCProcedure.GhostInfoTypes.BountyTarget);
            writer.Write(BountyHunter.bounty.PlayerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            if (FastDestroyableSingleton<HudManager>.Instance != null &&
                FastDestroyableSingleton<HudManager>.Instance.UseButton != null)
            {
                foreach (var pp in TORMapOptions.playerIcons.Values) pp.gameObject.SetActive(false);
                if (TORMapOptions.playerIcons.ContainsKey(BountyHunter.bounty.PlayerId) &&
                    TORMapOptions.playerIcons[BountyHunter.bounty.PlayerId].gameObject != null)
                    TORMapOptions.playerIcons[BountyHunter.bounty.PlayerId].gameObject.SetActive(true);
            }
        }
        if (MeetingHud.Instance && TORMapOptions.playerIcons.ContainsKey(BountyHunter.bounty?.PlayerId ?? byte.MaxValue) &&
            BountyHunter.bounty != null && TORMapOptions.playerIcons[BountyHunter.bounty.PlayerId].gameObject != null)
            TORMapOptions.playerIcons[BountyHunter.bounty.PlayerId].gameObject.SetActive(false);
        if (BountyHunter.cooldownText != null)
        {
            BountyHunter.cooldownText.text = Mathf
                .CeilToInt(Mathf.Clamp(BountyHunter.bountyUpdateTimer, 0, BountyHunter.bountyDuration)).ToString();
            BountyHunter.cooldownText.gameObject.SetActive(!MeetingHud.Instance);
        }
        if (BountyHunter.showArrow && BountyHunter.bounty != null)
        {
            if (BountyHunter.arrow == null) BountyHunter.arrow = new Arrow(Color.red);
            if (BountyHunter.arrowUpdateTimer <= 0f)
            {
                BountyHunter.arrow.Update(BountyHunter.bounty.transform.position);
                BountyHunter.arrowUpdateTimer = BountyHunter.arrowUpdateIntervall;
            }
            BountyHunter.arrow.Update();
        }
    }
}