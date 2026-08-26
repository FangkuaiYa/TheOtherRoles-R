using System.Collections.Generic;
using UnityEngine;

namespace TheOtherRoles.Roles.Modifier;

public class Invert : RoleBase
{
    public static Invert Instance;

    public static Color color = Color.yellow;

    public static RoleInfo Info = new("Invert", color, "Your movement is inverted", "Your movement is inverted",
        RoleId.Invert, false, true);

    public static List<PlayerControl> invert = new();
    public static int meetings = 3;

    public Invert()
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
        invert = new List<PlayerControl>();
        meetings = (int)CustomOptionHolder.modifierInvertDuration.getFloat();
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