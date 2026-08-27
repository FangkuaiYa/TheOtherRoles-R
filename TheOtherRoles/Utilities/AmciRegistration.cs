using AmongUs.GameOptions;
using AmongUs.Matchmaking;
using HarmonyLib;
using InnerNet;
using TheOtherRoles.Patches;

namespace TheOtherRoles.Utilities;

public static class AmciRegistration
{
    public static string ModGuid { get; private set; } = "";

    public static void Register()
    {
        ModGuid = GameStartManagerPatch.ModConstantGuid;
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

    [HarmonyPatch(typeof(CurrentModRegistration),
        nameof(CurrentModRegistration.UpdateFilterSetWithModRegistrationSettings))]
    public static class EnsureGuidPatch
    {
        public static void Prefix(GameFilterSet filterSet)
        {
            CurrentModRegistration.ModRegistrationGuidString = ModGuid;

            for (var i = filterSet.Filters.Count - 1; i >= 0; i--)
                if (filterSet.Filters[i].Key == "mod")
                    filterSet.Filters.RemoveAt(i);
        }
    }

    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.HostGame), typeof(IGameOptions),
        typeof(GameFilterOptions))]
    public static class LocalGamePatch
    {
        private static string _savedGuid;

        public static void Prefix()
        {
            if (AmongUsClient.Instance != null
                && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
            {
                _savedGuid = CurrentModRegistration.ModRegistrationGuidString;
                CurrentModRegistration.ModRegistrationGuidString = "";
            }
        }

        public static void Postfix()
        {
            if (_savedGuid != null)
            {
                CurrentModRegistration.ModRegistrationGuidString = _savedGuid;
                _savedGuid = null;
            }
        }
    }
}