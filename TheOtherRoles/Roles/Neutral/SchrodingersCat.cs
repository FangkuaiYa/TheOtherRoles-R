using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using TheOtherRoles.Objects;
using TheOtherRoles.Utilities;
using UnityEngine;

namespace TheOtherRoles.Roles.Neutral;

public class SchrodingersCat : RoleBase
{
    public static SchrodingersCat Instance;

    public enum CatTeam
    {
        None,
        Crewmate,
        Impostor,
        Jackal
    }

    public enum ExileType
    {
        None,
        Crewmate,
        Random
    }

    public static Color color = Color.grey;
    public static RoleInfo Info = new(color, RoleId.SchrodingersCat, isNeutral: true);

    public static PlayerControl cat;
    public static CatTeam team = CatTeam.None;
    public static PlayerControl killer;
    public static ExileType exileType = ExileType.None;

    // Settings
    public static float killCooldown = 20f;
    public static bool killsKiller = false;
    public static bool cantKillUntilLastOne = false;
    public static bool hideRole = false;
    public static bool canChooseTeam = false;

    // Kill button
    public static CustomButton killButton;
    private static Sprite killButtonSprite;

    // Team chooser button
    public static CustomButton switchButton;
    private static Sprite switchButtonSprite;

    public SchrodingersCat()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Neutral;
    }

    public static bool hasTeam()
    {
        return team != CatTeam.None;
    }

    public static bool tasksComplete(PlayerControl player)
    {
        if (player == null || player.Data.IsDead) return false;
        var taskInfo = TasksHandler.taskInfo(player.Data);
        int playerCompleted = taskInfo.Item1;
        int playerTotal = taskInfo.Item2;
        return playerTotal > 0 && playerCompleted >= playerTotal;
    }

    public static void setTeamRPC(CatTeam newTeam)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
            (byte)CustomRPC.SchrodingersCatSetTeam, SendOption.Reliable);
        writer.Write((byte)newTeam);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        setTeam(newTeam);
    }

    public static void setTeam(CatTeam newTeam)
    {
        team = newTeam;
        switch (newTeam)
        {
            case CatTeam.Crewmate:
                if (cat != null) cat.clearAllTasks();
                break;
            case CatTeam.Impostor:
                if (cat != null)
                {
                    cat.clearAllTasks();
                    cat.Data.Role.TeamType = RoleTeamTypes.Impostor;
                    FastDestroyableSingleton<RoleManager>.Instance.SetRole(cat, RoleTypes.Impostor);
                }
                break;
            case CatTeam.Jackal:
                if (cat != null) cat.clearAllTasks();
                break;
        }
    }

    public static void onDeath(PlayerControl victim, PlayerControl killerPlayer)
    {
        if (victim == null || victim != cat || hasTeam()) return;

        CatTeam newTeam;
        if (killerPlayer == null)
        {
            return;
        }
        else if (killerPlayer.Data.Role.IsImpostor)
        {
            newTeam = CatTeam.Impostor;
        }
        else if (killerPlayer == Jackal.jackal || (Sidekick.sidekick != null && killerPlayer == Sidekick.sidekick))
        {
            newTeam = CatTeam.Jackal;
        }
        else
        {
            newTeam = CatTeam.Crewmate;
        }

        // Revive FIRST (before setting team, since SetRole needs alive player)
        victim.Revive();

        // Remove dead body
        foreach (var db in Object.FindObjectsOfType<DeadBody>())
        {
            if (db.ParentId == victim.PlayerId)
            {
                Object.Destroy(db.gameObject);
                break;
            }
        }

        // Remove death record
        var deadPlayer = GameHistory.deadPlayers.FirstOrDefault(x => x.player?.PlayerId == victim.PlayerId);
        if (deadPlayer != null) GameHistory.deadPlayers.Remove(deadPlayer);

        // Set team (after revive, so SetRole works on alive player)
        setTeamRPC(newTeam);

        if (killsKiller && newTeam != CatTeam.Crewmate && killerPlayer != null && !killerPlayer.Data.IsDead)
        {
            killer = killerPlayer;
        }
    }

    public static void onExiled()
    {
        if (cat == null || cat.Data.Disconnected) return;
        if (hasTeam()) return;

        switch (exileType)
        {
            case ExileType.None:
                break;
            case ExileType.Crewmate:
                setTeamRPC(CatTeam.Crewmate);
                break;
            case ExileType.Random:
                var availableTeams = new List<CatTeam> { CatTeam.Crewmate };
                if (PlayerControl.AllPlayerControls.ToArray().Any(p => p.Data.Role.IsImpostor))
                    availableTeams.Add(CatTeam.Impostor);
                if (Jackal.jackal != null || Sidekick.sidekick != null)
                    availableTeams.Add(CatTeam.Jackal);
                var randomTeam = availableTeams[TheOtherRoles.rnd.Next(availableTeams.Count)];
                setTeamRPC(randomTeam);
                break;
        }
    }

    public static void showTeamMenu()
    {
        var availableTeams = new List<CatTeam>();
        if (PlayerControl.AllPlayerControls.ToArray().Any(p => p != cat && !p.Data.IsDead && p.Data.Role.IsImpostor))
            availableTeams.Add(CatTeam.Impostor);
        if (Jackal.jackal != null || Sidekick.sidekick != null)
            availableTeams.Add(CatTeam.Jackal);
        availableTeams.Add(CatTeam.Crewmate);

        if (availableTeams.Count > 0)
        {
            var currentIndex = availableTeams.IndexOf(team);
            var nextIndex = (currentIndex + 1) % availableTeams.Count;
            if (currentIndex < 0) nextIndex = 0;
            setTeamRPC(availableTeams[nextIndex]);
        }
    }

    public static void handleKillsKiller()
    {
        if (killer == null || !killsKiller) return;

        if (killer.Data.IsDead || killer.Data.Disconnected)
        {
            killer = null;
            return;
        }
    }

    public static Sprite getKillButtonSprite()
    {
        if (killButtonSprite) return killButtonSprite;
        killButtonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.KillButton.png", 115f);
        return killButtonSprite;
    }

    public static Sprite getSwitchButtonSprite()
    {
        if (switchButtonSprite) return switchButtonSprite;
        switchButtonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.ShiftButton.png", 115f);
        return switchButtonSprite;
    }

    public static void clearAndReload()
    {
        cat = null;
        team = CatTeam.None;
        killer = null;
        killCooldown = CustomOptionHolder.schrodingersCatKillCooldown.getFloat();
        killsKiller = CustomOptionHolder.schrodingersCatKillsKiller.getBool();
        cantKillUntilLastOne = CustomOptionHolder.schrodingersCatCantKillUntilLastOne.getBool();
        hideRole = CustomOptionHolder.schrodingersCatHideRole.getBool();
        canChooseTeam = CustomOptionHolder.schrodingersCatCanChooseTeam.getBool();
        var exileSelection = CustomOptionHolder.schrodingersCatExileType.getSelection();
        exileType = exileSelection switch
        {
            0 => ExileType.None,
            1 => ExileType.Crewmate,
            2 => ExileType.Random,
            _ => ExileType.None
        };
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    public override RoleInfo GetRoleInfo()
    {
        if (!hasTeam()) return Info;
        Color teamColor = team switch
        {
            CatTeam.Impostor => Palette.ImpostorRed,
            CatTeam.Jackal => Jackal.color,
            CatTeam.Crewmate => Color.white,
            _ => color
        };
        return new RoleInfo(teamColor, RoleId.SchrodingersCat, isNeutral: !hasTeam());
    }

    public override void OnMurderPlayer(PlayerControl killer, PlayerControl target)
    {
        if (target == cat && !hasTeam())
        {
            onDeath(target, killer);
        }
    }

    public override void OnPlayerExiled(PlayerControl player)
    {
        if (player == cat && !hasTeam())
        {
            onExiled();
        }
    }

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (cat == null || cat != player) return;
        handleKillsKiller();
    }

    public override void OnMeetingStart()
    {
        if (killer != null && killsKiller && !killer.Data.IsDead && !killer.Data.Disconnected)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                killer.MurderPlayer(killer);
                var deadPlayer = new DeadPlayer(killer, System.DateTime.UtcNow, DeadPlayer.CustomDeathReason.Kill, killer);
                GameHistory.deadPlayers.Add(deadPlayer);
            }
            killer = null;
        }
    }
}
