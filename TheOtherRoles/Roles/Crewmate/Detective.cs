using TheOtherRoles.Objects;
using TheOtherRoles.Patches;
using UnityEngine;

namespace TheOtherRoles.Roles.Crewmate;

public class Detective : RoleBase
{
    public static Detective Instance;

    public static Color color = new Color32(45, 106, 165, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.Detective);

    public static PlayerControl detective;

    public static float footprintIntervall = 1f;
    public static float footprintDuration = 1f;
    public static bool anonymousFootprints;
    public static float reportNameDuration;
    public static float reportColorDuration = 20f;
    public static float timer = 6.2f;

    public Detective()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Crewmate;
    }

    public static void clearAndReload()
    {
        detective = null;
        anonymousFootprints = CustomOptionHolder.detectiveAnonymousFootprints.getBool();
        footprintIntervall = CustomOptionHolder.detectiveFootprintIntervall.getFloat();
        footprintDuration = CustomOptionHolder.detectiveFootprintDuration.getFloat();
        reportNameDuration = CustomOptionHolder.detectiveReportNameDuration.getFloat();
        reportColorDuration = CustomOptionHolder.detectiveReportColorDuration.getFloat();
        timer = 6.2f;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    public override RoleInfo GetRoleInfo()
    {
        return Info;
    }

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (Detective.detective == null || Detective.detective != player) return;
        Detective.timer -= Time.fixedDeltaTime;
        if (Detective.timer <= 0f)
        {
            Detective.timer = Detective.footprintIntervall;
            foreach (var p in PlayerControl.AllPlayerControls)
                if (p != null && p != player && !p.Data.IsDead && !p.inVent)
                    FootprintHolder.Instance.MakeFootprint(p);
        }
    }
}