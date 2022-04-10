using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Pot : NetworkBehaviour
{

  public GameObject pickupFX;

  private void OnTriggerEnter(Collider other)
  {
    if (NetworkManager.Singleton.IsServer)
    {

      if (other.tag == "Hand")
      {

        SpawnHitFXClientRpc();

        SpawnGem();

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


  void SpawnGem() {
    string prefab = "Gem";

    switch (Random.Range(0,4))
    {
      case 0:
        prefab += ".Green";
        break;
      case 1:
        prefab += ".Purple";
        break;
      case 2:
        prefab += ".Red";
        break;
      case 3:
        prefab += ".Blue";
        break;
      default:
        break;
    }

    GameObject gem = Instantiate(Resources.Load(prefab), transform.position, Quaternion.identity) as GameObject;
    gem.GetComponent<NetworkObject>().Spawn();
  }
}
