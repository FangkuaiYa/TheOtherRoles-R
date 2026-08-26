using TheOtherRoles.Utilities;
using UnityEngine;

namespace TheOtherRoles.Roles.Crewmate;

public class Hacker : RoleBase
{
    public static Hacker Instance;

    public static Color color = new Color32(117, 250, 76, byte.MaxValue);

    public static RoleInfo Info = new("Hacker", color, "Hack systems to find the <color=#FF1919FF>Impostors</color>",
        "Hack to find the Impostors", RoleId.Hacker);

    public static PlayerControl hacker;
    public static Minigame vitals;
    public static Minigame doorLog;

    public static float cooldown = 30f;
    public static float duration = 10f;
    public static float toolsNumber = 5f;
    public static bool onlyColorType;
    public static float hackerTimer;
    public static int rechargeTasksNumber = 2;
    public static int rechargedTasks = 2;
    public static int chargesVitals = 1;
    public static int chargesAdminTable = 1;
    public static bool cantMove = true;

    private static Sprite buttonSprite;
    private static Sprite vitalsSprite;
    private static Sprite logSprite;
    private static Sprite adminSprite;

    public Hacker()
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
        buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.HackerButton.png", 115f);
        return buttonSprite;
    }

    public static Sprite getVitalsSprite()
    {
        if (vitalsSprite) return vitalsSprite;
        vitalsSprite = FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.VitalsButton]
            .Image;
        return vitalsSprite;
    }

    public static Sprite getLogSprite()
    {
        if (logSprite) return logSprite;
        logSprite = FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.DoorLogsButton]
            .Image;
        return logSprite;
    }

    public static Sprite getAdminSprite()
    {
        var mapId = GameOptionsManager.Instance.currentNormalGameOptions.MapId;
        var button =
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.PolusAdminButton];
        if (Helpers.isSkeld() || mapId == 3)
            button = FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.AdminMapButton];
        else if (Helpers.isMira())
            button = FastDestroyableSingleton<HudManager>.Instance.UseButton
                .fastUseSettings[ImageNames.MIRAAdminButton];
        else if (Helpers.isAirship())
            button = FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[
                ImageNames.AirshipAdminButton];
        else if (Helpers.isFungle())
            button = FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.AdminMapButton];
        adminSprite = button.Image;
        return adminSprite;
    }

    public static void clearAndReload()
    {
        hacker = null;
        vitals = null;
        doorLog = null;
        hackerTimer = 0f;
        adminSprite = null;
        cooldown = CustomOptionHolder.hackerCooldown.getFloat();
        duration = CustomOptionHolder.hackerHackeringDuration.getFloat();
        onlyColorType = CustomOptionHolder.hackerOnlyColorType.getBool();
        toolsNumber = CustomOptionHolder.hackerToolsNumber.getFloat();
        rechargeTasksNumber = Mathf.RoundToInt(CustomOptionHolder.hackerRechargeTasksNumber.getFloat());
        rechargedTasks = Mathf.RoundToInt(CustomOptionHolder.hackerRechargeTasksNumber.getFloat());
        chargesVitals = Mathf.RoundToInt(CustomOptionHolder.hackerToolsNumber.getFloat()) / 2;
        chargesAdminTable = Mathf.RoundToInt(CustomOptionHolder.hackerToolsNumber.getFloat()) / 2;
        cantMove = CustomOptionHolder.hackerNoMove.getBool();
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