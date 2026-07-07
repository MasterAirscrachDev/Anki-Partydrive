using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityGrappler : MonoBehaviour, IWorldEntity
{
    Transform targetTransform;
    string targetID;
    string userID;
    [SerializeField] LineRenderer grapplerLine;
    bool abilityActive = false;

    string _entityId;
    public string EntityId => _entityId;
    public string EntityType => "AbilityGrappler";

    void Awake()
    {
        _entityId = Guid.NewGuid().ToString();
    }

    void Start()
    {
        SR.wes?.Register(this);
    }

    void OnDestroy()
    {
        SR.wes?.Unregister(_entityId);
    }

    public WorldEntityData GetEntityData()
    {
        var extras = new Dictionary<string, string>
        {
            ["userId"] = userID ?? "",
            ["targetId"] = targetID ?? ""
        };
        if (abilityActive && targetTransform != null)
        {
            Vector3 uPos = SR.cet.GetCarVisualPosition(userID);
            Vector3 tPos = targetTransform.position;
            extras["ux"] = uPos.x.ToString("F3");
            extras["uy"] = uPos.y.ToString("F3");
            extras["uz"] = uPos.z.ToString("F3");
            extras["tx"] = tPos.x.ToString("F3");
            extras["ty"] = tPos.y.ToString("F3");
            extras["tz"] = tPos.z.ToString("F3");
        }
        return WorldEntityData.Build(_entityId, EntityType, transform, userID, extras);
    }

    public void Setup(Transform target, string targetCarID, string userCarID)
    {
        targetTransform = target;
        targetID = targetCarID;
        userID = userCarID;

        if(grapplerLine != null)
        {
            grapplerLine.positionCount = 2;
        }
        OnHitTarget();
    }
    void Update()
    {
        if(targetTransform == null || !abilityActive)
        {
            Destroy(gameObject);
            return;
        }

        if(HasUserOvertakenTarget())
        {
            EndAbilityEarly();
            return;
        }

        if(grapplerLine != null)
        {
            Vector3 userPos = SR.cet.GetCarVisualPosition(userID);
            Vector3 targetPos = targetTransform.position;

            grapplerLine.SetPosition(0, userPos);
            grapplerLine.SetPosition(1, targetPos);
        }
    }

    bool HasUserOvertakenTarget()
    {
        TrackCoordinate userCoord = SR.cet.GetCarTrackCoordinate(userID);
        TrackCoordinate targetCoord = SR.cet.GetCarTrackCoordinate(targetID);

        if(userCoord == null || targetCoord == null) return false;

        double userPosition = (userCoord.idx * 1000.0) + userCoord.progression;
        double targetPosition = (targetCoord.idx * 1000.0) + targetCoord.progression;

        return userPosition > targetPosition;
    }

    void EndAbilityEarly()
    {
        abilityActive = false;

        CarController userCar = SR.cms.GetController(userID);
        CarController targetCar = SR.cms.GetController(targetID);
        if(userCar != null) {
            userCar.AddSpeedModifier(new PercentSpeedModifier(1, 0, "GrapplerBoost"));
        }
        if(targetCar != null) {
            targetCar.AddSpeedModifier(new PercentSpeedModifier(1, 0, "GrapplerSlow"));
        }

        Destroy(gameObject);
    }

    void OnHitTarget()
    {
        CarController userCar = SR.cms.GetController(userID);
        CarController targetCar = SR.cms.GetController(targetID);

        if(userCar != null && targetCar != null)
        {
            abilityActive = true;
            userCar.AddSpeedModifier(new PercentSpeedModifier(1.2f, 8f, "GrapplerBoost"));
            targetCar.AddSpeedModifier(new PercentSpeedModifier(0.7f, 8f, "GrapplerSlow"));
            StartCoroutine(DestroyAfterDuration(8f));
        } else { Destroy(gameObject); }
    }
    IEnumerator DestroyAfterDuration(float duration) {
        yield return new WaitForSeconds(duration);
        abilityActive = false;
        Destroy(gameObject);
    }
}
