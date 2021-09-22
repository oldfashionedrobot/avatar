using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kinect = Windows.Kinect;

public class PuppetAvatar : MonoBehaviour {
  private BodySourceView _BodyManager;
  // public TrackerHandler KinectDevice;
  Dictionary<Kinect.JointType, Quaternion> absoluteOffsetMap;
  Animator PuppetAnimator;
  public GameObject RootPosition;
  public float OffsetY;
  public float OffsetZ;
  private static HumanBodyBones MapKinectJoint(Kinect.JointType joint) {
    // https://docs.microsoft.com/en-us/azure/Kinect-dk/body-joints
    switch (joint) {
      case Kinect.JointType.SpineBase:
        return HumanBodyBones.Hips;
      case Kinect.JointType.SpineMid:
        return HumanBodyBones.Spine;
      case Kinect.JointType.SpineShoulder:
        return HumanBodyBones.Chest;
      case Kinect.JointType.Neck:
        return HumanBodyBones.Neck;
      case Kinect.JointType.Head:
        return HumanBodyBones.Head;
      case Kinect.JointType.HipLeft:
        return HumanBodyBones.LeftUpperLeg;
      case Kinect.JointType.KneeLeft:
        return HumanBodyBones.LeftLowerLeg;
      case Kinect.JointType.AnkleLeft:
        return HumanBodyBones.LeftFoot;
      case Kinect.JointType.FootLeft:
        return HumanBodyBones.LeftToes;
      case Kinect.JointType.HipRight:
        return HumanBodyBones.RightUpperLeg;
      case Kinect.JointType.KneeRight:
        return HumanBodyBones.RightLowerLeg;
      case Kinect.JointType.AnkleRight:
        return HumanBodyBones.RightFoot;
      case Kinect.JointType.FootRight:
        return HumanBodyBones.RightToes;
        // case Kinect.JointType.ClavicleLeft:
        //   return HumanBodyBones.LeftShoulder;
      case Kinect.JointType.ShoulderLeft:
        return HumanBodyBones.LeftUpperArm;
      case Kinect.JointType.ElbowLeft:
        return HumanBodyBones.LeftLowerArm;
      case Kinect.JointType.WristLeft:
        return HumanBodyBones.LeftHand;
        // case Kinect.JointType.ClavicleRight:
        //   return HumanBodyBones.RightShoulder;
      case Kinect.JointType.ShoulderRight:
        return HumanBodyBones.RightUpperArm;
      case Kinect.JointType.ElbowRight:
        return HumanBodyBones.RightLowerArm;
      case Kinect.JointType.WristRight:
        return HumanBodyBones.RightHand;
      default:
        return HumanBodyBones.LastBone;
    }
  }
  private void Start() {
    _BodyManager = GameObject.Find("BodyView").GetComponent<BodySourceView>();
    PuppetAnimator = GetComponent<Animator>();
    Transform _rootJointTransform = PuppetAnimator.GetBoneTransform(HumanBodyBones.Hips);

    absoluteOffsetMap = new Dictionary<Kinect.JointType, Quaternion>();

    for (int i = 0; i < (int) Enum.GetNames(typeof(Kinect.JointType)).Length; i++) {
      HumanBodyBones hbb = MapKinectJoint((Kinect.JointType) i);
      if (hbb != HumanBodyBones.LastBone) {
        Transform transform = PuppetAnimator.GetBoneTransform(hbb);
        Quaternion absOffset = GetSkeletonBone(PuppetAnimator, transform.name).rotation;
        // find the absolute offset for the tpose
        while (!ReferenceEquals(transform, _rootJointTransform)) {
          transform = transform.parent;
          absOffset = GetSkeletonBone(PuppetAnimator, transform.name).rotation * absOffset;
        }
        absoluteOffsetMap[(Kinect.JointType) i] = absOffset;
      }
    }
  }

  private static SkeletonBone GetSkeletonBone(Animator animator, string boneName) {
    int count = 0;
    foreach (SkeletonBone sb in animator.avatar.humanDescription.skeleton) {
      if (sb.name == boneName) {
        return animator.avatar.humanDescription.skeleton[count];
      }
      count++;
    }
    return new SkeletonBone();
  }

  // Update is called once per frame
  private void LateUpdate() {
    for (int j = 0; j < (int) Enum.GetNames(typeof(Kinect.JointType)).Length; j++) {
      if (MapKinectJoint((Kinect.JointType) j) != HumanBodyBones.LastBone && absoluteOffsetMap.ContainsKey((Kinect.JointType) j)) {
        // get the absolute offset
        Quaternion absOffset = absoluteOffsetMap[(Kinect.JointType) j];
        Transform finalJoint = PuppetAnimator.GetBoneTransform(MapKinectJoint((Kinect.JointType) j));
        try {

          finalJoint.rotation = absOffset * Quaternion.Inverse(absOffset) * _BodyManager.jointRotations[j] * absOffset;
        } catch {

        }

        if (j == 0) {
          finalJoint.localPosition = new Vector3(RootPosition.transform.localPosition.x, RootPosition.transform.localPosition.y + OffsetY, RootPosition.transform.localPosition.z - OffsetZ);
        }
      }
    }
  }
}