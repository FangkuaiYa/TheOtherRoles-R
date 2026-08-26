using System.Collections.Generic;
using UnityEngine;

namespace TheOtherRoles.Roles.Modifier;

public class AntiTeleport : RoleBase
{
    public static AntiTeleport Instance;

    public static Color color = Color.yellow;

    public static RoleInfo Info = new("Anti tp", color, "You will not get teleported", "You will not get teleported",
        RoleId.AntiTeleport, false, true);

    public static List<PlayerControl> antiTeleport = new();
    public static Vector3 position;

    public AntiTeleport()
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
        antiTeleport = new List<PlayerControl>();
        position = Vector3.zero;
    }

    public static void setPosition()
    {
        if (position == Vector3.zero) return;
        if (antiTeleport.FindAll(x => x.PlayerId == PlayerControl.LocalPlayer.PlayerId).Count > 0)
        {
            PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(position);
            if (SubmergedCompatibility.IsSubmerged) SubmergedCompatibility.ChangeFloor(position.y > -7);
        }
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