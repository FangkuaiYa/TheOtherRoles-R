using System.Collections.Generic;
using UnityEngine;

namespace TheOtherRoles.Roles.Modifier;

public class Bloody : RoleBase
{
    public static Bloody Instance;

    public static Color color = Color.yellow;

    public static RoleInfo Info = new("Bloody", color, "Your killer leaves a bloody trail",
        "Your killer leaves a bloody trail", RoleId.Bloody, false, true);

    public static List<PlayerControl> bloody = new();
    public static Dictionary<byte, float> active = new();
    public static Dictionary<byte, byte> bloodyKillerMap = new();

    public static float duration = 5f;

    public Bloody()
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
        bloody = new List<PlayerControl>();
        active = new Dictionary<byte, float>();
        bloodyKillerMap = new Dictionary<byte, byte>();
        duration = CustomOptionHolder.modifierBloodyDuration.getFloat();
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