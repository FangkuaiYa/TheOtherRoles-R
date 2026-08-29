using System.Collections.Generic;
using TheOtherRoles.Patches;
using UnityEngine;

namespace TheOtherRoles.Roles.Impostor;

public class Eraser : RoleBase
{
    public static Eraser Instance;

    public static Color color = Palette.ImpostorRed;

    public static RoleInfo Info = new(color, RoleId.Eraser);

    public static PlayerControl eraser;
    public static List<byte> alreadyErased = new();
    public static List<PlayerControl> futureErased = new();
    public static PlayerControl currentTarget;
    public static float cooldown = 30f;
    public static bool canEraseAnyone;

    private static Sprite buttonSprite;

    public Eraser()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Impostor;
    }

    public static Sprite getButtonSprite()
    {
        if (buttonSprite) return buttonSprite;
        buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.EraserButton.png", 115f);
        return buttonSprite;
    }

    public static void clearAndReload()
    {
        eraser = null;
        futureErased = new List<PlayerControl>();
        currentTarget = null;
        cooldown = CustomOptionHolder.eraserCooldown.getFloat();
        canEraseAnyone = CustomOptionHolder.eraserCanEraseAnyone.getBool();
        alreadyErased = new List<byte>();
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
        if (Eraser.eraser == null || Eraser.eraser != player) return;
        var untargetables = new List<PlayerControl>();
        if (Spy.spy != null) untargetables.Add(Spy.spy);
        if (Sidekick.wasTeamRed) untargetables.Add(Sidekick.sidekick);
        if (Jackal.wasTeamRed) untargetables.Add(Jackal.jackal);
        Eraser.currentTarget = PlayerControlFixedUpdatePatch.setTarget(!Eraser.canEraseAnyone,
            untargetablePlayers: Eraser.canEraseAnyone ? new List<PlayerControl>() : untargetables);
        PlayerControlFixedUpdatePatch.setPlayerOutline(Eraser.currentTarget, Eraser.color);
    }
}