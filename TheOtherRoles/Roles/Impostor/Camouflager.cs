using UnityEngine;

namespace TheOtherRoles.Roles.Impostor;

public class Camouflager : RoleBase
{
    public static Camouflager Instance;

    public static Color color = Palette.ImpostorRed;

    public static RoleInfo Info = new(color, RoleId.Camouflager);

    public static PlayerControl camouflager;
    public static float cooldown = 30f;
    public static float duration = 10f;
    public static float camouflageTimer;

    private static Sprite buttonSprite;

    public Camouflager()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Impostor;
    }

    public static Sprite getButtonSprite()
    {
        if (buttonSprite) return buttonSprite;
        buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.CamoButton.png", 115f);
        return buttonSprite;
    }

    public static void resetCamouflage()
    {
        camouflageTimer = 0f;
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p == Ninja.ninja && Ninja.isInvisble)
                continue;
            p.setDefaultLook();
        }
    }

    public static void clearAndReload()
    {
        resetCamouflage();
        camouflager = null;
        camouflageTimer = 0f;
        cooldown = CustomOptionHolder.camouflagerCooldown.getFloat();
        duration = CustomOptionHolder.camouflagerDuration.getFloat();
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