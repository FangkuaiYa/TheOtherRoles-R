using System;
using System.Collections.Generic;
using UnityEngine;
using TheOtherRoles.Utilities;

namespace TheOtherRoles.Roles.Impostor
{
    public class Yoyo : RoleBase
    {
        public static Yoyo Instance;

        public static Color color = Palette.ImpostorRed;
        public static RoleInfo Info = new RoleInfo("Yo-Yo", color, "Blink to a marked location and Back", "Blink to a location", RoleId.Yoyo);

        public static PlayerControl yoyo = null;
        public static float blinkDuration = 0;
        public static float markCooldown = 0;
        public static bool markStaysOverMeeting = false;
        public static bool hasAdminTable = false;
        public static float adminCooldown = 0;
        public static float SilhouetteVisibility => (silhouetteVisibility == 0 && (PlayerControl.LocalPlayer == yoyo || PlayerControl.LocalPlayer.Data.IsDead)) ? 0.1f : silhouetteVisibility;
        public static float silhouetteVisibility = 0;

        public static Vector3? markedLocation = null;

        private static Sprite markButtonSprite;

        public static Sprite getMarkButtonSprite()
        {
            if (markButtonSprite) return markButtonSprite;
            markButtonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.YoyoMarkButtonSprite.png", 115f);
            return markButtonSprite;
        }
        private static Sprite blinkButtonSprite;

        public static Sprite getBlinkButtonSprite()
        {
            if (blinkButtonSprite) return blinkButtonSprite;
            blinkButtonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.YoyoBlinkButtonSprite.png", 115f);
            return blinkButtonSprite;
        }

        public static void markLocation(Vector3 position)
        {
            markedLocation = position;
        }

        public Yoyo() : base()
        {
            Instance = this;
            RoleName = Info.name;
            LongDescription = Info.introDescription;
            ShortDescription = Info.shortDescription;
            RoleColor = color;
            Team = RoleTeam.Impostor;
        }

        public static void clearAndReload()
        {
            blinkDuration = CustomOptionHolder.yoyoBlinkDuration.getFloat();
            markCooldown = CustomOptionHolder.yoyoMarkCooldown.getFloat();
            markStaysOverMeeting = CustomOptionHolder.yoyoMarkStaysOverMeeting.getBool();
            hasAdminTable = CustomOptionHolder.yoyoHasAdminTable.getBool();
            adminCooldown = CustomOptionHolder.yoyoAdminTableCooldown.getFloat();
            silhouetteVisibility = CustomOptionHolder.yoyoSilhouetteVisibility.getSelection() / 10f;

            markedLocation = null;
        }

        public override void ClearAndReload()
        {
            clearAndReload();
        }

        public override RoleInfo GetRoleInfo() => Info;
    }
}
