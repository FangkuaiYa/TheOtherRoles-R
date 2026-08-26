using System.Collections.Generic;
using UnityEngine;

namespace TheOtherRoles.Roles.Modifier;

public class Bait : RoleBase
{
    public static Bait Instance;

    public static Color color = new Color32(0, 247, 255, byte.MaxValue);

    public static RoleInfo Info = new("Bait", color, "Bait your enemies", "Bait your enemies", RoleId.Bait, false,
        true);

    public static List<PlayerControl> bait = new();
    public static Dictionary<DeadPlayer, float> active = new();

    public static float reportDelayMin;
    public static float reportDelayMax;
    public static bool showKillFlash = true;

    public Bait()
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
        bait = new List<PlayerControl>();
        active = new Dictionary<DeadPlayer, float>();
        reportDelayMin = CustomOptionHolder.modifierBaitReportDelayMin.getFloat();
        reportDelayMax = CustomOptionHolder.modifierBaitReportDelayMax.getFloat();
        if (reportDelayMin > reportDelayMax) reportDelayMin = reportDelayMax;
        showKillFlash = CustomOptionHolder.modifierBaitShowKillFlash.getBool();
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