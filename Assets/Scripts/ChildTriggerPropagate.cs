using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChildTriggerPropagate : MonoBehaviour {

  public SpellTest spellScript;

  void OnTriggerEnter(Collider other) {
    spellScript.OnChildTriggerEntered(other, gameObject);
  }
}