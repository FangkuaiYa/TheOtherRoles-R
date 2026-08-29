using System.Collections.Generic;
using Hazel;
using TheOtherRoles.Patches;
using UnityEngine;

namespace TheOtherRoles.Roles.Neutral;

public class Sidekick : RoleBase
{
    public static Sidekick Instance;

    public static Color color = new Color32(0, 180, 235, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.Sidekick, isNeutral: true);

    public static PlayerControl sidekick;

    public static PlayerControl currentTarget;

    public static bool wasTeamRed;
    public static bool wasImpostor;
    public static bool wasSpy;

    public static float cooldown = 30f;
    public static bool canUseVents = true;
    public static bool canKill = true;
    public static bool promotesToJackal = true;
    public static bool hasImpostorVision;
    public static bool canSabotageLights;

    public Sidekick()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Neutral;
    }

    public static void clearAndReload()
    {
        sidekick = null;
        currentTarget = null;
        cooldown = CustomOptionHolder.jackalKillCooldown.getFloat();
        canUseVents = CustomOptionHolder.sidekickCanUseVents.getBool();
        canKill = CustomOptionHolder.sidekickCanKill.getBool();
        promotesToJackal = CustomOptionHolder.sidekickPromotesToJackal.getBool();
        hasImpostorVision = CustomOptionHolder.jackalAndSidekickHaveImpostorVision.getBool();
        wasTeamRed = wasImpostor = wasSpy = false;
        canSabotageLights = CustomOptionHolder.sidekickCanSabotageLights.getBool();
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
        if (Sidekick.sidekick == null || Sidekick.sidekick != player) return;
        var untargetablePlayers = new List<PlayerControl>();
        if (Jackal.jackal != null) untargetablePlayers.Add(Jackal.jackal);
        if (Mini.mini != null && !Mini.isGrownUp())
            untargetablePlayers.Add(Mini.mini);
        Sidekick.currentTarget = PlayerControlFixedUpdatePatch.setTarget(untargetablePlayers: untargetablePlayers);
        if (Sidekick.canKill) PlayerControlFixedUpdatePatch.setPlayerOutline(Sidekick.currentTarget, Palette.ImpostorRed);

        // sidekickCheckPromotion
        if (Sidekick.sidekick.Data.IsDead || !Sidekick.promotesToJackal) return;
        if (Jackal.jackal == null || Jackal.jackal?.Data?.Disconnected == true)
        {
            var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                (byte)CustomRPC.SidekickPromotes, SendOption.Reliable);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCProcedure.sidekickPromotes();
        }
    }
}