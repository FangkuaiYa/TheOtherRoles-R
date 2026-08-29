using TheOtherRoles.Patches;
using UnityEngine;

namespace TheOtherRoles.Roles.Modifier;

public class Shifter : RoleBase
{
    public static Shifter Instance;

    public static Color color = Color.yellow;

    public static RoleInfo Info = new(color, RoleId.Shifter, isModifier: true);

    public static PlayerControl shifter;

    public static PlayerControl futureShift;
    public static PlayerControl currentTarget;

    public static bool shiftsMedicShield;

    private static Sprite buttonSprite;

    public Shifter()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Modifier;
        IsModifier = true;
    }

    public static Sprite getButtonSprite()
    {
        if (buttonSprite) return buttonSprite;
        buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.ShiftButton.png", 115f);
        return buttonSprite;
    }

    public static void shiftRole(PlayerControl player1, PlayerControl player2, bool repeat = true)
    {
        if (Mayor.mayor != null && Mayor.mayor == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Mayor.mayor = player1;
        }
        else if (Portalmaker.portalmaker != null && Portalmaker.portalmaker == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Portalmaker.portalmaker = player1;
        }
        else if (Engineer.engineer != null && Engineer.engineer == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Engineer.engineer = player1;
        }
        else if (Sheriff.sheriff != null && Sheriff.sheriff == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            if (Sheriff.formerDeputy != null && Sheriff.formerDeputy == Sheriff.sheriff) Sheriff.formerDeputy = player1;
            Sheriff.sheriff = player1;
        }
        else if (Deputy.deputy != null && Deputy.deputy == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Deputy.deputy = player1;
        }
        else if (Lighter.lighter != null && Lighter.lighter == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Lighter.lighter = player1;
        }
        else if (Detective.detective != null && Detective.detective == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Detective.detective = player1;
        }
        else if (TimeMaster.timeMaster != null && TimeMaster.timeMaster == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            TimeMaster.timeMaster = player1;
        }
        else if (Medic.medic != null && Medic.medic == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Medic.medic = player1;
            if (Medic.shielded != null && Medic.shielded == player1 && shiftsMedicShield)
                Medic.shielded = player2;
        }
        else if (Swapper.swapper != null && Swapper.swapper == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Swapper.swapper = player1;
        }
        else if (Seer.seer != null && Seer.seer == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Seer.seer = player1;
        }
        else if (Hacker.hacker != null && Hacker.hacker == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Hacker.hacker = player1;
        }
        else if (Tracker.tracker != null && Tracker.tracker == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Tracker.tracker = player1;
        }
        else if (Snitch.snitch != null && Snitch.snitch == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Snitch.snitch = player1;
        }
        else if (Spy.spy != null && Spy.spy == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Spy.spy = player1;
        }
        else if (SecurityGuard.securityGuard != null && SecurityGuard.securityGuard == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            SecurityGuard.securityGuard = player1;
        }
        else if (Guesser.niceGuesser != null && Guesser.niceGuesser == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Guesser.niceGuesser = player1;
        }
        else if (Medium.medium != null && Medium.medium == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Medium.medium = player1;
        }
        else if (Pursuer.pursuer != null && Pursuer.pursuer == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Pursuer.pursuer = player1;
        }
        else if (Trapper.trapper != null && Trapper.trapper == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Trapper.trapper = player1;
        }
    }

    public static void clearAndReload()
    {
        shifter = null;
        currentTarget = null;
        futureShift = null;
        shiftsMedicShield = CustomOptionHolder.modifierShifterShiftsMedicShield.getBool();
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
        if (Shifter.shifter == null || Shifter.shifter != player) return;
        Shifter.currentTarget = PlayerControlFixedUpdatePatch.setTarget();
        if (Shifter.futureShift == null) PlayerControlFixedUpdatePatch.setPlayerOutline(Shifter.currentTarget, Color.yellow);
    }
}