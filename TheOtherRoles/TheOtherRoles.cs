using System.Linq;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using TheOtherRoles.Objects;

using TheOtherRoles.Utilities;
using TheOtherRoles.CustomGameModes;
using static TheOtherRoles.TheOtherRoles;
using AmongUs.Data;
using Hazel;
using Reactor.Utilities.Extensions;
using TheOtherRoles.Roles;

namespace TheOtherRoles
{
    [HarmonyPatch]
    public static class TheOtherRoles
    {
        public static System.Random rnd = new System.Random((int)DateTime.Now.Ticks);

        private static bool rolesRegistered = false;

        public static void clearAndReloadRoles() {
            if (!rolesRegistered) {
                RegisterAllRoles();
                rolesRegistered = true;
            }

            Roles.CustomRoleManager.Instance.ClearAndReloadAll();

            HandleGuesser.clearAndReload();
            HideNSeek.clearAndReload();
            PropHunt.clearAndReload();
        }

        private static void RegisterAllRoles() {
            new Roles.Crewmate.Sheriff();
            new Roles.Crewmate.Deputy();
            new Roles.Crewmate.Lighter();
            new Roles.Crewmate.Detective();
            new Roles.Crewmate.TimeMaster();
            new Roles.Crewmate.Medic();
            new Roles.Crewmate.Swapper();
            new Roles.Crewmate.Seer();
            new Roles.Crewmate.Hacker();
            new Roles.Crewmate.Tracker();
            new Roles.Crewmate.Snitch();
            new Roles.Crewmate.Engineer();
            new Roles.Crewmate.Mayor();
            new Roles.Crewmate.Portalmaker();
            new Roles.Crewmate.SecurityGuard();
            new Roles.Crewmate.Medium();
            new Roles.Crewmate.Trapper();

            new Roles.Impostor.Godfather();
            new Roles.Impostor.Mafioso();
            new Roles.Impostor.Janitor();
            new Roles.Impostor.Morphling();
            new Roles.Impostor.Camouflager();
            new Roles.Impostor.Vampire();
            new Roles.Impostor.Eraser();
            new Roles.Impostor.Trickster();
            new Roles.Impostor.Cleaner();
            new Roles.Impostor.Warlock();
            new Roles.Impostor.Spy();
            new Roles.Impostor.Witch();
            new Roles.Impostor.Ninja();
            new Roles.Impostor.BountyHunter();
            new Roles.Impostor.Bomber();
            new Roles.Impostor.Yoyo();

            new Roles.Neutral.Jester();
            new Roles.Neutral.Jackal();
            new Roles.Neutral.Sidekick();
            new Roles.Neutral.Arsonist();
            new Roles.Neutral.Vulture();
            new Roles.Neutral.Lawyer();
            new Roles.Neutral.Pursuer();
            new Roles.Neutral.Thief();

            new Roles.Modifier.Bait();
            new Roles.Modifier.Bloody();
            new Roles.Modifier.AntiTeleport();
            new Roles.Modifier.Tiebreaker();
            new Roles.Modifier.Sunglasses();
            new Roles.Modifier.Mini();
            new Roles.Modifier.Vip();
            new Roles.Modifier.Invert();
            new Roles.Modifier.Chameleon();
            new Roles.Modifier.Armored();
            new Roles.Modifier.Shifter();
            new Roles.Modifier.Lovers();
        }
    }
}
