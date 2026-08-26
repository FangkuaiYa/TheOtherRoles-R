using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TheOtherRoles.Roles.Neutral;

public class Jackal : RoleBase
{
    public static Jackal Instance;

    public static Color color = new Color32(0, 180, 235, byte.MaxValue);

    public static RoleInfo Info = new("Jackal", color,
        "Kill all Crewmates and <color=#FF1919FF>Impostors</color> to win", "Kill everyone", RoleId.Jackal, true);

    public static PlayerControl jackal;
    public static PlayerControl fakeSidekick;
    public static PlayerControl currentTarget;
    public static List<PlayerControl> formerJackals = new();

    public static float cooldown = 30f;
    public static float createSidekickCooldown = 30f;
    public static bool canUseVents = true;
    public static bool canCreateSidekick = true;
    public static Sprite buttonSprite;
    public static bool jackalPromotedFromSidekickCanCreateSidekick = true;
    public static bool canCreateSidekickFromImpostor = true;
    public static bool hasImpostorVision;
    public static bool wasTeamRed;
    public static bool wasImpostor;
    public static bool wasSpy;
    public static bool canSabotageLights;

    public Jackal()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Neutral;
    }

    public static Sprite getSidekickButtonSprite()
    {
        if (buttonSprite) return buttonSprite;
        buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.SidekickButton.png", 115f);
        return buttonSprite;
    }

    public static void removeCurrentJackal()
    {
        if (!formerJackals.Any(x => x.PlayerId == jackal.PlayerId)) formerJackals.Add(jackal);
        jackal = null;
        currentTarget = null;
        fakeSidekick = null;
        cooldown = CustomOptionHolder.jackalKillCooldown.getFloat();
        createSidekickCooldown = CustomOptionHolder.jackalCreateSidekickCooldown.getFloat();
    }

    public static void clearAndReload()
    {
        jackal = null;
        currentTarget = null;
        fakeSidekick = null;
        cooldown = CustomOptionHolder.jackalKillCooldown.getFloat();
        createSidekickCooldown = CustomOptionHolder.jackalCreateSidekickCooldown.getFloat();
        canUseVents = CustomOptionHolder.jackalCanUseVents.getBool();
        canCreateSidekick = CustomOptionHolder.jackalCanCreateSidekick.getBool();
        jackalPromotedFromSidekickCanCreateSidekick =
            CustomOptionHolder.jackalPromotedFromSidekickCanCreateSidekick.getBool();
        canCreateSidekickFromImpostor = CustomOptionHolder.jackalCanCreateSidekickFromImpostor.getBool();
        formerJackals.Clear();
        hasImpostorVision = CustomOptionHolder.jackalAndSidekickHaveImpostorVision.getBool();
        wasTeamRed = wasImpostor = wasSpy = false;
        canSabotageLights = CustomOptionHolder.jackalCanSabotageLights.getBool();
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    public override RoleInfo GetRoleInfo()
    {
        return Info;
    }
}