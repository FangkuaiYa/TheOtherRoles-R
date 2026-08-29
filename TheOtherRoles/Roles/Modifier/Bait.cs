using System.Collections.Generic;
using System.Linq;
using Hazel;
using TheOtherRoles.Patches;
using UnityEngine;

namespace TheOtherRoles.Roles.Modifier;

public class Bait : RoleBase
{
    public static Bait Instance;

    public static Color color = new Color32(0, 247, 255, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.Bait, isModifier: true);

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

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (!Bait.active.Any()) return;
        foreach (var entry in new Dictionary<DeadPlayer, float>(Bait.active))
        {
            Bait.active[entry.Key] = entry.Value - Time.fixedDeltaTime;
            if (entry.Value <= 0)
            {
                Bait.active.Remove(entry.Key);
                if (entry.Key.killerIfExisting != null &&
                    entry.Key.killerIfExisting.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                {
                    Helpers.handleVampireBiteOnBodyReport();
                    RPCProcedure.uncheckedCmdReportDeadBody(entry.Key.killerIfExisting.PlayerId,
                        entry.Key.player.PlayerId);
                    var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                        (byte)CustomRPC.UncheckedCmdReportDeadBody, SendOption.Reliable);
                    writer.Write(entry.Key.killerIfExisting.PlayerId);
                    writer.Write(entry.Key.player.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                }
            }
        }
    }
}