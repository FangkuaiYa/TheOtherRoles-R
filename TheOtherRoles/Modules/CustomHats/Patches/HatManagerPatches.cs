using System.Collections.Generic;
using HarmonyLib;

namespace TheOtherRoles.Modules.CustomHats.Patches;

[HarmonyPatch(typeof(HatManager))]
internal static class HatManagerPatches
{
    private static bool isLoaded;

    [HarmonyPatch(nameof(HatManager.GetHatById))]
    [HarmonyPrefix]
    private static bool GetHatByIdPrefix(HatManager __instance)
    {
        if (isLoaded || CustomHatManager.UnregisteredHats.Count == 0) return true;

        var hatsToAdd = new List<HatData>();
        var cache = CustomHatManager.UnregisteredHats.ToArray();
        foreach (var hat in cache)
        {
            try
            {
                hatsToAdd.Add(CustomHatManager.CreateHatBehaviour(hat));
                CustomHatManager.UnregisteredHats.Remove(hat);
            }
            catch
            {
                // File not downloaded yet
            }
        }

        if (CustomHatManager.UnregisteredHats.Count == 0)
            isLoaded = true;

        if (hatsToAdd.Count > 0)
        {
            var oldLen = __instance.allHats.Length;
            var newArray = new Il2CppReferenceArray<HatData>(oldLen + hatsToAdd.Count);
            for (int i = 0; i < oldLen; i++)
                newArray[i] = __instance.allHats[i];
            for (int i = 0; i < hatsToAdd.Count; i++)
                newArray[oldLen + i] = hatsToAdd[i];
            __instance.allHats = newArray;
        }

        return true;
    }
}
