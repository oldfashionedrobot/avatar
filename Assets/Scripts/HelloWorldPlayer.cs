using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace HelloWorld {
  public class HelloWorldPlayer : NetworkBehaviour {
    // debug stuff
    [Tooltip("Turn off body driving to manually move drivers in scene")]
    [SerializeField] private bool debugDrive = false;

    private NetworkVariable<bool> z_runtimeRigOn = new NetworkVariable<bool>(false);
    private NetworkVariable<int> z_playerNum = new NetworkVariable<int>(0);

    private Animator anim;
    private Camera mainCam;

    [SerializeField] private Rig rig;

    [SerializeField] private Transform rightShoulderIKRoot;
    [SerializeField] private Transform rightArmIKTarget;
    [SerializeField] private Transform rightArmIKHint;

    [SerializeField] private Transform leftShoulderIKRoot;
    [SerializeField] private Transform leftArmIKTarget;
    [SerializeField] private Transform leftArmIKHint;

    /// TEMP:
    [SerializeField] private Transform spineIKRoot;
    [SerializeField] private Transform spineIKTarget;

    [SerializeField] private GameObject playerModel1; 
    [SerializeField] private GameObject playerModel2; 
    [SerializeField] private GameObject playerModel3; 
    [SerializeField] private GameObject playerModel4; 

    protected InputAction m_buttonAction;
    protected InputAction m_dPadAction;
    protected InputAction m_stickMoveAction;

    private bool runtimeRigOn = false;
    // private bool strafeMovementOn = false;

    /// TESTING kinect stuf
    private BodySourceView bodySourceView;

    // TESTING spell stuff
    public Collider leftHandTouch;
    public Collider rightHandTouch;
    public SpellTest spellStuff;
    public GameObject bowModel;
    private CinemachineCameraOffset camOffsetScript;

    private bool shootingMode = false;
    /// END TESTING SPELL

    public override void OnNetworkSpawn() {
      if (IsOwner) {
        // attach camera
        FindObjectOfType<HelloWorldManager>().InitFollowCamera(transform);

        // TEMP: shite
        camOffsetScript = FindObjectOfType<CinemachineCameraOffset>();

        /// attach body drive stuff
        bodySourceView = FindObjectOfType<BodySourceView>();

        m_buttonAction = new InputAction(name: "GamepadButtonAction", InputActionType.PassThrough,
          binding: "<Gamepad>/<button>");
        m_buttonAction.performed += callbackContext => OnControllerButtonPress(callbackContext.control as ButtonControl);
        m_buttonAction.Enable();

        // m_dPadAction = new InputAction(name: "GamepadDpadAction", InputActionType.PassThrough,
        //   binding: "<Gamepad>/<dpad>");
        // m_dPadAction.performed += callbackContext => OnDpadPress(callbackContext.control as DpadControl);
        // m_dPadAction.Enable();

        m_stickMoveAction = new InputAction(name: "GamepadStickMoveAction", InputActionType.PassThrough,
          binding: "<Gamepad>/<stick>");
        m_stickMoveAction.performed += callbackContext => StickMove(callbackContext.control as StickControl);
        m_stickMoveAction.Enable();
      }
    }

    public void SetPlayerNumber(int num) {
      if (NetworkManager.Singleton.IsServer) {
        SetPlayerNumClientRpc(num);
      } 
    }

    [ClientRpc]
    void SetPlayerNumClientRpc(int num) {
      playerModel1.SetActive(false);

      switch (num) {
        case 2:
          playerModel2.SetActive(true);
          break;
        case 3:
          playerModel3.SetActive(true);
          break;
        case 4:
          playerModel4.SetActive(true);
          break;
        default:
          playerModel1.SetActive(true);
          break;
      }
    }

    void Awake() {
      anim = GetComponent<Animator>();
      mainCam = Camera.main;
    }

    private void OnAnimatorMove() {
      transform.position = transform.position + anim.deltaPosition;
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

    #region randommovement
    public void RandomMove() {
      Vector3 newPos = GetRandomPositionOnPlane();

      if (NetworkManager.Singleton.IsServer) {
        transform.position = newPos;
      } else {
        SubmitPositionRequestServerRpc(newPos);
      }
    }

    [ServerRpc]
    void SubmitPositionRequestServerRpc(Vector3 newPosition, ServerRpcParams rpcParams = default) {
      transform.position = newPosition;
    }

    static Vector3 GetRandomPositionOnPlane() {
      return new Vector3(Random.Range(-3f, 3f), -5f, Random.Range(-3f, 3f));
    }
    #endregion 

    #region newstuff
    void Update() {
      runtimeRigOn = z_runtimeRigOn.Value;

      rightHandTouch.enabled = runtimeRigOn;
      leftHandTouch.enabled = runtimeRigOn;

      if (runtimeRigOn && rig.weight < 1f) {
        // turn IK on
        rig.weight += 0.02f;
      } else if (!runtimeRigOn && rig.weight > 0f) {
        // turn IK off
        rig.weight -= 0.02f;
      }


      if (IsOwner) {
        // NOTE: need a more robust way to degrade if no kinect/body tracking
        if (!debugDrive && runtimeRigOn && bodySourceView != null) {

          /// TEST WORKING kinect / body stuff
          float upperArmLength = 0.3f;
          float lowerArmLength = 0.3f;

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


          if(NetworkManager.Singleton.IsServer) {
            leftArmIKHint.position = leftElbowPos;
            leftArmIKTarget.position = leftHandPos;
            leftArmIKTarget.right = -(leftHandPos - leftElbowPos);

            rightArmIKHint.position = rightElbowPos;
            rightArmIKTarget.position = rightHandPos;
            rightArmIKTarget.right = (rightHandPos - rightElbowPos);
          } else {
            SetArmHandlesServerRPC(leftElbowPos, leftHandPos, rightElbowPos, rightHandPos);
          }

          /// TODO: 

          // Debug.DrawLine(leftShoulderIKRoot.position, leftElbowPos, Color.magenta);
          // Debug.DrawLine(leftElbowPos, leftHandPos, Color.magenta);

          // Debug.DrawLine(rightShoulderIKRoot.position, rightElbowPos, Color.cyan);
          // Debug.DrawLine(rightElbowPos, rightHandPos, Color.cyan);

          /// TESTING: what other tracking can i use

          // spine?
          float upperSpineLength = .2f;
          float lowerSpineLength = .2f;

          Vector3 spineMidPos = spineIKRoot.position + (lowerSpineLength * ConvertDirectionToLocalSpace(bodySourceView.spineBaseAim, transform));
          Vector3 spineTopPos = spineMidPos + (upperSpineLength * ConvertDirectionToLocalSpace(bodySourceView.spineMidAim, transform));
          Vector3 spineTopAim = ConvertDirectionToLocalSpace(bodySourceView.spineTopAim, transform);

          if(NetworkManager.Singleton.IsServer) {
            spineIKTarget.position = spineTopPos;
            spineIKTarget.forward = spineTopAim;
          } else {
            SetSpineHandlesServerRPC(spineTopPos, spineTopAim);
          }

          // Debug.DrawLine(spineIKTarget.position, spineIKTarget.position + ConvertDirectionToLocalSpace(bodySourceView.spineTopAim, transform), Color.red);

          // lean might be useful 
          // Vector3 leanDebugPos = transform.position + (Vector3.up * 1f);
          // Vector3 leanDebugTgt = leanDebugPos + bodySourceView.leanDirection;
          // Debug.DrawLine(leanDebugPos, leanDebugTgt, Color.green);
        }

        var gamepad = Gamepad.current;
        if (gamepad == null) {
          Debug.Log("No gamepad");
          return;
        }

        if (shootingMode) {
          Vector3 camFwd = mainCam.transform.forward;
          Vector3 lookDir = new Vector3(camFwd.x, 0f, camFwd.z);

          camOffsetScript.m_Offset = Vector3.Lerp(camOffsetScript.m_Offset, new Vector3(0.5f, 0.1f, 0.5f), 20f * Time.deltaTime);

          if (NetworkManager.Singleton.IsServer) {
            anim.SetFloat("speedX", 0f);
            transform.rotation = Quaternion.LookRotation(lookDir);
          } else {
            SetAnimFloatServerRPC("speedX", 0f);
            anim.SetFloat("speedX", 0f);
            SetRotationServerRPC(lookDir);
            transform.rotation = Quaternion.LookRotation(lookDir);

          }
        } else {
          Vector3 moveDir = GetInputDirectionByCamera(gamepad);

          camOffsetScript.m_Offset = Vector3.Lerp(camOffsetScript.m_Offset, Vector3.zero, 20f * Time.deltaTime); 

          if (NetworkManager.Singleton.IsServer) {
            anim.SetFloat("speedX", moveDir.magnitude);
          } else {
            SetAnimFloatServerRPC("speedX", moveDir.magnitude);
            anim.SetFloat("speedX", moveDir.magnitude);
          }

          if (moveDir.magnitude > 0f) {
            if (NetworkManager.Singleton.IsServer) {
              transform.rotation = Quaternion.LookRotation(moveDir);
            } else {
              SetRotationServerRPC(moveDir);
              transform.rotation = Quaternion.LookRotation(moveDir);
            }
          }
        }
      }
    }

    [ServerRpc]
    void ClientToggleBowServerRpc(bool onOff)
    {
      ToggleBowClientRpc(onOff);
    }

    [ClientRpc]
    void ToggleBowClientRpc(bool onOff)
    {
      bowModel.SetActive(onOff);
    }

    [ServerRpc]
    void SetArmHandlesServerRPC(Vector3 leftElbowPos, Vector3 leftHandPos, Vector3 rightElbowPos, Vector3 rightHandPos) {
      leftArmIKHint.position = leftElbowPos;
      leftArmIKTarget.position = leftHandPos;
      leftArmIKTarget.right = -(leftHandPos - leftElbowPos);

      rightArmIKHint.position = rightElbowPos;
      rightArmIKTarget.position = rightHandPos;
      rightArmIKTarget.right = (rightHandPos - rightElbowPos);
    }

    [ServerRpc]
    void SetSpineHandlesServerRPC(Vector3 spineTopPos, Vector3 spineTopAim) {
      spineIKTarget.position = spineTopPos;
      spineIKTarget.forward = spineTopAim;
    }

    [ServerRpc]
    void SetAnimFloatServerRPC(string paramName, float paramValue, ServerRpcParams rpcParams = default) {
      anim.SetFloat(paramName, paramValue);
    }

    [ServerRpc]
    void SetAnimBoolServerRPC(string paramName, bool paramValue, ServerRpcParams rpcParams = default) {
      anim.SetBool(paramName, paramValue);
    }

    [ServerRpc]
    void SetRotationServerRPC(Vector3 lookDirection, ServerRpcParams rpcParams = default) {
      transform.rotation = Quaternion.LookRotation(lookDirection);
    }

    [ServerRpc]
    void SetBodyDriveServerRPC(bool value, ServerRpcParams rpcParams = default) {
      z_runtimeRigOn.Value = value;
    }
    #endregion

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

    protected void ActivateFireMode() {
      if(spellStuff.ActivateBowAction(CancelSpellAction)) {
        ActivateBodyDrive();
        shootingMode = true;
        bowModel.SetActive(true);
        if(NetworkManager.Singleton.IsServer) {
          ToggleBowClientRpc(true);
        } else {
          ClientToggleBowServerRpc(true);
        }
      }
    }

    protected void ActivateChargeMode() {
      spellStuff.ActivateElementSelect(CancelSpellAction);
      ActivateBodyDrive();
    }

    protected void CancelSpellAction() {
      spellStuff.CancelAll();
      DeactivateBodyDrive();
      shootingMode = false;
      if (NetworkManager.Singleton.IsServer) {
        ToggleBowClientRpc(false);
      } else {
        ClientToggleBowServerRpc(false);
      }
    }

    protected void ActivateBodyDrive() {
      // Debug.Log("activate body drive");
      if (NetworkManager.Singleton.IsServer) {
        z_runtimeRigOn.Value = true;
      } else {
        SetBodyDriveServerRPC(true);
        runtimeRigOn = true;
      }
    }

    protected void DeactivateBodyDrive() {
      // Debug.Log("deactivate body drive");
      if (NetworkManager.Singleton.IsServer) {
        z_runtimeRigOn.Value = false;
      } else {
        SetBodyDriveServerRPC(false);
        runtimeRigOn = false;
      }
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
        // TEMP: shite!!
        case "touchpadButton":
          Debug.Break();
          break;
        case "leftShoulder":
          ActivateChargeMode();
          break;
        case "leftTrigger":
          CancelSpellAction();
          break;
        case "rightShoulder":
          ActivateFireMode();

          break;
        case "rightTrigger":
          spellStuff.TakeShootButton(control.ReadValue());
          break;
        case "leftStick":
          if (NetworkManager.Singleton.IsServer) {
            anim.SetBool("crouch", true);
          } else {
            SetAnimBoolServerRPC("crouch", true);
            anim.SetBool("crouch", true);
          }
          break;
        case "rightStick":
          if (NetworkManager.Singleton.IsServer) {
            anim.SetBool("crouch", false);
          } else {
            SetAnimBoolServerRPC("crouch", false);
            anim.SetBool("crouch", false);
          }
          break;
        default:
          // Debug.Log("NO MAPPED ACTION FOR: " + buttonName + control.ReadValue());
          break;
      }

      // check what buttons pressed
      // if (buttonName == "North") {
      //   ActivateBodyDrive();
      // } else if (buttonName == "West") {
      //   DeactivateBodyDrive();
      // }

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
}