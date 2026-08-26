using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using InnerNet;
using TheOtherRoles.Utilities;
using UnityEngine;

namespace TheOtherRoles.Roles;

public class CustomRoleManager
{
    private static CustomRoleManager _instance;

    // Special RoleInfos (no corresponding role class)
    public static RoleInfo impostor = new("Impostor", Palette.ImpostorRed,
        Helpers.cs(Palette.ImpostorRed, "Sabotage and kill everyone"), "Sabotage and kill everyone", RoleId.Impostor);

    public static RoleInfo crewmate = new("Crewmate", Color.white, "Find the Impostors", "Find the Impostors",
        RoleId.Crewmate);

    public static RoleInfo hunter = new("Hunter", Palette.ImpostorRed,
        Helpers.cs(Palette.ImpostorRed, "Seek and kill everyone"), "Seek and kill everyone", RoleId.Impostor);

    public static RoleInfo hunted = new("Hunted", Color.white, "Hide", "Hide", RoleId.Crewmate);

    public static RoleInfo prop = new("Prop", Color.white, "Disguise As An Object and Survive", "Disguise As An Object",
        RoleId.Crewmate);

    private static string ReadmePage = "";

    private readonly List<RoleBase> _roles = new();
    public static CustomRoleManager Instance => _instance ??= new CustomRoleManager();
    public IReadOnlyList<RoleBase> Roles => _roles.AsReadOnly();

    public List<RoleInfo> allRoleInfos
    {
        get
        {
            var infos = new List<RoleInfo>();
            infos.Add(impostor);
            infos.Add(crewmate);
            foreach (var role in _roles)
            {
                var info = role.GetRoleInfo();
                if (info != null) infos.Add(info);
            }

            return infos;
        }
    }

    public void RegisterRole(RoleBase role)
    {
        if (!_roles.Any(r => r.GetType() == role.GetType()))
            _roles.Add(role);
    }

    public void RegisterRoles(params RoleBase[] roles)
    {
        foreach (var role in roles)
            RegisterRole(role);
    }

    public T GetRole<T>() where T : RoleBase
    {
        return _roles.OfType<T>().FirstOrDefault();
    }

    public RoleBase GetRoleByType(Type type)
    {
        return _roles.FirstOrDefault(r => r.GetType() == type);
    }

    public IEnumerable<RoleBase> GetRolesByTeam(RoleTeam team)
    {
        return _roles.Where(r => r.Team == team && !r.IsModifier);
    }

    public IEnumerable<RoleBase> GetModifiers()
    {
        return _roles.Where(r => r.IsModifier);
    }

    public void ClearAndReloadAll()
    {
        foreach (var role in _roles)
            role.ClearAndReload();
    }

    public void InitializeAll()
    {
        foreach (var role in _roles)
            role.ClearAndReload();
    }

    // ── Lifecycle Dispatch ────────────────────────────────────────
    public void PlayerFixedUpdate(PlayerControl player)
    {
        foreach (var role in _roles)
            role.PlayerFixedUpdate(player);
    }

    public void PlayerUpdate(PlayerControl player)
    {
        foreach (var role in _roles)
            role.PlayerUpdate(player);
    }

    public void OnMeetingStart()
    {
        foreach (var role in _roles)
            role.OnMeetingStart();
    }

    public void OnMeetingEnd()
    {
        foreach (var role in _roles)
            role.OnMeetingEnd();
    }

    public void OnPlayerExiled(PlayerControl player)
    {
        foreach (var role in _roles)
            role.OnPlayerExiled(player);
    }

    public void OnPlayerDeath(PlayerControl player)
    {
        foreach (var role in _roles)
            role.OnPlayerDeath(player);
    }

    public void OnMurderPlayer(PlayerControl killer, PlayerControl victim)
    {
        foreach (var role in _roles)
            role.OnMurderPlayer(killer, victim);
    }

    public bool CanUseVent(PlayerControl player, Vent vent)
    {
        foreach (var role in _roles)
            if (role.CanUseVent(player, vent))
                return true;
        return false;
    }

    public bool CanKill(PlayerControl killer, PlayerControl target)
    {
        foreach (var role in _roles)
            if (role.CanKill(killer, target))
                return true;
        return false;
    }

    public void SetTarget(PlayerControl target)
    {
        foreach (var role in _roles)
            role.SetTarget(target);
    }

    public void OnClickButton()
    {
        foreach (var role in _roles)
            role.OnClickButton();
    }

    public static void Reset()
    {
        _instance = null;
    }

    public static List<RoleInfo> getRoleInfoForPlayer(PlayerControl p, bool showModifier = true)
    {
        var infos = new List<RoleInfo>();
        if (p == null) return infos;

        // Modifier
        if (showModifier)
        {
            if (!CustomOptionHolder.modifiersAreHidden.getBool() || PlayerControl.LocalPlayer.Data.IsDead ||
                AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Ended)
            {
                if (Bait.bait.Any(x => x.PlayerId == p.PlayerId)) infos.Add(Bait.Info);
                if (Bloody.bloody.Any(x => x.PlayerId == p.PlayerId)) infos.Add(Bloody.Info);
                if (Vip.vip.Any(x => x.PlayerId == p.PlayerId)) infos.Add(Vip.Info);
            }

            if (p == Lovers.lover1 || p == Lovers.lover2) infos.Add(Lovers.Info);
            if (p == Tiebreaker.tiebreaker) infos.Add(Tiebreaker.Info);
            if (AntiTeleport.antiTeleport.Any(x => x.PlayerId == p.PlayerId)) infos.Add(AntiTeleport.Info);
            if (Sunglasses.sunglasses.Any(x => x.PlayerId == p.PlayerId)) infos.Add(Sunglasses.Info);
            if (p == Mini.mini) infos.Add(Mini.Info);
            if (Invert.invert.Any(x => x.PlayerId == p.PlayerId)) infos.Add(Invert.Info);
            if (Chameleon.chameleon.Any(x => x.PlayerId == p.PlayerId)) infos.Add(Chameleon.Info);
            if (p == Armored.armored) infos.Add(Armored.Info);
            if (p == Shifter.shifter) infos.Add(Shifter.Info);
        }

        var count = infos.Count;

        // Special roles
        if (p == Jester.jester) infos.Add(Jester.Info);
        if (p == Mayor.mayor) infos.Add(Mayor.Info);
        if (p == Portalmaker.portalmaker) infos.Add(Portalmaker.Info);
        if (p == Engineer.engineer) infos.Add(Engineer.Info);
        if (p == Sheriff.sheriff || p == Sheriff.formerSheriff) infos.Add(Sheriff.Info);
        if (p == Deputy.deputy) infos.Add(Deputy.Info);
        if (p == Lighter.lighter) infos.Add(Lighter.Info);
        if (p == Godfather.godfather) infos.Add(Godfather.Info);
        if (p == Mafioso.mafioso) infos.Add(Mafioso.Info);
        if (p == Janitor.janitor) infos.Add(Janitor.Info);
        if (p == Morphling.morphling) infos.Add(Morphling.Info);
        if (p == Camouflager.camouflager) infos.Add(Camouflager.Info);
        if (p == Vampire.vampire) infos.Add(Vampire.Info);
        if (p == Eraser.eraser) infos.Add(Eraser.Info);
        if (p == Trickster.trickster) infos.Add(Trickster.Info);
        if (p == Cleaner.cleaner) infos.Add(Cleaner.Info);
        if (p == Warlock.warlock) infos.Add(Warlock.Info);
        if (p == Witch.witch) infos.Add(Witch.Info);
        if (p == Ninja.ninja) infos.Add(Ninja.Info);
        if (p == Bomber.bomber) infos.Add(Bomber.Info);
        if (p == Yoyo.yoyo) infos.Add(Yoyo.Info);
        if (p == Detective.detective) infos.Add(Detective.Info);
        if (p == TimeMaster.timeMaster) infos.Add(TimeMaster.Info);
        if (p == Medic.medic) infos.Add(Medic.Info);
        if (p == Swapper.swapper) infos.Add(Swapper.Info);
        if (p == Seer.seer) infos.Add(Seer.Info);
        if (p == Hacker.hacker) infos.Add(Hacker.Info);
        if (p == Tracker.tracker) infos.Add(Tracker.Info);
        if (p == Snitch.snitch) infos.Add(Snitch.Info);
        if (p == Jackal.jackal ||
            (Jackal.formerJackals != null && Jackal.formerJackals.Any(x => x.PlayerId == p.PlayerId)))
            infos.Add(Jackal.Info);
        if (p == Sidekick.sidekick) infos.Add(Sidekick.Info);
        if (p == Spy.spy) infos.Add(Spy.Info);
        if (p == SecurityGuard.securityGuard) infos.Add(SecurityGuard.Info);
        if (p == Arsonist.arsonist) infos.Add(Arsonist.Info);
        if (p == Guesser.niceGuesser) infos.Add(Guesser.NiceInfo);
        if (p == Guesser.evilGuesser) infos.Add(Guesser.EvilInfo);
        if (p == BountyHunter.bountyHunter) infos.Add(BountyHunter.Info);
        if (p == Vulture.vulture) infos.Add(Vulture.Info);
        if (p == Medium.medium) infos.Add(Medium.Info);
        if (p == Lawyer.lawyer && !Lawyer.isProsecutor) infos.Add(Lawyer.Info);
        if (p == Lawyer.lawyer && Lawyer.isProsecutor) infos.Add(Lawyer.ProsecutorInfo);
        if (p == Trapper.trapper) infos.Add(Trapper.Info);
        if (p == Pursuer.pursuer) infos.Add(Pursuer.Info);
        if (p == Thief.thief) infos.Add(Thief.Info);

        // Default roles
        if (infos.Count == count)
        {
            if (p.Data.Role.IsImpostor)
                infos.Add(TORMapOptions.gameMode == CustomGamemodes.HideNSeek ||
                          TORMapOptions.gameMode == CustomGamemodes.PropHunt
                    ? hunter
                    : impostor);
            else
                infos.Add(TORMapOptions.gameMode == CustomGamemodes.HideNSeek ? hunted :
                    TORMapOptions.gameMode == CustomGamemodes.PropHunt ? prop : crewmate);
        }

        return infos;
    }

    public static string GetRolesString(PlayerControl p, bool useColors, bool showModifier = true,
        bool suppressGhostInfo = false)
    {
        string roleName;
        roleName = string.Join(" ",
            getRoleInfoForPlayer(p, showModifier).Select(x => useColors ? Helpers.cs(x.color, x.name) : x.name)
                .ToArray());
        if (Lawyer.target != null && p.PlayerId == Lawyer.target.PlayerId && PlayerControl.LocalPlayer != Lawyer.target)
            roleName += useColors ? Helpers.cs(Pursuer.color, " §") : " §";
        if (HandleGuesser.isGuesserGm && HandleGuesser.isGuesser(p.PlayerId))
        {
            var remainingShots = HandleGuesser.remainingShots(p.PlayerId);
            var (playerCompleted, playerTotal) = TasksHandler.taskInfo(p.Data);
            if ((!Helpers.isEvil(p) && playerCompleted < HandleGuesser.tasksToUnlock) || remainingShots == 0)
                roleName += Helpers.cs(Color.gray, " (Guesser)");
            else
                roleName += Helpers.cs(Color.white, " (Guesser)");
        }

        if (!suppressGhostInfo && p != null)
        {
            if (p == Shifter.shifter &&
                (PlayerControl.LocalPlayer == Shifter.shifter || Helpers.shouldShowGhostInfo()) &&
                Shifter.futureShift != null)
                roleName += Helpers.cs(Color.yellow, " ← " + Shifter.futureShift.Data.PlayerName);
            if (p == Vulture.vulture && (PlayerControl.LocalPlayer == Vulture.vulture || Helpers.shouldShowGhostInfo()))
                roleName = roleName + Helpers.cs(Vulture.color,
                    $" ({Vulture.vultureNumberToWin - Vulture.eatenBodies} left)");
            if (Helpers.shouldShowGhostInfo())
            {
                if (Eraser.futureErased.Contains(p))
                    roleName = Helpers.cs(Color.gray, "(erased) ") + roleName;
                if (Vampire.vampire != null && !Vampire.vampire.Data.IsDead && Vampire.bitten == p && !p.Data.IsDead)
                    roleName = Helpers.cs(Vampire.color,
                        $"(bitten {(int)HudManagerStartPatch.vampireKillButton.Timer + 1}) ") + roleName;
                if (Deputy.handcuffedPlayers.Contains(p.PlayerId))
                    roleName = Helpers.cs(Color.gray, "(cuffed) ") + roleName;
                if (Deputy.handcuffedKnows.ContainsKey(p.PlayerId))
                    roleName = Helpers.cs(Deputy.color, "(cuffed) ") + roleName;
                if (p == Warlock.curseVictim)
                    roleName = Helpers.cs(Warlock.color, "(cursed) ") + roleName;
                if (p == Ninja.ninjaMarked)
                    roleName = Helpers.cs(Ninja.color, "(marked) ") + roleName;
                if (Pursuer.blankedList.Contains(p) && !p.Data.IsDead)
                    roleName = Helpers.cs(Pursuer.color, "(blanked) ") + roleName;
                if (Witch.futureSpelled.Contains(p) && !MeetingHud.Instance)
                    roleName = Helpers.cs(Witch.color, "☆ ") + roleName;
                if (BountyHunter.bounty == p)
                    roleName = Helpers.cs(BountyHunter.color, "(bounty) ") + roleName;
                if (Arsonist.dousedPlayers.Contains(p))
                    roleName = Helpers.cs(Arsonist.color, "♨ ") + roleName;
                if (p == Arsonist.arsonist)
                    roleName = roleName + Helpers.cs(Arsonist.color,
                        $" ({PlayerControl.AllPlayerControls.ToArray().Count(x => { return x != Arsonist.arsonist && !x.Data.IsDead && !x.Data.Disconnected && !Arsonist.dousedPlayers.Any(y => y.PlayerId == x.PlayerId); })} left)");
                if (p == Jackal.fakeSidekick)
                    roleName = Helpers.cs(Sidekick.color, " (fake SK)") + roleName;

                // Death Reason on Ghosts
                if (p.Data.IsDead)
                {
                    var deathReasonString = "";
                    var deadPlayer = GameHistory.deadPlayers.FirstOrDefault(x => x.player.PlayerId == p.PlayerId);

                    Color killerColor = new();
                    if (deadPlayer != null && deadPlayer.killerIfExisting != null)
                        killerColor = getRoleInfoForPlayer(deadPlayer.killerIfExisting, false).FirstOrDefault().color;

                    if (deadPlayer != null)
                    {
                        switch (deadPlayer.deathReason)
                        {
                            case DeadPlayer.CustomDeathReason.Disconnect:
                                deathReasonString = " - disconnected";
                                break;
                            case DeadPlayer.CustomDeathReason.Exile:
                                deathReasonString = " - voted out";
                                break;
                            case DeadPlayer.CustomDeathReason.Kill:
                                deathReasonString =
                                    $" - killed by {Helpers.cs(killerColor, deadPlayer.killerIfExisting.Data.PlayerName)}";
                                break;
                            case DeadPlayer.CustomDeathReason.Guess:
                                if (deadPlayer.killerIfExisting.Data.PlayerName == p.Data.PlayerName)
                                    deathReasonString = " - failed guess";
                                else
                                    deathReasonString =
                                        $" - guessed by {Helpers.cs(killerColor, deadPlayer.killerIfExisting.Data.PlayerName)}";
                                break;
                            case DeadPlayer.CustomDeathReason.Shift:
                                deathReasonString =
                                    $" - {Helpers.cs(Color.yellow, "shifted")} {Helpers.cs(killerColor, deadPlayer.killerIfExisting.Data.PlayerName)}";
                                break;
                            case DeadPlayer.CustomDeathReason.WitchExile:
                                deathReasonString =
                                    $" - {Helpers.cs(Witch.color, "witched")} by {Helpers.cs(killerColor, deadPlayer.killerIfExisting.Data.PlayerName)}";
                                break;
                            case DeadPlayer.CustomDeathReason.LoverSuicide:
                                deathReasonString = $" - {Helpers.cs(Lovers.color, "lover died")}";
                                break;
                            case DeadPlayer.CustomDeathReason.LawyerSuicide:
                                deathReasonString = $" - {Helpers.cs(Lawyer.color, "bad Lawyer")}";
                                break;
                            case DeadPlayer.CustomDeathReason.Bomb:
                                deathReasonString =
                                    $" - bombed by {Helpers.cs(killerColor, deadPlayer.killerIfExisting.Data.PlayerName)}";
                                break;
                            case DeadPlayer.CustomDeathReason.Arson:
                                deathReasonString =
                                    $" - burnt by {Helpers.cs(killerColor, deadPlayer.killerIfExisting.Data.PlayerName)}";
                                break;
                        }

                        roleName = roleName + deathReasonString;
                    }
                }
            }
        }

        return roleName;
    }

    public static async Task loadReadme()
    {
        if (ReadmePage == "")
        {
            var client = new HttpClient();
            var response = await client.GetAsync(Helpers.isChinese()
                ? "https://v6.gh-proxy.org/"
                : "" + "https://raw.githubusercontent.com/FangkuaiYa/TheOtherRoles-R/main/README.md");
            response.EnsureSuccessStatusCode();
            var httpres = await response.Content.ReadAsStringAsync();
            ReadmePage = httpres;
        }
    }

    public static string GetRoleDescription(RoleInfo roleInfo)
    {
        while (ReadmePage == "")
        {
        }

        var index = ReadmePage.IndexOf($"## {roleInfo.name}");
        var endindex = ReadmePage.Substring(index).IndexOf("### Game Options");
        return ReadmePage.Substring(index, endindex);
    }
}