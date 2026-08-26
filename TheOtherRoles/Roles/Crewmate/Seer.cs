using System.Collections.Generic;
using UnityEngine;

namespace TheOtherRoles.Roles.Crewmate;

public class Seer : RoleBase
{
    public static Seer Instance;

    public static Color color = new Color32(97, 178, 108, byte.MaxValue);

    public static RoleInfo Info = new("Seer", color, "You will see players die", "You will see players die",
        RoleId.Seer);

    public static PlayerControl seer;
    public static List<Vector3> deadBodyPositions = new();

    public static float soulDuration = 15f;
    public static bool limitSoulDuration;
    public static int mode;

    private static Sprite soulSprite;

    public Seer()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Crewmate;
    }

    public static Sprite getSoulSprite()
    {
        if (soulSprite) return soulSprite;
        soulSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Soul.png", 500f);
        return soulSprite;
    }

    public static void clearAndReload()
    {
        seer = null;
        deadBodyPositions = new List<Vector3>();
        limitSoulDuration = CustomOptionHolder.seerLimitSoulDuration.getBool();
        soulDuration = CustomOptionHolder.seerSoulDuration.getFloat();
        mode = CustomOptionHolder.seerMode.getSelection();
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