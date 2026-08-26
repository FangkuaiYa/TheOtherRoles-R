using UnityEngine;

namespace TheOtherRoles.Roles.Impostor;

public class Mafioso : RoleBase
{
    public static Mafioso Instance;

    public static Color color = Palette.ImpostorRed;

    public static RoleInfo Info = new("Mafioso", color,
        "Work with the <color=#FF1919FF>Mafia</color> to kill the Crewmates", "Kill all Crewmates", RoleId.Mafioso);

    public static PlayerControl mafioso;

    public Mafioso()
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
        mafioso = null;
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