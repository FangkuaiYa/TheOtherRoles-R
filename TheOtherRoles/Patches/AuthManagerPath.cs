using HarmonyLib;

namespace TheOtherRoles.Patches
{
    internal class AuthManagerPath
    {
        [HarmonyPatch(typeof(AuthManager._CoConnect_d__4), "MoveNext")]
        public static class DoNothingInConnect
        {
            public static bool Prefix(AuthManager._CoConnect_d__4 __instance)
            {
                if (Helpers.isCustomServer())
                    return false;
                return true;
            }
        }

        [HarmonyPatch(typeof(AuthManager._CoWaitForNonce_d__6),"MoveNext")]
        public static class DontWaitForNonce
        {
            public static bool Prefix(AuthManager._CoWaitForNonce_d__6 __instance)
            {
                if (Helpers.isCustomServer())
                    return false;
                return true;
            }
        }
    }
}
