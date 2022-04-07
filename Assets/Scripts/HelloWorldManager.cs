using Cinemachine;
using Unity.Netcode;
using UnityEngine;

namespace HelloWorld {
  public class HelloWorldManager : MonoBehaviour {

    [SerializeField]
    private CinemachineFreeLook lookCam;

    [Tooltip("Automatically start as Host")]
    [SerializeField] private bool debugHost = false;


    // HOST managed state vars
    private static int numPlayers = 0;

    void OnGUI() {
      GUILayout.BeginArea(new Rect(10, 10, 300, 300));
      if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer) {
        StartButtons();
      } else {
        StatusLabels();

        SubmitNewPosition();
      }

      GUILayout.EndArea();
    }

    void StartButtons() {
      if (GUILayout.Button("Host") || debugHost) {
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.OnClientConnectedCallback += SpawnPlayerObject;
        NetworkManager.Singleton.StartHost();
      }

      if (GUILayout.Button("Client")) {
        // TODO: use some password or lobby name to differentiate connections
        NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes("room password");
        NetworkManager.Singleton.StartClient();
      }

      // if (GUILayout.Button("Server")) {
      //   NetworkManager.Singleton.StartServer();
      // }
    }

    static void StatusLabels() {
      var mode = NetworkManager.Singleton.IsHost
        ? "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

      GUILayout.Label("Transport: "
        + NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
      GUILayout.Label("Mode: " + mode);
    }

    static void SubmitNewPosition() {
      if (GUILayout.Button(NetworkManager.Singleton.IsServer ? "Move" : "Request Position Change")) {
        var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        var player = playerObject.GetComponent<HelloWorldPlayer>();
        player.RandomMove();
      }
    }

    static void SpawnPlayerObject(ulong clientId) {
      // TEST: trying manual spawning
      GameObject pp = Instantiate(Resources.Load("Avatar"), Vector3.zero - (Vector3.up * 5f), Quaternion.identity) as GameObject;
      pp.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
      pp.GetComponent<HelloWorldPlayer>().SetPlayerNumber(numPlayers);
    }

    static void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback) {
      string data = System.Text.Encoding.ASCII.GetString(connectionData);

      // Debug.Log("Tryin to connect: ");
      // Debug.Log("Length: " + connectionData.Length + " -- " + data);

      //Your logic here6
      bool approve = true;
      bool createPlayerObject = false;

      // The prefab hash. Use null to use the default player prefab
      // If using this hash, replace "MyPrefabHashGenerator" with the name of a prefab added to the NetworkPrefabs field of your NetworkManager object in the scene
      // ulong? prefabHash = NetworkSpawnManager.GetPrefabHashFromGenerator("MyPrefabHashGenerator");'
      uint? _prefabHash = null;
      Vector3? _positionToSpawnAt = null;
      Quaternion? _rotationToSpawnWith = null;

      /// NOTE: only real functionahere!!!!!!!!!!!
      numPlayers += 1;
      //dfsfsdfsdfsd

      //If approve is true, the connection gets added. If it's false. The client gets disconnected
      callback(createPlayerObject, _prefabHash, approve, _positionToSpawnAt, _rotationToSpawnWith);
    }

    // setup stuff
    public void InitFollowCamera(Transform target) {
      lookCam.Follow = target;
      lookCam.LookAt = target;
    }
  }

}