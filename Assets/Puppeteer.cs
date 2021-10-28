using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class Puppeteer : MonoBehaviour {
  public BodySourceView bodySourceView;

  public Transform rightShoulderIKRoot;
  public Transform leftShoulderIKRoot;

  public Transform leftArmIKTarget;
  public Transform leftArmIKHint;

  private bool bodyDriveOn = false;

  private Camera mainCam;
  private Animator anim;
  private PlayerControls ctrl;

  protected InputAction m_buttonAction;
  protected InputAction m_dPadAction;
  protected InputAction m_stickMoveAction;

  void Awake() {
    anim = GetComponent<Animator>();
    mainCam = Camera.main;
    // ctrl = new PlayerControls();
    // ctrl.BaseGameplay.Jump.performed += ctx => TriggerJump();
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

    Vector3 moveDir = GetInputDirectionByCamera(gamepad);

    bool crouched = gamepad.leftStickButton.wasPressedThisFrame; //Input.GetKey(KeyCode.LeftShift);
    anim.SetBool("crouch", crouched);
    anim.SetFloat("speed", moveDir.magnitude);

    if (moveDir.magnitude > 0f) {
      Debug.DrawLine(transform.position, transform.position + moveDir, Color.red);
      transform.rotation = Quaternion.LookRotation(moveDir);
    }

    /// TEST WORKING
    float upperArmLength = 0.3f;
    float lowerArmLength = 0.3f;

    if (bodyDriveOn) {

      float currWeight = leftShoulderIKRoot.GetComponent<TwoBoneIKConstraint>().weight;

      if (currWeight < 1f) {
        leftShoulderIKRoot.GetComponent<TwoBoneIKConstraint>().weight += 0.02f;
      }

      Vector3 rightElbowPos = leftShoulderIKRoot.position + (upperArmLength * bodySourceView.rightShoulderAim);
      Vector3 rightHandPos = rightElbowPos + (lowerArmLength * bodySourceView.rightElbowAim);

      leftArmIKHint.position = transform.right * rightElbowPos.x + Vector3.up * rightElbowPos.y + transform.forward * rightElbowPos.z;
      leftArmIKTarget.position = transform.right * rightHandPos.x + Vector3.up * rightHandPos.y + transform.forward * rightHandPos.z;
      // leftArmIKTarget.right = -(bodySourceView.rightElbowAim);

      Debug.DrawLine(leftShoulderIKRoot.position, rightElbowPos, Color.magenta);
      Debug.DrawLine(rightElbowPos, rightHandPos, Color.magenta);
    } else {
      float currWeight = leftShoulderIKRoot.GetComponent<TwoBoneIKConstraint>().weight;

      if (currWeight > 0f) {
        leftShoulderIKRoot.GetComponent<TwoBoneIKConstraint>().weight -= 0.02f;
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

  /// pulled from input examples
  protected virtual void OnControllerButtonPress(ButtonControl control, string dpadName = null, bool isXbox = false, bool isPS = false) {
    string buttonName = control.name;
    Transform button = null;

    // If the button input is from pressing a stick
    if (buttonName.Contains("StickPress")) {
      buttonName = buttonName.Replace("Press", "");
      Debug.Log(buttonName + "  pressed!!!!!");
    } else {
      if (control.aliases.Count > 0) {
        // TODO: should i map it all to cardinal dirs?
        if (isXbox) buttonName = control.aliases[0];
        else if (isPS) buttonName = control.aliases[1];
        else buttonName = control.name.Replace("button", "");
      }

      // check what buttons pressed
      if (buttonName == "triangle") {
        Debug.Log("activate body drive");
        bodyDriveOn = true;
      } else if (buttonName == "square") {
        Debug.Log("deactivate body drive");
        bodyDriveOn = false;
      }

      Debug.Log(buttonName + "  pressed!!!!!");
    }

    if (button == null)
      return;

    if (control.ReadValue() > 0)
      Debug.Log(buttonName + control.ReadValue());
    else
      Debug.Log(buttonName + control.ReadValue());
  }

  // Callback function when a stick is moved.
  protected virtual void StickMove(StickControl control) {
    Vector2 pos = control.ReadValue();

    if (pos.magnitude > 0.1)
      Debug.Log(control.name + "   " + pos);
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