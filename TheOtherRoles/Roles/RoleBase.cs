using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using TheOtherRoles.Utilities;
using TheOtherRoles.CustomGameModes;
using static TheOtherRoles.TheOtherRoles;

namespace TheOtherRoles
{
    public class RoleInfo {
        public Color color;
        public string name;
        public string introDescription;
        public string shortDescription;
        public RoleId roleId;
        public bool isNeutral;
        public bool isModifier;
        public bool isImpostor => color == Palette.ImpostorRed && !(roleId == RoleId.Spy);
        public static Dictionary<RoleId, RoleInfo> roleInfoById = new();

        public RoleInfo(string name, Color color, string introDescription, string shortDescription, RoleId roleId, bool isNeutral = false, bool isModifier = false) {
            this.color = color;
            this.name = name;
            this.introDescription = introDescription;
            this.shortDescription = shortDescription;
            this.roleId = roleId;
            this.isNeutral = isNeutral;
            this.isModifier = isModifier;
            roleInfoById.TryAdd(roleId, this);
        }
    }
}

namespace TheOtherRoles.Roles
{
    public enum RoleTeam
    {
        Crewmate,
        Impostor,
        Neutral,
        Modifier
    }

    public abstract class RoleBase
    {
        private static int _nextId = 0;

        public int Id { get; }
        public string RoleName { get; protected set; } = "";
        public string LongDescription { get; protected set; } = "";
        public string ShortDescription { get; protected set; } = "";
        public Color RoleColor { get; protected set; }
        public RoleTeam Team { get; protected set; }
        public bool IsModifier { get; protected set; }
        public bool IsSubRole { get; protected set; }
        public bool IsVisible { get; protected set; } = true;
        public virtual RoleInfo GetRoleInfo() => null;

        protected RoleBase()
        {
            Id = _nextId++;
            CustomRoleManager.Instance.RegisterRole(this);
        }

        public virtual void ClearAndReload() { }
        public virtual void PlayerFixedUpdate(PlayerControl player) { }
        public virtual void PlayerUpdate(PlayerControl player) { }
        public virtual void OnMeetingStart() { }
        public virtual void OnMeetingEnd() { }
        public virtual void OnPlayerExiled(PlayerControl player) { }
        public virtual void OnPlayerDeath(PlayerControl player) { }
        public virtual void OnMurderPlayer(PlayerControl killer, PlayerControl victim) { }
        public virtual bool CanUseVent(PlayerControl player, Vent vent) { return false; }
        public virtual bool CanKill(PlayerControl killer, PlayerControl target) { return false; }
        public virtual void SetTarget(PlayerControl target) { }
        public virtual void OnClickButton() { }
    }
}
