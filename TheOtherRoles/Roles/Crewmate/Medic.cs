using UnityEngine;

namespace TheOtherRoles.Roles.Crewmate;

public class Medic : RoleBase
{
    public static Medic Instance;

    public static Color color = new Color32(126, 251, 194, byte.MaxValue);

    public static RoleInfo Info = new("Medic", color, "Protect someone with your shield", "Protect other players",
        RoleId.Medic);

    public static PlayerControl medic;
    public static PlayerControl shielded;
    public static PlayerControl futureShielded;

    public static bool usedShield;

    public static int showShielded;
    public static bool showAttemptToShielded;
    public static bool showAttemptToMedic;
    public static bool setShieldAfterMeeting;
    public static bool showShieldAfterMeeting;
    public static bool meetingAfterShielding;

    public static Color shieldedColor = new Color32(0, 221, 255, byte.MaxValue);
    public static PlayerControl currentTarget;

    private static Sprite buttonSprite;

    public Medic()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Crewmate;
    }

    public static Sprite getButtonSprite()
    {
        if (buttonSprite) return buttonSprite;
        buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.ShieldButton.png", 115f);
        return buttonSprite;
    }

    public static bool shieldVisible(PlayerControl target)
    {
        var hasVisibleShield = false;

        var isMorphedMorphling =
            target == Morphling.morphling && Morphling.morphTarget != null && Morphling.morphTimer > 0f;
        if (shielded != null && ((target == shielded && !isMorphedMorphling) ||
                                 (isMorphedMorphling && Morphling.morphTarget == shielded)))
        {
            hasVisibleShield = showShielded == 0 || Helpers.shouldShowGhostInfo()
                                                 || (showShielded == 1 && (PlayerControl.LocalPlayer == shielded ||
                                                                           PlayerControl.LocalPlayer == medic))
                                                 || (showShielded == 2 && PlayerControl.LocalPlayer == medic);
            hasVisibleShield = hasVisibleShield && (meetingAfterShielding || !showShieldAfterMeeting ||
                                                    PlayerControl.LocalPlayer == medic ||
                                                    Helpers.shouldShowGhostInfo());
        }

        return hasVisibleShield;
    }

    public static void clearAndReload()
    {
        medic = null;
        shielded = null;
        futureShielded = null;
        currentTarget = null;
        usedShield = false;
        showShielded = CustomOptionHolder.medicShowShielded.getSelection();
        showAttemptToShielded = CustomOptionHolder.medicShowAttemptToShielded.getBool();
        showAttemptToMedic = CustomOptionHolder.medicShowAttemptToMedic.getBool();
        setShieldAfterMeeting = CustomOptionHolder.medicSetOrShowShieldAfterMeeting.getSelection() == 2;
        showShieldAfterMeeting = CustomOptionHolder.medicSetOrShowShieldAfterMeeting.getSelection() == 1;
        meetingAfterShielding = false;
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