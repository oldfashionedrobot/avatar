using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Gem : NetworkBehaviour
{

  public GameObject pickupFX;
  
  private void OnTriggerEnter(Collider other) {
    if (NetworkManager.Singleton.IsServer) {

      if (other.tag == "Player") {

        SpawnHitFXClientRpc();
        Destroy(gameObject);
      }
    }
  }

  [ClientRpc]
  void SpawnHitFXClientRpc()
  {
    GameObject fx = Instantiate(pickupFX, transform.position, Quaternion.identity);

    Destroy(fx, 2.0f);
  }
}
