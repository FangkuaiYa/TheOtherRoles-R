using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TheOtherRoles.Roles.Neutral;

public class Arsonist : RoleBase
{
    public static Arsonist Instance;

    public static Color color = new Color32(238, 112, 46, byte.MaxValue);
    public static RoleInfo Info = new("Arsonist", color, "Let them burn", "Let them burn", RoleId.Arsonist, true);

    public static PlayerControl arsonist;

    public static float cooldown = 30f;
    public static float duration = 3f;
    public static bool triggerArsonistWin;

    public static PlayerControl currentTarget;
    public static PlayerControl douseTarget;
    public static List<PlayerControl> dousedPlayers = new();

    private static Sprite douseSprite;
    private static Sprite igniteSprite;

    public Arsonist()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Neutral;
    }

    public static Sprite getDouseSprite()
    {
        if (douseSprite) return douseSprite;
        douseSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.DouseButton.png", 115f);
        return douseSprite;
    }

    public static Sprite getIgniteSprite()
    {
        if (igniteSprite) return igniteSprite;
        igniteSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.IgniteButton.png", 115f);
        return igniteSprite;
    }

    public static bool dousedEveryoneAlive()
    {
        return PlayerControl.AllPlayerControls.ToArray().All(x =>
        {
            return x == arsonist || x.Data.IsDead || x.Data.Disconnected ||
                   dousedPlayers.Any(y => y.PlayerId == x.PlayerId);
        });
    }

    public static void clearAndReload()
    {
        arsonist = null;
        currentTarget = null;
        douseTarget = null;
        triggerArsonistWin = false;
        dousedPlayers = new List<PlayerControl>();
        foreach (var p in TORMapOptions.playerIcons.Values)
            if (p != null && p.gameObject != null)
                p.gameObject.SetActive(false);
        cooldown = CustomOptionHolder.arsonistCooldown.getFloat();
        duration = CustomOptionHolder.arsonistDuration.getFloat();
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