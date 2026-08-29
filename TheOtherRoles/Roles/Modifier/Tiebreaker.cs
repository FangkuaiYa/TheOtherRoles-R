using UnityEngine;

namespace TheOtherRoles.Roles.Modifier;

public class Tiebreaker : RoleBase
{
    public static Tiebreaker Instance;

    public static Color color = Color.yellow;

    public static RoleInfo Info = new(color, RoleId.Tiebreaker, isModifier: true);

    public static PlayerControl tiebreaker;

    public static bool isTiebreak;

    public Tiebreaker()
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
        tiebreaker = null;
        isTiebreak = false;
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