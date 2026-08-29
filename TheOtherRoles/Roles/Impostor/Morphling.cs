using TheOtherRoles.Patches;
using UnityEngine;

namespace TheOtherRoles.Roles.Impostor;

public class Morphling : RoleBase
{
    public static Morphling Instance;

    public static Color color = Palette.ImpostorRed;

    public static RoleInfo Info = new(color, RoleId.Morphling);

    public static PlayerControl morphling;
    private static Sprite sampleSprite;
    private static Sprite morphSprite;

    public static float cooldown = 30f;
    public static float duration = 10f;

    public static PlayerControl currentTarget;
    public static PlayerControl sampledTarget;
    public static PlayerControl morphTarget;
    public static float morphTimer;

    public Morphling()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Impostor;
    }

    public static void resetMorph()
    {
        morphTarget = null;
        morphTimer = 0f;
        if (morphling == null) return;
        morphling.setDefaultLook();
    }

    public static void clearAndReload()
    {
        resetMorph();
        morphling = null;
        currentTarget = null;
        sampledTarget = null;
        morphTarget = null;
        morphTimer = 0f;
        cooldown = CustomOptionHolder.morphlingCooldown.getFloat();
        duration = CustomOptionHolder.morphlingDuration.getFloat();
    }

    public static Sprite getSampleSprite()
    {
        if (sampleSprite) return sampleSprite;
        sampleSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.SampleButton.png", 115f);
        return sampleSprite;
    }

    public static Sprite getMorphSprite()
    {
        if (morphSprite) return morphSprite;
        morphSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.MorphButton.png", 115f);
        return morphSprite;
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
        if (Morphling.morphling == null || Morphling.morphling != player) return;
        Morphling.currentTarget = PlayerControlFixedUpdatePatch.setTarget();
        PlayerControlFixedUpdatePatch.setPlayerOutline(Morphling.currentTarget, Morphling.color);
    }
}