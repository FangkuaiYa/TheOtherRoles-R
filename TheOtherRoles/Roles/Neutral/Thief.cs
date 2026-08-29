using System.Collections.Generic;
using TheOtherRoles.Patches;
using UnityEngine;

namespace TheOtherRoles.Roles.Neutral;

public class Thief : RoleBase
{
    public static Thief Instance;

    public static Color color = new Color32(71, 99, 45, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.Thief, isNeutral: true);

    public static PlayerControl thief;
    public static PlayerControl currentTarget;
    public static PlayerControl formerThief;

    public static float cooldown = 30f;

    public static bool suicideFlag; // Used as a flag for suicide

    public static bool hasImpostorVision;
    public static bool canUseVents;
    public static bool canKillSheriff;
    public static bool canStealWithGuess;

    public Thief()
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
        thief = null;
        suicideFlag = false;
        currentTarget = null;
        formerThief = null;
        hasImpostorVision = CustomOptionHolder.thiefHasImpVision.getBool();
        cooldown = CustomOptionHolder.thiefCooldown.getFloat();
        canUseVents = CustomOptionHolder.thiefCanUseVents.getBool();
        canKillSheriff = CustomOptionHolder.thiefCanKillSheriff.getBool();
        canStealWithGuess = CustomOptionHolder.thiefCanStealWithGuess.getBool();
    }

    public static bool isFailedThiefKill(PlayerControl target, PlayerControl killer, RoleInfo targetRole)
    {
        return killer == thief && !target.Data.Role.IsImpostor && !new List<RoleInfo>
            { Jackal.Info, canKillSheriff ? Sheriff.Info : null, Sidekick.Info }.Contains(targetRole);
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
        if (Thief.thief == null || Thief.thief != player) return;
        var untargetables = new List<PlayerControl>();
        if (Mini.mini != null && !Mini.isGrownUp()) untargetables.Add(Mini.mini);
        Thief.currentTarget = PlayerControlFixedUpdatePatch.setTarget(untargetablePlayers: untargetables);
        PlayerControlFixedUpdatePatch.setPlayerOutline(Thief.currentTarget, Thief.color);
    }
}