using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kinect = Windows.Kinect;

public class BodySourceView : MonoBehaviour {
  /// TEST WORKING
  public Vector3 leftShoulderAim;
  public Vector3 rightShoulderAim;
  public Vector3 leftElbowAim;
  public Vector3 rightElbowAim;

  /// END TEST WORKING

  public GameObject bodyObject;

  public Material BoneMaterial;
  public GameObject BodySourceManager;

  public Quaternion[] jointRotations;

  private Dictionary<ulong, GameObject> _Bodies = new Dictionary<ulong, GameObject>();
  private BodySourceManager _BodyManager;

  private Dictionary<Kinect.JointType, Kinect.JointType> _BoneMap = new Dictionary<Kinect.JointType, Kinect.JointType>() { { Kinect.JointType.FootLeft, Kinect.JointType.AnkleLeft }, { Kinect.JointType.AnkleLeft, Kinect.JointType.KneeLeft }, { Kinect.JointType.KneeLeft, Kinect.JointType.HipLeft }, { Kinect.JointType.HipLeft, Kinect.JointType.SpineBase },

    { Kinect.JointType.FootRight, Kinect.JointType.AnkleRight }, { Kinect.JointType.AnkleRight, Kinect.JointType.KneeRight }, { Kinect.JointType.KneeRight, Kinect.JointType.HipRight }, { Kinect.JointType.HipRight, Kinect.JointType.SpineBase },

    { Kinect.JointType.HandTipLeft, Kinect.JointType.HandLeft }, { Kinect.JointType.ThumbLeft, Kinect.JointType.HandLeft }, { Kinect.JointType.HandLeft, Kinect.JointType.WristLeft }, { Kinect.JointType.WristLeft, Kinect.JointType.ElbowLeft }, { Kinect.JointType.ElbowLeft, Kinect.JointType.ShoulderLeft }, { Kinect.JointType.ShoulderLeft, Kinect.JointType.SpineShoulder },

    { Kinect.JointType.HandTipRight, Kinect.JointType.HandRight }, { Kinect.JointType.ThumbRight, Kinect.JointType.HandRight }, { Kinect.JointType.HandRight, Kinect.JointType.WristRight }, { Kinect.JointType.WristRight, Kinect.JointType.ElbowRight }, { Kinect.JointType.ElbowRight, Kinect.JointType.ShoulderRight }, { Kinect.JointType.ShoulderRight, Kinect.JointType.SpineShoulder },

    { Kinect.JointType.SpineBase, Kinect.JointType.SpineMid }, { Kinect.JointType.SpineMid, Kinect.JointType.SpineShoulder }, { Kinect.JointType.SpineShoulder, Kinect.JointType.Neck }, { Kinect.JointType.Neck, Kinect.JointType.Head },
  };

  void Start() {
    jointRotations = new Quaternion[30];
  }

  void Update() {
    if (BodySourceManager == null) {
      return;
    }

    _BodyManager = BodySourceManager.GetComponent<BodySourceManager>();
    if (_BodyManager == null) {
      return;
    }

    Kinect.Body[] data = _BodyManager.GetData();
    if (data == null) {
      return;
    }

    List<ulong> trackedIds = new List<ulong>();
    foreach (var body in data) {
      if (body == null) {
        continue;
      }

      if (body.IsTracked) {
        trackedIds.Add(body.TrackingId);
      }
    }

    List<ulong> knownIds = new List<ulong>(_Bodies.Keys);

    // First delete untracked bodies
    foreach (ulong trackingId in knownIds) {
      if (!trackedIds.Contains(trackingId)) {
        Destroy(_Bodies[trackingId]);
        _Bodies.Remove(trackingId);
      }
    }

    foreach (var body in data) {
      if (body == null) {
        continue;
      }

      if (body.IsTracked) {
        // here is the beefff

        if (!_Bodies.ContainsKey(body.TrackingId)) {
          // instants a new body obj, I prob wont need, since I'm mapping to existing body 
          _Bodies[body.TrackingId] = CreateBodyObject(body.TrackingId);
        }
        GameObject bb = _Bodies[body.TrackingId];

        /// updates positions of joints of body obj
        RefreshBodyObject(body, bb);

        // WORKIN TEST

        Transform shoulderLeft = bb.transform.Find(Kinect.JointType.ShoulderLeft.ToString()).transform;
        Transform elbowLeft = bb.transform.Find(Kinect.JointType.ElbowLeft.ToString()).transform;
        Transform wristLeft = bb.transform.Find(Kinect.JointType.WristLeft.ToString()).transform;

        leftShoulderAim = elbowLeft.position - shoulderLeft.position;
        leftElbowAim = wristLeft.position - elbowLeft.position;

        Transform shoulderRight = bb.transform.Find(Kinect.JointType.ShoulderRight.ToString()).transform;
        Transform elbowRight = bb.transform.Find(Kinect.JointType.ElbowRight.ToString()).transform;
        Transform wristRight = bb.transform.Find(Kinect.JointType.WristRight.ToString()).transform;

        rightShoulderAim = Vector3.Normalize(elbowRight.position - shoulderRight.position);
        rightElbowAim = Vector3.Normalize(wristRight.position - elbowRight.position);

        // Debug.DrawLine(shoulderRight.position, shoulderRight.position + (2f * rightShoulderAim), Color.cyan);
        // Debug.DrawLine(elbowRight.position, elbowRight.position + (2f * rightElbowAim), Color.cyan);

        // driving puppet

      }
    }
  }

  private GameObject CreateBodyObject(ulong id) {
    GameObject body = new GameObject("Body:" + id);

    for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++) {
      GameObject jointObj = GameObject.CreatePrimitive(PrimitiveType.Cube);

      LineRenderer lr = jointObj.AddComponent<LineRenderer>();
      lr.SetVertexCount(2);
      lr.material = BoneMaterial;
      lr.SetWidth(0.05f, 0.05f);

      jointObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
      jointObj.name = jt.ToString();
      jointObj.transform.parent = body.transform;
    }

    return body;
  }

  private void RefreshBodyObject(Kinect.Body body, GameObject bodyGuy) {
    for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++) {
      Kinect.Joint sourceJoint = body.Joints[jt];
      Kinect.Joint? targetJoint = null;

      if (_BoneMap.ContainsKey(jt)) {
        targetJoint = body.Joints[_BoneMap[jt]];
      }

      // position the joints of the actual driven body
      Transform jointObj = bodyGuy.transform.Find(jt.ToString()).transform;
      jointObj.localPosition = GetVector3FromJoint(sourceJoint);

      LineRenderer lr = jointObj.GetComponent<LineRenderer>();

      if (targetJoint.HasValue) {
        // source joint look dir
        Vector3 lookDir = GetVector3FromJoint(targetJoint.Value) - GetVector3FromJoint(sourceJoint);

        jointRotations[(int) jt] = Quaternion.LookRotation(GetVector3FromJoint(targetJoint.Value) - GetVector3FromJoint(sourceJoint));

        lr.SetPosition(0, jointObj.localPosition);
        lr.SetPosition(1, jointObj.localPosition + lookDir.normalized);
        lr.SetColors(GetColorForState(sourceJoint.TrackingState), GetColorForState(targetJoint.Value.TrackingState));
      } else {
        lr.enabled = false;
      }
    }
  }

  private static Color GetColorForState(Kinect.TrackingState state) {
    switch (state) {
      case Kinect.TrackingState.Tracked:
        return Color.green;

      case Kinect.TrackingState.Inferred:
        return Color.red;

      default:
        return Color.black;
    }
  }

  private static Vector3 GetVector3FromJoint(Kinect.Joint joint) {
    return new Vector3(joint.Position.X * 10, joint.Position.Y * 10, joint.Position.Z * 10);
  }
}