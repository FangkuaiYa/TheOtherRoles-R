using System.Collections.Generic;
using System.Linq;
using TheOtherRoles.Objects;
using TheOtherRoles.Patches;
using TheOtherRoles.Utilities;
using UnityEngine;

namespace TheOtherRoles.Roles.Crewmate;

public class Tracker : RoleBase
{
    public static Tracker Instance;

    public static Color color = new Color32(100, 58, 220, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.Tracker);

    public static PlayerControl tracker;
    public static List<Arrow> localArrows = new();

    public static float updateIntervall = 5f;
    public static bool resetTargetAfterMeeting;
    public static bool canTrackCorpses;
    public static float corpsesTrackingCooldown = 30f;
    public static float corpsesTrackingDuration = 5f;
    public static float corpsesTrackingTimer;
    public static int trackingMode;
    public static List<Vector3> deadBodyPositions = new();

    public static PlayerControl currentTarget;
    public static PlayerControl tracked;
    public static bool usedTracker;
    public static float timeUntilUpdate;
    public static Arrow arrow = new(Color.blue);

    public static GameObject DangerMeterParent;
    public static DangerMeter Meter;

    private static Sprite trackCorpsesButtonSprite;

    private static Sprite buttonSprite;

    public Tracker()
    {
        Instance = this;
        RoleName = Info.name;
        LongDescription = Info.introDescription;
        ShortDescription = Info.shortDescription;
        RoleColor = color;
        Team = RoleTeam.Crewmate;
    }

    public static Sprite getTrackCorpsesButtonSprite()
    {
        if (trackCorpsesButtonSprite) return trackCorpsesButtonSprite;
        trackCorpsesButtonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.PathfindButton.png", 115f);
        return trackCorpsesButtonSprite;
    }

    public static Sprite getButtonSprite()
    {
        if (buttonSprite) return buttonSprite;
        buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.TrackerButton.png", 115f);
        return buttonSprite;
    }

    public static void resetTracked()
    {
        currentTarget = tracked = null;
        usedTracker = false;
        if (arrow?.arrow != null) Object.Destroy(arrow.arrow);
        arrow = new Arrow(Color.blue);
        if (arrow.arrow != null) arrow.arrow.SetActive(false);
    }

    public static void clearAndReload()
    {
        tracker = null;
        resetTracked();
        timeUntilUpdate = 0f;
        updateIntervall = CustomOptionHolder.trackerUpdateIntervall.getFloat();
        resetTargetAfterMeeting = CustomOptionHolder.trackerResetTargetAfterMeeting.getBool();
        if (localArrows != null)
            foreach (var arrow in localArrows)
                if (arrow?.arrow != null)
                    Object.Destroy(arrow.arrow);
        deadBodyPositions = new List<Vector3>();
        corpsesTrackingTimer = 0f;
        corpsesTrackingCooldown = CustomOptionHolder.trackerCorpsesTrackingCooldown.getFloat();
        corpsesTrackingDuration = CustomOptionHolder.trackerCorpsesTrackingDuration.getFloat();
        canTrackCorpses = CustomOptionHolder.trackerCanTrackCorpses.getBool();
        trackingMode = CustomOptionHolder.trackerTrackingMethod.getSelection();
        if (DangerMeterParent)
        {
            Meter.gameObject.Destroy();
            DangerMeterParent.Destroy();
        }
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
        Tracker.corpsesTrackingTimer -= Time.deltaTime;
        // trackerSetTarget
        if (Tracker.tracker == null || Tracker.tracker != player) return;
        Tracker.currentTarget = PlayerControlFixedUpdatePatch.setTarget();
        if (!Tracker.usedTracker) PlayerControlFixedUpdatePatch.setPlayerOutline(Tracker.currentTarget, Tracker.color);

        // trackerUpdate - player tracking
        if (Tracker.arrow?.arrow != null)
        {
            if (Tracker.tracker == null || player != Tracker.tracker)
            {
                Tracker.arrow.arrow.SetActive(false);
                if (Tracker.DangerMeterParent) Tracker.DangerMeterParent.SetActive(false);
            }
            else if (Tracker.tracked != null && !Tracker.tracker.Data.IsDead)
            {
                Tracker.timeUntilUpdate -= Time.fixedDeltaTime;
                if (Tracker.timeUntilUpdate <= 0f)
                {
                    var trackedOnMap = !Tracker.tracked.Data.IsDead;
                    var position = Tracker.tracked.transform.position;
                    if (!trackedOnMap)
                    {
                        var body = Object.FindObjectsOfType<DeadBody>()
                            .FirstOrDefault(b => b.ParentId == Tracker.tracked.PlayerId);
                        if (body != null) { trackedOnMap = true; position = body.transform.position; }
                    }
                    if (Tracker.trackingMode == 1 || Tracker.trackingMode == 2) Arrow.UpdateProximity(position);
                    if (Tracker.trackingMode == 0 || Tracker.trackingMode == 2)
                    {
                        Tracker.arrow.Update(position);
                        Tracker.arrow.arrow.SetActive(trackedOnMap);
                    }
                    Tracker.timeUntilUpdate = Tracker.updateIntervall;
                }
                else
                {
                    if (Tracker.trackingMode == 0 || Tracker.trackingMode == 2) Tracker.arrow.Update();
                }
            }
            else if (Tracker.tracker.Data.IsDead)
            {
                Tracker.DangerMeterParent?.SetActive(false);
                Tracker.Meter?.gameObject.SetActive(false);
            }
        }

        // trackerUpdate - corpses tracking
        if (Tracker.tracker != null && Tracker.tracker == player &&
            Tracker.corpsesTrackingTimer >= 0f && !Tracker.tracker.Data.IsDead)
        {
            var arrowsCountChanged = Tracker.localArrows.Count != Tracker.deadBodyPositions.Count();
            var index = 0;
            if (arrowsCountChanged)
            {
                foreach (var arrow in Tracker.localArrows) Object.Destroy(arrow.arrow);
                Tracker.localArrows = new List<Arrow>();
            }
            foreach (var position in Tracker.deadBodyPositions)
            {
                if (arrowsCountChanged)
                {
                    Tracker.localArrows.Add(new Arrow(Tracker.color));
                    Tracker.localArrows[index].arrow.SetActive(true);
                }
                if (Tracker.localArrows[index] != null) Tracker.localArrows[index].Update(position);
                index++;
            }
        }
        else if (Tracker.localArrows.Count > 0)
        {
            foreach (var arrow in Tracker.localArrows) Object.Destroy(arrow.arrow);
            Tracker.localArrows = new List<Arrow>();
        }
    }
}