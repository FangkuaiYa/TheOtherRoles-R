using UnityEngine;

namespace TheOtherRoles.Roles.Impostor;

public class Warlock : RoleBase
{
    public static Warlock Instance;

    public static Color color = Palette.ImpostorRed;

    public static RoleInfo Info = new("Warlock", color, "Curse other players and kill everyone",
        "Curse and kill everyone", RoleId.Warlock);

    public static PlayerControl warlock;
    public static PlayerControl currentTarget;
    public static PlayerControl curseVictim;
    public static PlayerControl curseVictimTarget;
    public static float cooldown = 30f;
    public static float rootTime = 5f;

    private static Sprite curseButtonSprite;
    private static Sprite curseKillButtonSprite;

    public Warlock()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Impostor;
    }

    public static Sprite getCurseButtonSprite()
    {
        if (curseButtonSprite) return curseButtonSprite;
        curseButtonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.CurseButton.png", 115f);
        return curseButtonSprite;
    }

    public static Sprite getCurseKillButtonSprite()
    {
        if (curseKillButtonSprite) return curseKillButtonSprite;
        curseKillButtonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.CurseKillButton.png", 115f);
        return curseKillButtonSprite;
    }

    public static void clearAndReload()
    {
        warlock = null;
        currentTarget = null;
        curseVictim = null;
        curseVictimTarget = null;
        cooldown = CustomOptionHolder.warlockCooldown.getFloat();
        rootTime = CustomOptionHolder.warlockRootTime.getFloat();
    }

    public static void resetCurse()
    {
        HudManagerStartPatch.warlockCurseButton.Timer = HudManagerStartPatch.warlockCurseButton.MaxTimer;
        HudManagerStartPatch.warlockCurseButton.Sprite = getCurseButtonSprite();
        HudManagerStartPatch.warlockCurseButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
        currentTarget = null;
        curseVictim = null;
        curseVictimTarget = null;
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