using UnityEngine;

namespace TheOtherRoles.Roles.Modifier;

public class Armored : RoleBase
{
    public static Armored Instance;

    public static Color color = Color.yellow;

    public static RoleInfo Info = new(color, RoleId.Armored, isModifier: true);

    public static PlayerControl armored;

    public static bool isBrokenArmor;

    public Armored()
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
        armored = null;
        isBrokenArmor = false;
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