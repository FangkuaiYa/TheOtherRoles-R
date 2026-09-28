using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using TheOtherRoles.Patches;
using TheOtherRoles.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

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

    // Active rewind (RewindButton) settings
    public static bool canRewind = false;
    public static float rewindCooldown = 30f;

    // State of the rewind which is currently running
    // rewindDuration is the amount of game time (in seconds) which is being rewound
    public static float rewindDuration = 3f;
    public static float rewindEndTime;

    public static bool shieldActive;
    public static bool isRewinding;

    private static Sprite buttonSprite;
    private static Sprite rewindButtonSprite;

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

    public static Sprite getRewindButtonSprite()
    {
        if (rewindButtonSprite) return rewindButtonSprite;
        rewindButtonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.RewindButton.png", 115f);
        return rewindButtonSprite;
    }

    public static void clearAndReload()
    {
        timeMaster = null;
        isRewinding = false;
        shieldActive = false;
        rewindTime = CustomOptionHolder.timeMasterRewindTime.getFloat();
        shieldDuration = CustomOptionHolder.timeMasterShieldDuration.getFloat();
        cooldown = CustomOptionHolder.timeMasterCooldown.getFloat();
        canRewind = CustomOptionHolder.timeMasterCanRewind.getBool();
        rewindCooldown = CustomOptionHolder.timeMasterRewindCooldown.getFloat();
        reviveDuringRewind = CustomOptionHolder.timeMasterReviveDuringRewind.getBool();
        rewindDuration = rewindTime;
        rewindEndTime = 0f;
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
            if (GameHistory.localPlayerPositions.Count > 0 &&
                (TimeMaster.rewindEndTime <= 0f || Time.time <= TimeMaster.rewindEndTime))
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
            // Keep enough positions in the history to be able to rewind the longest possible rewind
            var historyDuration = Mathf.Max(TimeMaster.rewindTime, TimeMaster.shieldDuration);
            while (GameHistory.localPlayerPositions.Count >= Mathf.Round(historyDuration / Time.fixedDeltaTime))
                GameHistory.localPlayerPositions.RemoveAt(GameHistory.localPlayerPositions.Count - 1);
            GameHistory.localPlayerPositions.Insert(0,
                new Tuple<Vector3, bool>(player.transform.position, player.CanMove));
        }
    }

    // Revives everyone who died within the time span which is currently being rewound.
    // Has to be called on every client, as it is executed within the rewind RPC.
    public static void revivePlayersDiedDuringRewind()
    {
        if (!reviveDuringRewind || TimeMaster.timeMaster == null) return;

        var now = DateTime.UtcNow;
        List<DeadPlayer> diedDuringRewind = GameHistory.deadPlayers
            .Where(x => x.player != null && !x.player.Data.Disconnected &&
                        (now - x.timeOfDeath).TotalSeconds <= TimeMaster.rewindDuration)
            .ToList();

        foreach (var deadPlayer in diedDuringRewind) revivePlayer(deadPlayer);
    }

    private static void revivePlayer(DeadPlayer deadPlayer)
    {
        var player = deadPlayer.player;
        if (player == null) return;

        // Remove the corpse
        foreach (var body in Object.FindObjectsOfType<DeadBody>())
            if (body.ParentId == player.PlayerId)
            {
                Object.Destroy(body.gameObject);
                break;
            }

        // The Medium keeps track of dead players as well
        Medium.deadBodies?.RemoveAll(x => x.Item1?.player?.PlayerId == player.PlayerId);
        Medium.futureDeadBodies?.RemoveAll(x => x.Item1?.player?.PlayerId == player.PlayerId);

        if (player.Data.IsDead)
        {
            player.Revive();
            // Dying replaced the alive role with a ghost role, so it has to be restored.
            // RoleWhenAlive is an Il2CppSystem.Nullable, hence the null check and the .Value access.
            RoleTypes aliveRole;
            var roleWhenAlive = player.Data.RoleWhenAlive;
            if (roleWhenAlive != null)
                aliveRole = roleWhenAlive.Value;
            else
                aliveRole = player.Data.Role.IsImpostor ? RoleTypes.Impostor : RoleTypes.Crewmate;
            FastDestroyableSingleton<RoleManager>.Instance.SetRole(player, aliveRole);

            // Restore venting for roles which only vent as a non impostor (see RPCProcedure.setRole)
            if (AmongUsClient.Instance.AmHost && player.roleCanUseVents() && !player.Data.Role.IsImpostor)
            {
                player.RpcSetRole(RoleTypes.Engineer);
                player.CoSetRole(RoleTypes.Engineer, true);
            }
        }

        GameHistory.deadPlayers.Remove(deadPlayer);
    }
}