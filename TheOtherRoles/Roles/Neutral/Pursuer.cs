using System.Collections.Generic;
using TheOtherRoles.Patches;
using UnityEngine;

namespace TheOtherRoles.Roles.Neutral;

public class Pursuer : RoleBase
{
    public static Pursuer Instance;

    public static Color color = Lawyer.color;
    public static RoleInfo Info = new(color, RoleId.Pursuer, isNeutral: true);

    public static PlayerControl pursuer;
    public static PlayerControl target;
    public static List<PlayerControl> blankedList = new();
    public static int blanks;
    public static Sprite blank;
    public static bool notAckedExiled;

    public static float cooldown = 30f;
    public static int blanksNumber = 5;

    public Pursuer()
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
        if (blank) return blank;
        blank = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.PursuerButton.png", 115f);
        return blank;
    }

    public static void clearAndReload()
    {
        pursuer = null;
        target = null;
        blankedList = new List<PlayerControl>();
        blanks = 0;
        notAckedExiled = false;

        cooldown = CustomOptionHolder.pursuerCooldown.getFloat();
        blanksNumber = Mathf.RoundToInt(CustomOptionHolder.pursuerBlanksNumber.getFloat());
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
        if (Pursuer.pursuer == null || Pursuer.pursuer != player) return;
        Pursuer.target = PlayerControlFixedUpdatePatch.setTarget();
        PlayerControlFixedUpdatePatch.setPlayerOutline(Pursuer.target, Pursuer.color);
    }
}