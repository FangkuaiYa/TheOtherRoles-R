using System;
using System.Linq;
using TheOtherRoles.Patches;
using TheOtherRoles.Utilities;
using UnityEngine;

namespace TheOtherRoles.Roles.Crewmate;

public class TimeMaster : RoleBase
{
    public static TimeMaster Instance;

    public static Color color = new Color32(112, 142, 239, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.TimeMaster);

    public static PlayerControl timeMaster;

    public static bool reviveDuringRewind = false;
    public static float rewindTime = 3f;
    public static float shieldDuration = 3f;
    public static float cooldown = 30f;

    public static bool shieldActive;
    public static bool isRewinding;

    private static Sprite buttonSprite;

    public TimeMaster()
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
        buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.TimeShieldButton.png", 115f);
        return buttonSprite;
    }

    public static void clearAndReload()
    {
        timeMaster = null;
        isRewinding = false;
        shieldActive = false;
        rewindTime = CustomOptionHolder.timeMasterRewindTime.getFloat();
        shieldDuration = CustomOptionHolder.timeMasterShieldDuration.getFloat();
        cooldown = CustomOptionHolder.timeMasterCooldown.getFloat();
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
        if (TimeMaster.isRewinding)
        {
            if (GameHistory.localPlayerPositions.Count > 0)
            {
                var next = GameHistory.localPlayerPositions[0];
                if (next.Item2)
                {
                    if (player.inVent)
                        foreach (var vent in MapUtilities.CachedShipStatus.AllVents)
                        {
                            bool canUse;
                            bool couldUse;
                            vent.CanUse(player.Data, out canUse, out couldUse);
                            if (canUse)
                            {
                                player.MyPhysics.RpcExitVent(vent.Id);
                                vent.SetButtons(false);
                            }
                        }
                    player.transform.position = next.Item1;
                }
                else if (GameHistory.localPlayerPositions.Any(x => x.Item2))
                {
                    player.transform.position = next.Item1;
                }
                if (SubmergedCompatibility.IsSubmerged) SubmergedCompatibility.ChangeFloor(next.Item1.y > -7);
                GameHistory.localPlayerPositions.RemoveAt(0);
                if (GameHistory.localPlayerPositions.Count > 1)
                    GameHistory.localPlayerPositions.RemoveAt(0);
            }
            else
            {
                TimeMaster.isRewinding = false;
                player.moveable = true;
            }
        }
        else
        {
            while (GameHistory.localPlayerPositions.Count >= Mathf.Round(TimeMaster.rewindTime / Time.fixedDeltaTime))
                GameHistory.localPlayerPositions.RemoveAt(GameHistory.localPlayerPositions.Count - 1);
            GameHistory.localPlayerPositions.Insert(0,
                new Tuple<Vector3, bool>(player.transform.position, player.CanMove));
        }
    }
}