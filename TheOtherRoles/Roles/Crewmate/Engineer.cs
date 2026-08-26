using UnityEngine;

namespace TheOtherRoles.Roles.Crewmate;

public class Engineer : RoleBase
{
    public static Engineer Instance;

    public static Color color = new Color32(0, 40, 245, byte.MaxValue);

    public static RoleInfo Info = new("Engineer", color, "Maintain important systems on the ship", "Repair the ship",
        RoleId.Engineer);

    public static PlayerControl engineer;
    private static Sprite buttonSprite;

    public static int remainingFixes = 1;
    public static bool highlightForImpostors = true;
    public static bool highlightForTeamJackal = true;

    public Engineer()
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
        buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.RepairButton.png", 115f);
        return buttonSprite;
    }

    public static void clearAndReload()
    {
        engineer = null;
        remainingFixes = Mathf.RoundToInt(CustomOptionHolder.engineerNumberOfFixes.getFloat());
        highlightForImpostors = CustomOptionHolder.engineerHighlightForImpostors.getBool();
        highlightForTeamJackal = CustomOptionHolder.engineerHighlightForTeamJackal.getBool();
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