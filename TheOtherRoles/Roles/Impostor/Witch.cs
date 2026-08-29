using System.Collections.Generic;
using System.Linq;
using TheOtherRoles.Patches;
using UnityEngine;

namespace TheOtherRoles.Roles.Impostor;

public class Witch : RoleBase
{
    public static Witch Instance;

    public static Color color = Palette.ImpostorRed;

    public static RoleInfo Info = new(color, RoleId.Witch);

    public static PlayerControl witch;
    public static List<PlayerControl> futureSpelled = new();
    public static PlayerControl currentTarget;
    public static PlayerControl spellCastingTarget;
    public static float cooldown = 30f;
    public static float spellCastingDuration = 2f;
    public static float cooldownAddition = 10f;
    public static float currentCooldownAddition;
    public static bool canSpellAnyone;
    public static bool triggerBothCooldowns = true;
    public static bool witchVoteSavesTargets = true;

    private static Sprite buttonSprite;

    private static Sprite spelledOverlaySprite;

    public Witch()
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
        buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.SpellButton.png", 115f);
        return buttonSprite;
    }

    public static Sprite getSpelledOverlaySprite()
    {
        if (spelledOverlaySprite) return spelledOverlaySprite;
        spelledOverlaySprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.SpellButtonMeeting.png", 225f);
        return spelledOverlaySprite;
    }

    public static void clearAndReload()
    {
        witch = null;
        futureSpelled = new List<PlayerControl>();
        currentTarget = spellCastingTarget = null;
        cooldown = CustomOptionHolder.witchCooldown.getFloat();
        cooldownAddition = CustomOptionHolder.witchAdditionalCooldown.getFloat();
        currentCooldownAddition = 0f;
        canSpellAnyone = CustomOptionHolder.witchCanSpellAnyone.getBool();
        spellCastingDuration = CustomOptionHolder.witchSpellCastingDuration.getFloat();
        triggerBothCooldowns = CustomOptionHolder.witchTriggerBothCooldowns.getBool();
        witchVoteSavesTargets = CustomOptionHolder.witchVoteSavesTargets.getBool();
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
        if (Witch.witch == null || Witch.witch != player) return;
        List<PlayerControl> untargetables;
        if (Witch.spellCastingTarget != null)
        {
            untargetables = PlayerControl.AllPlayerControls.ToArray()
                .Where(x => x.PlayerId != Witch.spellCastingTarget.PlayerId)
                .ToList();
        }
        else
        {
            untargetables = new List<PlayerControl>();
            if (Spy.spy != null && !Witch.canSpellAnyone) untargetables.Add(Spy.spy);
            if (Sidekick.wasTeamRed && !Witch.canSpellAnyone) untargetables.Add(Sidekick.sidekick);
            if (Jackal.wasTeamRed && !Witch.canSpellAnyone) untargetables.Add(Jackal.jackal);
        }
        Witch.currentTarget = PlayerControlFixedUpdatePatch.setTarget(!Witch.canSpellAnyone, untargetablePlayers: untargetables);
        PlayerControlFixedUpdatePatch.setPlayerOutline(Witch.currentTarget, Witch.color);
    }
}