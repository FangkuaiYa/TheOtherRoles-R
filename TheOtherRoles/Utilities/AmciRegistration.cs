using System.Reflection;
using AmongUs.Matchmaking;
using HarmonyLib;
using InnerNet;

namespace TheOtherRoles.Utilities
{
    public static class AmciRegistration
    {
        public static string ModGuid { get; private set; } = "";

        public static void Register()
        {
            ModGuid = Assembly.GetExecutingAssembly().ManifestModule.ModuleVersionId.ToString();
            CurrentModRegistration.ModRegistrationGuidString = ModGuid;
            TheOtherRolesPlugin.Logger.LogInfo($"[AMCI] Register(): GUID={ModGuid}");
        }

        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
        public static class StampPatch
        {
            public static void Postfix()
            {
                CurrentModRegistration.ModRegistrationGuidString = ModGuid;
                ModManager.Instance.ShowModStamp();
            }
        }

        [HarmonyPatch(typeof(CurrentModRegistration), nameof(CurrentModRegistration.TryGetModRegistrationGuid))]
        public static class LocalGamePatch
        {
            public static void Postfix(ref bool __result)
            {
                if (__result && AmongUsClient.Instance != null
                    && AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame)
                {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(CurrentModRegistration), nameof(CurrentModRegistration.UpdateFilterSetWithModRegistrationSettings))]
        public static class EnsureGuidPatch
        {
            public static void Prefix(GameFilterSet filterSet)
            {
                CurrentModRegistration.ModRegistrationGuidString = ModGuid;

                for (int i = filterSet.Filters.Count - 1; i >= 0; i--)
                {
                    if (filterSet.Filters[i].Key == "mod")
                    {
                        filterSet.Filters.RemoveAt(i);
                    }
                }
            }
        }
    }
}
