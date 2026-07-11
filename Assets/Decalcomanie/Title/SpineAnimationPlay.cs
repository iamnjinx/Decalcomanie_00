using System.Collections;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.EventSystems;

public class SpineAnimationPlay : MonoBehaviour, IPointerClickHandler
{
    public SkeletonAnimation skeletonAnimation;
    public string animationName = "animation";

    void OnEnable()
    {
        skeletonAnimation.AnimationState.Complete += HandleComplete;
    }

    void OnDisable()
    {
        skeletonAnimation.AnimationState.Complete -= HandleComplete;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        AudioManager.Instance.PlaySFX("jump");
        skeletonAnimation.AnimationState.SetAnimation(0, animationName, false);
    }

    void HandleComplete(TrackEntry trackEntry)
    {
        if (trackEntry.Animation.Name == animationName)
        {
            skeletonAnimation.AnimationState.ClearTrack(0);
            skeletonAnimation.Skeleton.SetupPose();
        }
    }
}
