using TheOtherRoles.Objects;
using TMPro;
using UnityEngine;

namespace TheOtherRoles.Roles.Impostor;

public class BountyHunter : RoleBase
{
    public static BountyHunter Instance;

    public static Color color = Palette.ImpostorRed;

    public static RoleInfo Info = new("Bounty Hunter", color, "Hunt your bounty down", "Hunt your bounty down",
        RoleId.BountyHunter);

    public static PlayerControl bountyHunter;
    public static Arrow arrow;
    public static float bountyDuration = 30f;
    public static bool showArrow = true;
    public static float bountyKillCooldown;
    public static float punishmentTime = 15f;
    public static float arrowUpdateIntervall = 10f;

    public static float arrowUpdateTimer;
    public static float bountyUpdateTimer;
    public static PlayerControl bounty;
    public static TextMeshPro cooldownText;

    public BountyHunter()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Impostor;
    }

    public static void clearAndReload()
    {
        arrow = new Arrow(color);
        bountyHunter = null;
        bounty = null;
        arrowUpdateTimer = 0f;
        bountyUpdateTimer = 0f;
        if (arrow != null && arrow.arrow != null) Object.Destroy(arrow.arrow);
        arrow = null;
        if (cooldownText != null && cooldownText.gameObject != null) Object.Destroy(cooldownText.gameObject);
        cooldownText = null;
        foreach (var p in TORMapOptions.playerIcons.Values)
            if (p != null && p.gameObject != null)
                p.gameObject.SetActive(false);

        bountyDuration = CustomOptionHolder.bountyHunterBountyDuration.getFloat();
        bountyKillCooldown = CustomOptionHolder.bountyHunterReducedCooldown.getFloat();
        punishmentTime = CustomOptionHolder.bountyHunterPunishmentTime.getFloat();
        showArrow = CustomOptionHolder.bountyHunterShowArrow.getBool();
        arrowUpdateIntervall = CustomOptionHolder.bountyHunterArrowUpdateIntervall.getFloat();
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