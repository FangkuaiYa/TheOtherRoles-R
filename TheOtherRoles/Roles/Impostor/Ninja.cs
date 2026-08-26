using System;
using System.Collections.Generic;
using UnityEngine;
using TheOtherRoles.Utilities;
using TheOtherRoles.Objects;

namespace TheOtherRoles.Roles.Impostor
{
    public class Ninja : RoleBase
    {
        public static Ninja Instance;

        public static Color color = Palette.ImpostorRed;
        public static RoleInfo Info = new RoleInfo("Ninja", color, "Surprise and assassinate your foes", "Surprise and assassinate your foes", RoleId.Ninja);

        public static PlayerControl ninja;
        public static PlayerControl ninjaMarked;
        public static PlayerControl currentTarget;
        public static float cooldown = 30f;
        public static float traceTime = 1f;
        public static bool knowsTargetLocation = false;
        public static float invisibleDuration = 5f;

        public static float invisibleTimer = 0f;
        public static bool isInvisble = false;
        private static Sprite markButtonSprite;
        private static Sprite killButtonSprite;
        public static Arrow arrow = new Arrow(Color.black);

        public static Sprite getMarkButtonSprite()
        {
            if (markButtonSprite) return markButtonSprite;
            markButtonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.NinjaMarkButton.png", 115f);
            return markButtonSprite;
        }

        public static Sprite getKillButtonSprite()
        {
            if (killButtonSprite) return killButtonSprite;
            killButtonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.NinjaAssassinateButton.png", 115f);
            return killButtonSprite;
        }

        public Ninja() : base()
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
            ninja = null;
            currentTarget = ninjaMarked = null;
            cooldown = CustomOptionHolder.ninjaCooldown.getFloat();
            knowsTargetLocation = CustomOptionHolder.ninjaKnowsTargetLocation.getBool();
            traceTime = CustomOptionHolder.ninjaTraceTime.getFloat();
            invisibleDuration = CustomOptionHolder.ninjaInvisibleDuration.getFloat();
            invisibleTimer = 0f;
            isInvisble = false;
            if (arrow?.arrow != null) UnityEngine.Object.Destroy(arrow.arrow);
            arrow = new Arrow(Color.black);
            if (arrow.arrow != null) arrow.arrow.SetActive(false);
        }

        public override void ClearAndReload()
        {
            clearAndReload();
        }

        public override RoleInfo GetRoleInfo() => Info;
    }
}
