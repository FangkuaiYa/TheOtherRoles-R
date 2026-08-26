using UnityEngine;

namespace TheOtherRoles.Roles.Neutral;

public class Lawyer : RoleBase
{
    public static Lawyer Instance;

    public static Color color = new Color32(134, 153, 25, byte.MaxValue);
    public static RoleInfo Info = new("Lawyer", color, "Defend your client", "Defend your client", RoleId.Lawyer, true);

    public static RoleInfo ProsecutorInfo = new("Prosecutor", color, "Vote out your target", "Vote out your target",
        RoleId.Prosecutor, true);

    public static PlayerControl lawyer;
    public static PlayerControl target;
    public static Sprite targetSprite;
    public static bool triggerProsecutorWin;
    public static bool isProsecutor;
    public static bool canCallEmergency = true;

    public static float vision = 1f;
    public static bool lawyerKnowsRole;
    public static bool targetCanBeJester;
    public static bool targetWasGuessed;

    public Lawyer()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Neutral;
    }

    public static Sprite getTargetSprite()
    {
        if (targetSprite) return targetSprite;
        targetSprite = Helpers.loadSpriteFromResources("", 150f);
        return targetSprite;
    }

    public static void clearAndReload(bool clearTarget = true)
    {
        lawyer = null;
        if (clearTarget)
        {
            target = null;
            targetWasGuessed = false;
        }

        isProsecutor = false;
        triggerProsecutorWin = false;
        vision = CustomOptionHolder.lawyerVision.getFloat();
        lawyerKnowsRole = CustomOptionHolder.lawyerKnowsRole.getBool();
        targetCanBeJester = CustomOptionHolder.lawyerTargetCanBeJester.getBool();
        canCallEmergency = CustomOptionHolder.lawyerCanCallEmergency.getBool();
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