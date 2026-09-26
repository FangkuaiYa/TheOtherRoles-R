using System.Collections.Generic;
using System.Linq;
using Cpp2IL.Core.Extensions;
using HarmonyLib;
using UnityEngine;

namespace TheOtherRoles.Modules.CustomHats.Patches;

[HarmonyPatch(typeof(HatManager))]
internal static class HatManagerPatches
{
    private static bool isRunning;
    private static bool isLoaded;
    private static float nextMergeAttempt;
    private const float BaseMergeCooldown = 0.2f;
    private const float MaxMergeCooldown = 2f;
    private static float currentCooldown = BaseMergeCooldown;

    [HarmonyPatch(nameof(HatManager.GetHatById))]
    [HarmonyPrefix]
    private static void GetHatByIdPrefix(HatManager __instance)
    {
        if (isRunning || isLoaded) return;
        if (CustomHatManager.UnregisteredHats.Count == 0)
        {
            isLoaded = true;
            return;
        }

        if (Time.realtimeSinceStartup < nextMergeAttempt) return;

        isRunning = true;
        // Maybe we can use lock keyword to ensure simultaneous list manipulations ?
        // -> I think lock only lbocks other threads from changing the array, but this seems to happen in one thread
        var allHats = __instance.allHats.ToList();
        var cache = CustomHatManager.UnregisteredHats.Clone();
        var added = false;
        var failedCount = 0;

        foreach (var hat in cache)
        {
            try
            {
                allHats.Add(CustomHatManager.CreateHatBehaviour(hat));
                CustomHatManager.UnregisteredHats.Remove(hat);
                added = true;
            }
            catch
            {
                // This means the file has not been downloaded yet, do nothing...
                failedCount++;
            }
        }

        if (CustomHatManager.UnregisteredHats.Count == 0)
        {
            isLoaded = true;
            currentCooldown = BaseMergeCooldown;
        }
        else if (failedCount > 0)
        {
            currentCooldown = Mathf.Min(currentCooldown * 1.5f, MaxMergeCooldown);
        }
        else
        {
            currentCooldown = BaseMergeCooldown;
        }

        nextMergeAttempt = Time.realtimeSinceStartup + currentCooldown;
        cache.Clear();

        // only touch hats if something actually changed
        if (added) __instance.allHats = allHats.ToArray();
    }

    [HarmonyPatch(nameof(HatManager.GetHatById))]
    [HarmonyPostfix]
    private static void GetHatByIdPostfix()
    {
        isRunning = false;
    }
}