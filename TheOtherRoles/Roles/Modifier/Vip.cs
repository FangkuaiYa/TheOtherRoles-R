using System;
using System.Collections.Generic;
using UnityEngine;
using TheOtherRoles.Utilities;

namespace TheOtherRoles.Roles.Modifier
{
    public class Vip : RoleBase
    {
        public static Vip Instance;

        public static Color color = Color.yellow;
        public static RoleInfo Info = new RoleInfo("VIP", color, "You are the VIP", "Everyone is notified when you die", RoleId.Vip, false, true);

        public static List<PlayerControl> vip = new List<PlayerControl>();
        public static bool showColor = true;

        public Vip() : base()
        {
            Instance = this;
            RoleName = Info.name;
            LongDescription = Info.introDescription;
            ShortDescription = Info.shortDescription;
            RoleColor = color;
            Team = RoleTeam.Modifier;
            IsModifier = true;
        }

        public static void clearAndReload() {
            vip = new List<PlayerControl>();
            showColor = CustomOptionHolder.modifierVipShowColor.getBool();
        }

        public override void ClearAndReload() { clearAndReload(); }

        public override RoleInfo GetRoleInfo() => Info;
    }
}
