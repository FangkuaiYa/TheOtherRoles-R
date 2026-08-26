using UnityEngine;

namespace TheOtherRoles.Roles.Crewmate;

public class Lighter : RoleBase
{
    public static Lighter Instance;

    public static Color color = new Color32(238, 229, 190, byte.MaxValue);

    public static RoleInfo Info = new("Lighter", color, "Your light never goes out", "Your light never goes out",
        RoleId.Lighter);

    public static PlayerControl lighter;

    public static float lighterModeLightsOnVision = 2f;
    public static float lighterModeLightsOffVision = 0.75f;
    public static float flashlightWidth = 0.75f;

    public Lighter()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Crewmate;
    }

    public static void clearAndReload()
    {
        lighter = null;
        flashlightWidth = CustomOptionHolder.lighterFlashlightWidth.getFloat();
        lighterModeLightsOnVision = CustomOptionHolder.lighterModeLightsOnVision.getFloat();
        lighterModeLightsOffVision = CustomOptionHolder.lighterModeLightsOffVision.getFloat();
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