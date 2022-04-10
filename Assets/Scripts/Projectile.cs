using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : NetworkBehaviour {
  private Vector3 _target;
  private float _speed;
  private float _height;
  private Vector3 _startPos;
  private float _startTime;

  public GameObject hitFX;

  public void Launch(Vector3 target, float pullDistance, float aimShift) {
    this._target = target;
    this._speed = Mathf.Min(5f, 5f * 0.5f / Mathf.Abs(pullDistance));
    this._height = 0f;
  }

   void OnTriggerEnter(Collider other) {
     if(NetworkManager.Singleton.IsServer) {
      // Debug.Log("projectile hit " + other);  
      
      if(other.tag == "Totem") {
        Destroy(gameObject);  
        SpawnHitFXClientRpc(this._target);
      } else if(other.tag == "Defense") {
        Debug.Log("HIT A " + other.gameObject.name);


        SpawnHitFXClientRpc(other.transform.position);
        Destroy(other.gameObject);
        Destroy(gameObject);
      }
    } 
  }

  [ClientRpc]
  void SpawnHitFXClientRpc(Vector3 spawnLoc) {
    GameObject fx = Instantiate(hitFX, spawnLoc, Quaternion.identity) as GameObject;
    Destroy(fx, 5f);
  }


  void Update() {
    float deltaTime = Time.time - this._startTime;
    
    float t = deltaTime / this._speed;

    this.transform.position = Vector3.Lerp(this._startPos, this._target, t);
  }

  void Start() {
    this._startTime = Time.time;
    this._startPos = transform.position;
  }
}