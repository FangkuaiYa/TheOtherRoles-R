using System.Collections.Generic;
using TheOtherRoles.Objects;
using TheOtherRoles.Patches;
using UnityEngine;

namespace TheOtherRoles.Roles.Impostor;

public class Vampire : RoleBase
{
    public static Vampire Instance;

    public static Color color = Palette.ImpostorRed;

    public static RoleInfo Info = new(color, RoleId.Vampire);

    public static PlayerControl vampire;
    public static float delay = 10f;
    public static float cooldown = 30f;
    public static bool canKillNearGarlics = true;
    public static bool localPlacedGarlic;
    public static bool garlicsActive = true;

    public static PlayerControl currentTarget;
    public static PlayerControl bitten;
    public static bool targetNearGarlic;

    private static Sprite buttonSprite;

    private static Sprite garlicButtonSprite;

    public Vampire()
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
        buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.VampireButton.png", 115f);
        return buttonSprite;
    }

    public static Sprite getGarlicButtonSprite()
    {
        if (garlicButtonSprite) return garlicButtonSprite;
        garlicButtonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.GarlicButton.png", 115f);
        return garlicButtonSprite;
    }

    public static void clearAndReload()
    {
        vampire = null;
        bitten = null;
        targetNearGarlic = false;
        localPlacedGarlic = false;
        currentTarget = null;
        garlicsActive = CustomOptionHolder.vampireSpawnRate.getSelection() > 0;
        delay = CustomOptionHolder.vampireKillDelay.getFloat();
        cooldown = CustomOptionHolder.vampireCooldown.getFloat();
        canKillNearGarlics = CustomOptionHolder.vampireCanKillNearGarlics.getBool();
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
        if (Vampire.vampire == null || Vampire.vampire != player) return;
        PlayerControl target = null;
        if (Spy.spy != null || Sidekick.wasSpy || Jackal.wasSpy)
        {
            if (Spy.impostorsCanKillAnyone)
                target = PlayerControlFixedUpdatePatch.setTarget(false, true);
            else
                target = PlayerControlFixedUpdatePatch.setTarget(true, true,
                    new List<PlayerControl>
                    {
                        Spy.spy, Sidekick.wasTeamRed ? Sidekick.sidekick : null,
                        Jackal.wasTeamRed ? Jackal.jackal : null
                    });
        }
        else
        {
            target = PlayerControlFixedUpdatePatch.setTarget(true, true,
                new List<PlayerControl>
                    { Sidekick.wasImpostor ? Sidekick.sidekick : null, Jackal.wasImpostor ? Jackal.jackal : null });
        }
        var targetNearGarlic = false;
        if (target != null)
            foreach (var garlic in Garlic.garlics)
                if (Vector2.Distance(garlic.garlic.transform.position, target.transform.position) <= 1.91f)
                    targetNearGarlic = true;
        Vampire.targetNearGarlic = targetNearGarlic;
        Vampire.currentTarget = target;
        PlayerControlFixedUpdatePatch.setPlayerOutline(Vampire.currentTarget, Vampire.color);
    }
}