using System;
using HarmonyLib;
using TheOtherRoles.CustomGameModes;
using TheOtherRoles.Utilities;

namespace TheOtherRoles;

[HarmonyPatch]
public static class TheOtherRoles
{
    public static Random rnd = new((int)DateTime.Now.Ticks);

    private static bool rolesRegistered;

    public static void clearAndReloadRoles()
    {
        if (!rolesRegistered)
        {
            RegisterAllRoles();
            rolesRegistered = true;
        }

        CustomRoleManager.Instance.ClearAndReloadAll();

        HandleGuesser.clearAndReload();
        HideNSeek.clearAndReload();
        PropHunt.clearAndReload();
    }

    private static void RegisterAllRoles()
    {
        new Sheriff();
        new Deputy();
        new Lighter();
        new Detective();
        new TimeMaster();
        new Medic();
        new Swapper();
        new Seer();
        new Hacker();
        new Tracker();
        new Snitch();
        new Engineer();
        new Mayor();
        new Portalmaker();
        new SecurityGuard();
        new Medium();
        new Trapper();

        new Godfather();
        new Mafioso();
        new Janitor();
        new Morphling();
        new Camouflager();
        new Vampire();
        new Eraser();
        new Trickster();
        new Cleaner();
        new Warlock();
        new Spy();
        new Witch();
        new Ninja();
        new BountyHunter();
        new Bomber();
        new Yoyo();

        new Jester();
        new Jackal();
        new Sidekick();
        new Arsonist();
        new Vulture();
        new Lawyer();
        new Pursuer();
        new Thief();
        new SchrodingersCat();

        new Bait();
        new Bloody();
        new AntiTeleport();
        new Tiebreaker();
        new Sunglasses();
        new Mini();
        new Vip();
        new Invert();
        new Chameleon();
        new Armored();
        new Shifter();
        new Lovers();
    }
}