using System.Collections.Generic;
using UnityEngine;

namespace TheOtherRoles.Roles.Modifier;

public class Sunglasses : RoleBase
{
    public static Sunglasses Instance;

    public static Color color = Color.yellow;

    public static RoleInfo Info = new("Sunglasses", color, "You got the sunglasses", "Your vision is reduced",
        RoleId.Sunglasses, false, true);

    public static List<PlayerControl> sunglasses = new();
    public static int vision = 1;

    public Sunglasses()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Modifier;
        IsModifier = true;
    }

    public static void clearAndReload()
    {
        sunglasses = new List<PlayerControl>();
        vision = CustomOptionHolder.modifierSunglassesVision.getSelection() + 1;
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