using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bob : MonoBehaviour {
  Vector3 floatY;
  float originalY;

  public float floatStrength;

private float randoffset;
private float randoffsetRot;

  void Start() {
    this.originalY = this.transform.position.y;
    randoffset = Random.Range(0f, 10f);
    randoffsetRot = Random.Range(0f, 2f);
  }

  void Update() {
    floatY = transform.position;
    floatY.y = (Mathf.Sin(Time.time + randoffset) * floatStrength) + this.originalY;
    transform.position = floatY;

    transform.transform.Rotate(0.0f, 1.0f * Time.deltaTime * randoffset, 0.0f);
  }
}
