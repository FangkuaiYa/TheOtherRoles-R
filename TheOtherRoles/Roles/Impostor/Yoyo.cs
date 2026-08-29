using UnityEngine;

namespace TheOtherRoles.Roles.Impostor;

public class Yoyo : RoleBase
{
    public static Yoyo Instance;

    public static Color color = Palette.ImpostorRed;

    public static RoleInfo Info = new(color, RoleId.Yoyo);

    public static PlayerControl yoyo = null;
    public static float blinkDuration;
    public static float markCooldown;
    public static bool markStaysOverMeeting;
    public static bool hasAdminTable;
    public static float adminCooldown;
    public static float silhouetteVisibility;

    public static Vector3? markedLocation;

    private static Sprite markButtonSprite;
    private static Sprite blinkButtonSprite;

    public Yoyo()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Impostor;
    }

    public static float SilhouetteVisibility =>
        silhouetteVisibility == 0 && (PlayerControl.LocalPlayer == yoyo || PlayerControl.LocalPlayer.Data.IsDead)
            ? 0.1f
            : silhouetteVisibility;

    public static Sprite getMarkButtonSprite()
    {
        if (markButtonSprite) return markButtonSprite;
        markButtonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.YoyoMarkButtonSprite.png", 115f);
        return markButtonSprite;
    }

    public static Sprite getBlinkButtonSprite()
    {
        if (blinkButtonSprite) return blinkButtonSprite;
        blinkButtonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.YoyoBlinkButtonSprite.png", 115f);
        return blinkButtonSprite;
    }

    public static void markLocation(Vector3 position)
    {
        markedLocation = position;
    }

    public static void clearAndReload()
    {
        blinkDuration = CustomOptionHolder.yoyoBlinkDuration.getFloat();
        markCooldown = CustomOptionHolder.yoyoMarkCooldown.getFloat();
        markStaysOverMeeting = CustomOptionHolder.yoyoMarkStaysOverMeeting.getBool();
        hasAdminTable = CustomOptionHolder.yoyoHasAdminTable.getBool();
        adminCooldown = CustomOptionHolder.yoyoAdminTableCooldown.getFloat();
        silhouetteVisibility = CustomOptionHolder.yoyoSilhouetteVisibility.getSelection() / 10f;

        markedLocation = null;
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