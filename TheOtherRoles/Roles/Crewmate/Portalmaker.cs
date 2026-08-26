using TheOtherRoles.Utilities;
using UnityEngine;

namespace TheOtherRoles.Roles.Crewmate;

public class Portalmaker : RoleBase
{
    public static Portalmaker Instance;

    public static Color color = new Color32(69, 69, 169, byte.MaxValue);

    public static RoleInfo Info = new("Portalmaker", color, "You can create portals", "You can create portals",
        RoleId.Portalmaker);

    public static PlayerControl portalmaker;

    public static float cooldown;
    public static float usePortalCooldown;
    public static bool logOnlyHasColors;
    public static bool logShowsTime;
    public static bool canPortalFromAnywhere;

    private static Sprite placePortalButtonSprite;
    private static Sprite usePortalButtonSprite;
    private static Sprite usePortalSpecialButtonSprite1;
    private static Sprite usePortalSpecialButtonSprite2;
    private static Sprite logSprite;

    public Portalmaker()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Crewmate;
    }

    public static Sprite getPlacePortalButtonSprite()
    {
        if (placePortalButtonSprite) return placePortalButtonSprite;
        placePortalButtonSprite =
            Helpers.loadSpriteFromResources("TheOtherRoles.Resources.PlacePortalButton.png", 115f);
        return placePortalButtonSprite;
    }

    public static Sprite getUsePortalButtonSprite()
    {
        if (usePortalButtonSprite) return usePortalButtonSprite;
        usePortalButtonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.UsePortalButton.png", 115f);
        return usePortalButtonSprite;
    }

    public static Sprite getUsePortalSpecialButtonSprite(bool first)
    {
        if (first)
        {
            if (usePortalSpecialButtonSprite1) return usePortalSpecialButtonSprite1;
            usePortalSpecialButtonSprite1 =
                Helpers.loadSpriteFromResources("TheOtherRoles.Resources.UsePortalSpecialButton1.png", 115f);
            return usePortalSpecialButtonSprite1;
        }

        if (usePortalSpecialButtonSprite2) return usePortalSpecialButtonSprite2;
        usePortalSpecialButtonSprite2 =
            Helpers.loadSpriteFromResources("TheOtherRoles.Resources.UsePortalSpecialButton2.png", 115f);
        return usePortalSpecialButtonSprite2;
    }

    public static Sprite getLogSprite()
    {
        if (logSprite) return logSprite;
        logSprite = FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.DoorLogsButton]
            .Image;
        return logSprite;
    }

    public static void clearAndReload()
    {
        portalmaker = null;
        cooldown = CustomOptionHolder.portalmakerCooldown.getFloat();
        usePortalCooldown = CustomOptionHolder.portalmakerUsePortalCooldown.getFloat();
        logOnlyHasColors = CustomOptionHolder.portalmakerLogOnlyColorType.getBool();
        logShowsTime = CustomOptionHolder.portalmakerLogHasTime.getBool();
        canPortalFromAnywhere = CustomOptionHolder.portalmakerCanPortalFromAnywhere.getBool();
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