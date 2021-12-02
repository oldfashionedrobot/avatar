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

  public Vector3 leanDirection;

  public Vector3 spineBaseAim;
  public Vector3 spineMidAim;
  public Vector3 spineTopAim;

  /// END TEST WORKING

  public Material BoneMaterial;
  public GameObject BodySourceManager;

  private Dictionary<ulong, GameObject> _Bodies = new Dictionary<ulong, GameObject>();
  private BodySourceManager _BodyManager;

  private Dictionary<Kinect.JointType, Kinect.JointType> _BoneMap = new Dictionary<Kinect.JointType, Kinect.JointType>() { { Kinect.JointType.FootLeft, Kinect.JointType.AnkleLeft }, { Kinect.JointType.AnkleLeft, Kinect.JointType.KneeLeft }, { Kinect.JointType.KneeLeft, Kinect.JointType.HipLeft }, { Kinect.JointType.HipLeft, Kinect.JointType.SpineBase },

    { Kinect.JointType.FootRight, Kinect.JointType.AnkleRight }, { Kinect.JointType.AnkleRight, Kinect.JointType.KneeRight }, { Kinect.JointType.KneeRight, Kinect.JointType.HipRight }, { Kinect.JointType.HipRight, Kinect.JointType.SpineBase },

    { Kinect.JointType.HandTipLeft, Kinect.JointType.HandLeft }, { Kinect.JointType.ThumbLeft, Kinect.JointType.HandLeft }, { Kinect.JointType.HandLeft, Kinect.JointType.WristLeft }, { Kinect.JointType.WristLeft, Kinect.JointType.ElbowLeft }, { Kinect.JointType.ElbowLeft, Kinect.JointType.ShoulderLeft }, { Kinect.JointType.ShoulderLeft, Kinect.JointType.SpineShoulder },

    { Kinect.JointType.HandTipRight, Kinect.JointType.HandRight }, { Kinect.JointType.ThumbRight, Kinect.JointType.HandRight }, { Kinect.JointType.HandRight, Kinect.JointType.WristRight }, { Kinect.JointType.WristRight, Kinect.JointType.ElbowRight }, { Kinect.JointType.ElbowRight, Kinect.JointType.ShoulderRight }, { Kinect.JointType.ShoulderRight, Kinect.JointType.SpineShoulder },

    { Kinect.JointType.SpineBase, Kinect.JointType.SpineMid }, { Kinect.JointType.SpineMid, Kinect.JointType.SpineShoulder }, { Kinect.JointType.SpineShoulder, Kinect.JointType.Neck }, { Kinect.JointType.Neck, Kinect.JointType.Head },
  };

  void Start() {}

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
        // TODO: This stuff works, need to move it all into my own Body Manager script

        Transform shoulderLeft = bb.transform.Find(Kinect.JointType.ShoulderLeft.ToString()).transform;
        Transform elbowLeft = bb.transform.Find(Kinect.JointType.ElbowLeft.ToString()).transform;
        Transform wristLeft = bb.transform.Find(Kinect.JointType.WristLeft.ToString()).transform;

        // NOTE: this will make the arms be placed relative to the shoulder pos
        Kinect.JointOrientation leftShoulderOri = body.JointOrientations[Kinect.JointType.ShoulderLeft];
        shoulderLeft.forward = new Quaternion(
          leftShoulderOri.Orientation.X,
          leftShoulderOri.Orientation.Y,
          leftShoulderOri.Orientation.Z,
          leftShoulderOri.Orientation.W
        ) * -transform.forward;

        leftShoulderAim = Vector3.Normalize(elbowLeft.position - shoulderLeft.position);
        leftElbowAim = Vector3.Normalize(wristLeft.position - elbowLeft.position);

        leftShoulderAim = shoulderLeft.InverseTransformDirection(leftShoulderAim);
        leftElbowAim = shoulderLeft.InverseTransformDirection(leftElbowAim);

        Transform shoulderRight = bb.transform.Find(Kinect.JointType.ShoulderRight.ToString()).transform;
        Transform elbowRight = bb.transform.Find(Kinect.JointType.ElbowRight.ToString()).transform;
        Transform wristRight = bb.transform.Find(Kinect.JointType.WristRight.ToString()).transform;

        // NOTE: this will make the arms be placed relative to the shoulder pos
        Kinect.JointOrientation rightShoulderOri = body.JointOrientations[Kinect.JointType.ShoulderRight];
        shoulderRight.forward = new Quaternion(
          rightShoulderOri.Orientation.X,
          rightShoulderOri.Orientation.Y,
          rightShoulderOri.Orientation.Z,
          rightShoulderOri.Orientation.W
        ) * -transform.forward;

        rightShoulderAim = shoulderRight.InverseTransformDirection(
          Vector3.Normalize(elbowRight.position - shoulderRight.position)
        );
        rightElbowAim = shoulderRight.InverseTransformDirection(
          Vector3.Normalize(wristRight.position - elbowRight.position)
        );

        // trying body lean values
        leanDirection = new Vector3(body.Lean.X, 0f, body.Lean.Y);

        Transform spineBase = bb.transform.Find(Kinect.JointType.SpineBase.ToString()).transform;
        Transform spineMid = bb.transform.Find(Kinect.JointType.SpineMid.ToString()).transform;
        Transform spineShoulder = bb.transform.Find(Kinect.JointType.SpineShoulder.ToString()).transform;

        spineBaseAim = Vector3.Normalize(spineMid.position - spineBase.position);
        spineMidAim = Vector3.Normalize(spineShoulder.position - spineMid.position);

        Kinect.JointOrientation spineOri = body.JointOrientations[Kinect.JointType.SpineShoulder];
        spineTopAim = new Quaternion(spineOri.Orientation.X, spineOri.Orientation.Y, spineOri.Orientation.Z, spineOri.Orientation.W) * transform.forward;

        Debug.DrawLine(spineBase.position, spineBase.position + (2f * spineBaseAim), Color.cyan);
        Debug.DrawLine(spineMid.position, spineMid.position + (2f * spineMidAim), Color.magenta);
        // Debug.DrawLine(spineShoulder.position, spineShoulder.position + (2f * spineTopAim), Color.yellow);

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

        // TESTING
        Kinect.JointOrientation jo = body.JointOrientations[jt];

        // Debug.Log("X : " + jo.Orientation.X + "  Y: " + jo.Orientation.Y + "  Z: " + jo.Orientation.Z + "  W: " + jo.Orientation.W);
        Vector3 testDir = new Quaternion(jo.Orientation.X * 10, jo.Orientation.Y * 10, jo.Orientation.Z * 10, jo.Orientation.W * 10) * jointObj.forward;

        lr.SetPosition(0, jointObj.localPosition);
        lr.SetPosition(1, jointObj.localPosition + testDir.normalized);
        // lr.SetPosition(1, jointObj.localPosition + lookDir.normalized);
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