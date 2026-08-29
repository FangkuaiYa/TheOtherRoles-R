using UnityEngine;

namespace TheOtherRoles.Roles.Impostor;

public class Godfather : RoleBase
{
    public static Godfather Instance;

    public static Color color = Palette.ImpostorRed;
    public static RoleInfo Info = new(color, RoleId.Godfather);

    public static PlayerControl godfather;

    public Godfather()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Impostor;
    }

    public static void clearAndReload()
    {
        godfather = null;
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