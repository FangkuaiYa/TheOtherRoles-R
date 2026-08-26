using System;
using UnityEngine;

namespace TheOtherRoles.Roles.Modifier;

public class Mini : RoleBase
{
    public const float defaultColliderRadius = 0.2233912f;
    public const float defaultColliderOffset = 0.3636057f;
    public static Mini Instance;

    public static Color color = Color.yellow;

    public static RoleInfo Info = new("Mini", color, "No one will harm you until you grow up", "No one will harm you",
        RoleId.Mini, false, true);

    public static PlayerControl mini;

    public static float growingUpDuration = 400f;
    public static bool isGrowingUpInMeeting = true;
    public static DateTime timeOfGrowthStart = DateTime.UtcNow;
    public static DateTime timeOfMeetingStart = DateTime.UtcNow;
    public static float ageOnMeetingStart = 0f;
    public static bool triggerMiniLose;

    public Mini()
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
        mini = null;
        triggerMiniLose = false;
        growingUpDuration = CustomOptionHolder.modifierMiniGrowingUpDuration.getFloat();
        isGrowingUpInMeeting = CustomOptionHolder.modifierMiniGrowingUpInMeeting.getBool();
        timeOfGrowthStart = DateTime.UtcNow;
    }

    public static float growingProgress()
    {
        var timeSinceStart = (float)(DateTime.UtcNow - timeOfGrowthStart).TotalMilliseconds;
        return Mathf.Clamp(timeSinceStart / (growingUpDuration * 1000), 0f, 1f);
    }

    public static bool isGrownUp()
    {
        return growingProgress() == 1f;
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