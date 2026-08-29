using System.Collections.Generic;
using Hazel;
using TheOtherRoles.Patches;
using UnityEngine;

namespace TheOtherRoles.Roles.Crewmate;

public class Deputy : RoleBase
{
    public static Deputy Instance;

    public static Color color = Sheriff.color;

    public static RoleInfo Info = new(color, RoleId.Deputy);

    public static PlayerControl deputy;

    public static PlayerControl currentTarget;
    public static List<byte> handcuffedPlayers = new();
    public static int promotesToSheriff;
    public static bool keepsHandcuffsOnPromotion;
    public static float handcuffDuration;
    public static float remainingHandcuffs;
    public static float handcuffCooldown;
    public static bool knowsSheriff;
    public static Dictionary<byte, float> handcuffedKnows = new();

    private static Sprite buttonSprite;
    private static Sprite handcuffedSprite;

    public Deputy()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Crewmate;
    }

    public static Sprite getButtonSprite()
    {
        if (buttonSprite) return buttonSprite;
        buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.DeputyHandcuffButton.png", 115f);
        return buttonSprite;
    }

    public static Sprite getHandcuffedButtonSprite()
    {
        if (handcuffedSprite) return handcuffedSprite;
        handcuffedSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.DeputyHandcuffed.png", 115f);
        return handcuffedSprite;
    }

    public static void setHandcuffedKnows(bool active = true, byte playerId = byte.MaxValue)
    {
        if (playerId == byte.MaxValue)
            playerId = PlayerControl.LocalPlayer.PlayerId;

        if (active && playerId == PlayerControl.LocalPlayer.PlayerId)
        {
            var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                (byte)CustomRPC.ShareGhostInfo, SendOption.Reliable);
            writer.Write(PlayerControl.LocalPlayer.PlayerId);
            writer.Write((byte)RPCProcedure.GhostInfoTypes.HandcuffNoticed);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        if (active)
        {
            handcuffedKnows.Add(playerId, handcuffDuration);
            handcuffedPlayers.RemoveAll(x => x == playerId);
        }

        if (playerId == PlayerControl.LocalPlayer.PlayerId)
        {
            HudManagerStartPatch.setAllButtonsHandcuffedStatus(active);
            SoundEffectsManager.play("deputyHandcuff");
        }
    }

    public static void clearAndReload()
    {
        deputy = null;
        currentTarget = null;
        handcuffedPlayers = new List<byte>();
        handcuffedKnows = new Dictionary<byte, float>();
        HudManagerStartPatch.setAllButtonsHandcuffedStatus(false, true);
        promotesToSheriff = CustomOptionHolder.deputyGetsPromoted.getSelection();
        remainingHandcuffs = CustomOptionHolder.deputyNumberOfHandcuffs.getFloat();
        handcuffCooldown = CustomOptionHolder.deputyHandcuffCooldown.getFloat();
        keepsHandcuffsOnPromotion = CustomOptionHolder.deputyKeepsHandcuffs.getBool();
        handcuffDuration = CustomOptionHolder.deputyHandcuffDuration.getFloat();
        knowsSheriff = CustomOptionHolder.deputyKnowsSheriff.getBool();
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    public override RoleInfo GetRoleInfo()
    {
        return Info;
    }

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        var keys = new List<byte>(Deputy.handcuffedKnows.Keys);
        foreach (var key in keys)
            Deputy.handcuffedKnows[key] -= Time.deltaTime;
        // deputySetTarget
        if (Deputy.deputy == null || Deputy.deputy != player) return;
        Deputy.currentTarget = PlayerControlFixedUpdatePatch.setTarget();
        PlayerControlFixedUpdatePatch.setPlayerOutline(Deputy.currentTarget, Deputy.color);

        // deputyUpdate
        if (PlayerControl.LocalPlayer == null ||
            !Deputy.handcuffedKnows.ContainsKey(PlayerControl.LocalPlayer.PlayerId)) return;
        if (Deputy.handcuffedKnows[PlayerControl.LocalPlayer.PlayerId] <= 0)
        {
            Deputy.handcuffedKnows.Remove(PlayerControl.LocalPlayer.PlayerId);
            Deputy.setHandcuffedKnows(false);
            var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                (byte)CustomRPC.ShareGhostInfo, SendOption.Reliable);
            writer.Write(PlayerControl.LocalPlayer.PlayerId);
            writer.Write((byte)RPCProcedure.GhostInfoTypes.HandcuffOver);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        // deputyCheckPromotion
        if (Deputy.promotesToSheriff == 0 || Deputy.deputy.Data.IsDead ||
            (Deputy.promotesToSheriff == 2 && false)) return; // isMeeting always false in FixedUpdate
        if (Sheriff.sheriff == null || Sheriff.sheriff?.Data?.Disconnected == true || Sheriff.sheriff.Data.IsDead)
        {
            var writer2 = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                (byte)CustomRPC.DeputyPromotes, SendOption.Reliable);
            AmongUsClient.Instance.FinishRpcImmediately(writer2);
            RPCProcedure.deputyPromotes();
        }
    }
}