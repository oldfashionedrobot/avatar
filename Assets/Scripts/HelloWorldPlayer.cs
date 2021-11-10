using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace HelloWorld {
  public enum AnimParameterType {
    Bool = 0,
    Float = 1,
    Integer = 2
  }

  public class HelloWorldPlayer : NetworkBehaviour {
    private Animator anim;
    private Camera mainCam;

    protected InputAction m_buttonAction;
    protected InputAction m_dPadAction;
    protected InputAction m_stickMoveAction;

    private bool bodyDriveOn = false;
    private bool strafeMovementOn = false;

    public override void OnNetworkSpawn() {
      if (IsOwner) {
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

    void Awake() {
      anim = GetComponent<Animator>();
      mainCam = Camera.main;
    }

    private void OnAnimatorMove() {
      transform.position = transform.position + anim.deltaPosition;
      // if (NetworkManager.Singleton.IsServer) {
      // }
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
      return new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
    }
    #endregion 

    #region newstuff
    void Update() {
      if (IsOwner) {
        var gamepad = Gamepad.current;
        if (gamepad == null) {
          Debug.Log("No gamepad");
          return;
        }

        if (strafeMovementOn) {

        } else {
          Vector3 moveDir = GetInputDirectionByCamera(gamepad);

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

        // Vector3 dir = GetInputDirectionByCamera();

        // if (dir.magnitude > 0.05f) {
        //   if (NetworkManager.Singleton.IsServer) {
        //     transform.position = transform.position + (dir * 0.05f);
        //     Position.Value = transform.position;
        //   } else {
        //     MoveMeServerRPC(transform.position + (dir * 0.05f));
        //     // Move local self here too?
        //   }
        // }
      }

      // transform.position = Position.Value;
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
          // bodyDriveOn = true;
          break;
        case "rightShoulder":
          // bodyDriveOn = false;
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
          Debug.Log("NO MAPPED ACTION FOR: " + buttonName + control.ReadValue());
          break;
      }

      // check what buttons pressed
      if (buttonName == "North") {
        // Debug.Log("activate body drive");
        // bodyDriveOn = true;
      } else if (buttonName == "West") {
        // Debug.Log("deactivate body drive");
        // bodyDriveOn = false;
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
}