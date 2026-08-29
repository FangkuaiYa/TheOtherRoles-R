using System.Collections.Generic;
using UnityEngine;

namespace TheOtherRoles.Roles
{
    public class RoleInfo
    {
        public static Dictionary<RoleId, RoleInfo> roleInfoById = new();
        public Color color;
        public string name { get { return name_ != null ? name_.GetString() : ""; } }
        public string introDescription { get { return introDescription_ != null ? introDescription_.GetString() : ""; } }
        public string shortDescription { get { return shortDescription_ != null ? shortDescription_.GetString() : ""; } }
        public bool isModifier;
        public bool isNeutral;
        TranslationInfo name_ = null;
        TranslationInfo introDescription_ = null;
        TranslationInfo shortDescription_ = null; public RoleId roleId;

        public RoleInfo(Color color, RoleId roleId,
            bool isNeutral = false, bool isModifier = false,
            TranslationInfo name = null, TranslationInfo introDescription = null, TranslationInfo shortDescription = null)
        {
            this.color = color;
            this.name_ = name != null ? name : ModTranslation.GetRoleName(roleId, color); ;
            this.introDescription_ = introDescription != null ? introDescription : ModTranslation.GetRoleIntroDesc(roleId, color);
            this.shortDescription_ = shortDescription != null ? shortDescription : ModTranslation.GetRoleShortDesc(roleId, color);
            this.roleId = roleId;
            this.isNeutral = isNeutral;
            this.isModifier = isModifier;
            roleInfoById.TryAdd(roleId, this);
        }

        public bool isImpostor => color == Palette.ImpostorRed && !(roleId == RoleId.Spy);
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
        private static int _nextId;

        protected RoleBase()
        {
            Id = _nextId++;
            CustomRoleManager.Instance.RegisterRole(this);
        }

        public int Id { get; }
        public string RoleName { get; protected set; } = "";
        public string LongDescription { get; protected set; } = "";
        public string ShortDescription { get; protected set; } = "";
        public Color RoleColor { get; protected set; }
        public RoleTeam Team { get; protected set; }
        public bool IsModifier { get; protected set; }
        public bool IsSubRole { get; protected set; }
        public bool IsVisible { get; protected set; } = true;

        public virtual RoleInfo GetRoleInfo()
        {
            return null;
        }

        public virtual void ClearAndReload()
        {
        }

        public virtual void PlayerFixedUpdate(PlayerControl player)
        {
        }

        public virtual void PlayerUpdate(PlayerControl player)
        {
        }

        public virtual void OnMeetingStart()
        {
        }

        public virtual void OnMeetingEnd()
        {
        }

        public virtual void OnPlayerExiled(PlayerControl player)
        {
        }

        public virtual void OnPlayerDeath(PlayerControl player)
        {
        }

        public virtual void OnMurderPlayer(PlayerControl killer, PlayerControl victim)
        {
        }

        public virtual bool CanUseVent(PlayerControl player, Vent vent)
        {
            return false;
        }

        public virtual bool CanKill(PlayerControl killer, PlayerControl target)
        {
            return false;
        }

        public virtual void SetTarget(PlayerControl target)
        {
        }

        public virtual void OnClickButton()
        {
        }
    }
}