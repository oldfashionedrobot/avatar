using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class Puppeteer : MonoBehaviour {
  public BodySourceView bodySourceView;

  public Transform rightShoulderIKRoot;
  public Transform rightArmIKTarget;
  public Transform rightArmIKHint;

  public Transform leftShoulderIKRoot;
  public Transform leftArmIKTarget;
  public Transform leftArmIKHint;

  private Camera mainCam;
  private Animator anim;
  private PlayerControls ctrl;

  protected InputAction m_buttonAction;
  protected InputAction m_dPadAction;
  protected InputAction m_stickMoveAction;

  // state vars
  private bool bodyDriveOn = false;
  private bool strafeMovementOn = false;

  void Awake() {
    anim = GetComponent<Animator>();
    mainCam = Camera.main;
  }

  void Start() {
    m_buttonAction = new InputAction(name: "DualShockButtonAction", InputActionType.PassThrough,
      binding: "*DualShock*/<button>");
    m_buttonAction.performed += callbackContext => OnControllerButtonPress(callbackContext.control as ButtonControl, isPS : true);
    m_buttonAction.Enable();

    // m_dPadAction = new InputAction(name: "DualShockDpadAction", InputActionType.PassThrough,
    //   binding: "*DualShock*/<dpad>");
    // m_dPadAction.performed += callbackContext => OnDpadPress(callbackContext.control as DpadControl);
    // m_dPadAction.Enable();

    m_stickMoveAction = new InputAction(name: "DualShockStickMoveAction", InputActionType.PassThrough,
      binding: "*DualShock*/<stick>");
    m_stickMoveAction.performed += callbackContext => StickMove(callbackContext.control as StickControl);
    m_stickMoveAction.Enable();
  }

  private void OnEnable() {
    if (m_buttonAction != null) m_buttonAction.Enable();
    if (m_dPadAction != null) m_dPadAction.Enable();
    if (m_stickMoveAction != null) m_stickMoveAction.Enable();
  }

  private void OnDisable() {
    m_buttonAction?.Disable();
    m_dPadAction?.Disable();
    m_stickMoveAction?.Disable();
  }

  void FixedUpdate() {
    var gamepad = Gamepad.current;
    if (gamepad == null) {
      Debug.Log("No gamepad");
      return;
    }

    if (strafeMovementOn) {

    } else {
      Vector3 moveDir = GetInputDirectionByCamera(gamepad);

      anim.SetFloat("speedX", moveDir.magnitude);

      if (moveDir.magnitude > 0f) {
        transform.rotation = Quaternion.LookRotation(moveDir);
      }
    }

    /// TEST WORKING
    float upperArmLength = 0.3f;
    float lowerArmLength = 0.3f;

    if (bodyDriveOn) {

      float currWeight = leftShoulderIKRoot.GetComponent<TwoBoneIKConstraint>().weight;

      if (currWeight < 1f) {
        leftShoulderIKRoot.GetComponent<TwoBoneIKConstraint>().weight += 0.02f;
        rightShoulderIKRoot.GetComponent<TwoBoneIKConstraint>().weight += 0.02f;
      }

      // positions for the IK targets
      Vector3 rightElbowPos =
        rightShoulderIKRoot.position
        + (upperArmLength * ConvertDirectionToLocalSpace(bodySourceView.rightShoulderAim, transform));
      Vector3 rightHandPos =
        rightElbowPos
        + (lowerArmLength * ConvertDirectionToLocalSpace(bodySourceView.rightElbowAim, transform));

      Vector3 leftElbowPos =
        leftShoulderIKRoot.position
        + (upperArmLength * ConvertDirectionToLocalSpace(bodySourceView.leftShoulderAim, transform));
      Vector3 leftHandPos =
        leftElbowPos
        + (lowerArmLength * ConvertDirectionToLocalSpace(bodySourceView.leftElbowAim, transform));

      leftArmIKHint.position = leftElbowPos;
      leftArmIKTarget.position = leftHandPos;
      leftArmIKTarget.right = -(leftHandPos - leftElbowPos);

      rightArmIKHint.position = rightElbowPos;
      rightArmIKTarget.position = rightHandPos;
      rightArmIKTarget.right = (rightHandPos - rightElbowPos);

      Debug.DrawLine(leftShoulderIKRoot.position, leftElbowPos, Color.magenta);
      Debug.DrawLine(leftElbowPos, leftHandPos, Color.magenta);

      Debug.DrawLine(rightShoulderIKRoot.position, rightElbowPos, Color.cyan);
      Debug.DrawLine(rightElbowPos, rightHandPos, Color.cyan);
    } else {
      float currWeight = leftShoulderIKRoot.GetComponent<TwoBoneIKConstraint>().weight;

      if (currWeight > 0f) {
        leftShoulderIKRoot.GetComponent<TwoBoneIKConstraint>().weight -= 0.1f;
        rightShoulderIKRoot.GetComponent<TwoBoneIKConstraint>().weight -= 0.1f;
      }
    }

    /// END TEST
  }

  private Vector3 GetInputDirectionByCamera(Gamepad gamepad) {
    Vector2 move = gamepad.leftStick.ReadValue();
    float horizontalAxis = move.x; //Input.GetAxis("Horizontal");
    float verticalAxis = move.y; // Input.GetAxis("Vertical");

    //camera forward and right vectors:
    var forward = mainCam.transform.forward;
    var right = mainCam.transform.right;

    //project forward and right vectors on the horizontal plane (y = 0)
    forward.y = 0f;
    right.y = 0f;
    forward.Normalize();
    right.Normalize();

    //this is the direction in the world space we want to move:
    return forward * verticalAxis + right * horizontalAxis;
  }
  private Vector3 ConvertDirectionToLocalSpace(Vector3 dir, Transform trans) {
    // forward and right vectors:
    var forward = trans.forward;
    var right = trans.right;
    var up = trans.up;

    // //project forward and right vectors on the horizontal plane (y = 0)
    // forward.y = 0f;
    // right.y = 0f;
    // forward.Normalize();
    // right.Normalize();

    //this is the direction in the world space we want to move:
    return -forward * dir.z + right * dir.x + up * dir.y;
  }

  /// pulled from input examples
  protected virtual void OnControllerButtonPress(ButtonControl control, string dpadName = null, bool isXbox = false, bool isPS = false) {
    string buttonName = control.name;
    Transform button = null;

    // If the button input is from pressing a stick
    if (buttonName.Contains("StickPress")) {
      buttonName = buttonName.Replace("Press", "");
    } else {
      if (control.aliases.Count > 0) {
        // TODO: should i map it all to cardinal dirs?
        if (isXbox) buttonName = control.aliases[0];
        else if (isPS) buttonName = control.aliases[1];
        else buttonName = control.name.Replace("button", "");
      }
    }

    float btnVal = control.ReadValue();

    switch (buttonName) {
      case "leftShoulder":
        bodyDriveOn = true;
        break;
      case "rightShoulder":
        bodyDriveOn = false;
        break;
      case "leftStick":
        anim.SetBool("crouch", true);
        break;
      case "rightStick":
        anim.SetBool("crouch", false);
        break;
      default:
        Debug.Log(buttonName + control.ReadValue());
        break;
    }

    // check what buttons pressed
    if (buttonName == "triangle") {
      Debug.Log("activate body drive");
      bodyDriveOn = true;
    } else if (buttonName == "square") {
      Debug.Log("deactivate body drive");
      bodyDriveOn = false;
    }

    if (button == null)
      return;

  }

  // Callback function when a stick is moved.
  protected virtual void StickMove(StickControl control) {
    Vector2 pos = control.ReadValue();

    if (pos.magnitude > 0.1) {
      // Debug.Log(control.name + "   " + pos);
      // left stick drive move by anim controller and the rotation code above
      // right stick drives camera with cinemachine config
    }
  }

  // Callback funtion when a button in a dpad is pressed.
  protected virtual void OnDpadPress(DpadControl control) {
    string dpadName = control.name;
    OnControllerButtonPress(control.up, dpadName);
    OnControllerButtonPress(control.down, dpadName);
    OnControllerButtonPress(control.left, dpadName);
    OnControllerButtonPress(control.right, dpadName);
  }

}