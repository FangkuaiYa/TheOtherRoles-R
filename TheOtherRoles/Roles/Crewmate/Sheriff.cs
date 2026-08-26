using UnityEngine;

namespace TheOtherRoles.Roles.Crewmate;

public class Sheriff : RoleBase
{
    public static Sheriff Instance;

    public static Color color = new Color32(248, 205, 70, byte.MaxValue);

    public static RoleInfo Info = new("Sheriff", color, "Shoot the <color=#FF1919FF>Impostors</color>",
        "Shoot the Impostors", RoleId.Sheriff);

    public static PlayerControl sheriff;
    public static float cooldown = 30f;
    public static bool canKillNeutrals;
    public static bool spyCanDieToSheriff;

    public static PlayerControl currentTarget;
    public static PlayerControl formerDeputy;
    public static PlayerControl formerSheriff;

    public Sheriff()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Crewmate;
    }

    public static void replaceCurrentSheriff(PlayerControl deputy)
    {
        if (!formerSheriff) formerSheriff = sheriff;
        sheriff = deputy;
        currentTarget = null;
        cooldown = CustomOptionHolder.sheriffCooldown.getFloat();
    }

    public static void clearAndReload()
    {
        sheriff = null;
        currentTarget = null;
        formerDeputy = null;
        formerSheriff = null;
        cooldown = CustomOptionHolder.sheriffCooldown.getFloat();
        canKillNeutrals = CustomOptionHolder.sheriffCanKillNeutrals.getBool();
        spyCanDieToSheriff = CustomOptionHolder.spyCanDieToSheriff.getBool();
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }
}