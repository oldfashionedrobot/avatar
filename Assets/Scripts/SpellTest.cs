using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using HelloWorld;

public class SpellTest : NetworkBehaviour {

  public enum SpellState {
    None = 0,
    ElementSelect = 1,
    SpellCast = 2
  }

  public enum SpellElement {
    Fire = 0,
    Water = 1,
    Earth = 2,
    Wind = 3,
    Aether = 4
  }

  public enum BowState {
    None = 0,
    Ready = 1,
    Aim = 2,
    Fire = 3
  }


  public GameObject elementSelect;
  public GameObject genericSpell;
  public GameObject fireSpell;
  public GameObject bowAction;
  public GameObject chargeEffects;

  private Action cleanupCallback;

  private SpellState state = SpellState.None;
  private BowState bowState = BowState.None;
  private SpellElement element = SpellElement.Wind;

  private int numCharges = 0;

  public void OnChildTriggerEntered(Collider other, GameObject triggered) {
    switch (state) {
      case SpellState.ElementSelect:
        SelectElement(triggered.name);
        return;
      case SpellState.SpellCast:
        HandleSpellCast(triggered);
        return;
      case SpellState.None:
      default:
        // TEMP: this is hacky, but works for now
        if(bowState != BowState.None) {
          HandleBowAction(triggered.name, other.transform.parent.name);
        }
        break;
    }
  }

  public void CancelAll() {
    OnDisable();
  }

  public bool ActivateElementSelect(Action cleanupCb) {
    cleanupCallback = cleanupCb;

    if (state != SpellState.ElementSelect) {
      state = SpellState.ElementSelect;
      elementSelect.SetActive(true);
      return true;
    } else {
      return false;
    }
  }

  public bool ActivateBowAction(Action cleanupCb) {
    // if(numCharges < 1) {
    //   return false;
    // }

    cleanupCallback = cleanupCb;

    InitBowAction();
    return true;
  }

// TEMP: SHITE !!!!!
private bool heldDown = false;
private Vector3 pullStart;
  public void TakeShootButton(float dir) {  
    if(waitForShootBtn) {
      if(dir == 1 && !heldDown) {
        // Debug.Log("PULLING BOW");
        heldDown = true;
        pullStart = GameObject.Find("Hand_R").transform.position;
      }

      if(heldDown && dir == 0) {
        // Debug.Log("RELEASING BOW");
        heldDown = false;
        // shite to get the pull distance
        Vector3 pullEnd = GameObject.Find("Hand_R").transform.position;
        float pullDistance = (pullStart - pullEnd).magnitude;

        // shite to get the aim vert
        Vector3 leftHandPos = GameObject.Find("Hand_L").transform.position;
        float aimShift = (leftHandPos - spellStart.transform.position).y;

        waitForShootBtn = false;
        bowState = BowState.None;
   

        Vector3 projSpawn = spellStart.transform.position;
        Vector3 aimDir;
        Transform tgt = GameObject.FindObjectOfType<HelloWorldPlayer>().target;

        if(tgt != null) {
          // shoot at current target
          aimDir = (tgt.position - projSpawn).normalized * 100f;
        } else {
          Ray camAim = Camera.main.ScreenPointToRay(new Vector3((Screen.width * .5f), (Screen.height * .5f), 0));
          Vector3 targetPoint = camAim.GetPoint(100f);
          aimDir = targetPoint - projSpawn;
        }

        aimDir.y += 10f;

        if (NetworkManager.Singleton.IsServer) {
          SetChargeEffectsClientRpc(element, 0);
        } else {
          ClientSetChargeEffectsServerRpc(element, 0);
        }
        
        // fire projectile for each charge
        for(int i = 0; i < numCharges; i++) {
          StartCoroutine(DelayFire(element, aimDir, projSpawn, aimShift, pullDistance, i * 0.1f));
        }

        // consume charges
        numCharges = 0;

        cleanupCallback();
      }
    } else if(dir == 0) {

      // Vector3 projSpawn = transform.position;
      // projSpawn.y += 1.5f;
      // Ray camAim = Camera.main.ScreenPointToRay(new Vector3((Screen.width * .6f), (Screen.height * .6f), 0));
      // Vector3 targetPoint = camAim.GetPoint(100f);
      // Vector3 aimDir = targetPoint - projSpawn;
      // aimDir.y += 10f;
      
      // if (NetworkManager.Singleton.IsServer) {
      //   ReleaseSpellClientRpc(element, aimDir, projSpawn, Vector3.zero);
      // } else {
      //   ClientReleaseSpellServerRpc(element, aimDir, projSpawn, Vector3.zero);
      // }
    }
  }

  IEnumerator DelayFire(SpellElement elem, Vector3 aimDir, Vector3 projSpawn, float aimShift, float pullDistance = 0, float delayTime = 0) {
    //Wait for the specified delay time before continuing.
    yield return new WaitForSeconds(delayTime);

    Debug.Log("firing");
    if (NetworkManager.Singleton.IsServer) {
      ReleaseSpellClientRpc(element, aimDir, projSpawn, aimShift, pullDistance);
    } else {
      ClientReleaseSpellServerRpc(element, aimDir, projSpawn, aimShift, pullDistance);
    }

    //Do the action after the delay time has finished.
  }

  // Start is called before the first frame update
  void Start() {

  }

  // Update is called once per frame
  void Update() {

  }

  void OnEnable() {
    numCharges = 0;
    state = SpellState.None;
    bowState = BowState.None;
    waitForShootBtn = false;
    heldDown = false;
    elementSelect.SetActive(false);
    genericSpell.SetActive(false);
    fireSpell.SetActive(false);
    bowAction.SetActive(false);
  }

  void OnDisable() {
    state = SpellState.None;
    bowState = BowState.None;
    waitForShootBtn = false;
    heldDown = false;
    elementSelect.SetActive(false);
    genericSpell.SetActive(false);
    fireSpell.SetActive(false);
    bowAction.SetActive(false);
  }

  private void SelectElement(string elementSelected) {
    // Debug.Log("SELECTED " + elementSelected);

    state = SpellState.SpellCast;
    elementSelect.SetActive(false);

    switch (elementSelected) {
      case "Fire":
        element = SpellElement.Fire;
        InitSpellCast();
        break;
      case "Water":
        element = SpellElement.Water;
        InitSpellCast();
        break;
      case "Wind":
        element = SpellElement.Wind;
        InitSpellCast();
        break;
      case "Earth":
        element = SpellElement.Earth;
        InitSpellCast();
        break;
      default:
        element = SpellElement.Aether;
        InitSpellCast();
        break;
    }
  }

  // TODO: each spell should have its own script to drive
  // here is a basic logic for a generic "bowl" action

  private int castingState = 0; // o:pre-start, 1:started-move to charge spot, 2:charged-move to start spot to release 
  private GameObject spellStart;
  private GameObject spellCharge;

  private void InitSpellCast() {
    // NOTE: hacky stuff here, need to move all specific spell logic into their own
    // polymorphic spell thingies

    if (element != SpellElement.Fire && element != SpellElement.Wind) {
      genericSpell.SetActive(true);
      castingState = 0;
      spellStart = genericSpell.transform.Find("Start").gameObject;
      spellStart.SetActive(true);

      spellCharge = genericSpell.transform.Find("Charge").gameObject;
      spellCharge.SetActive(false);
    } else {
      fireSpell.SetActive(true);
      castingState = 1;

      // to simulate fire spawning around you, just flip scale to be left side randomly
      if (UnityEngine.Random.value > 0.5f) {
        fireSpell.transform.localScale = new Vector3(-1, 1, 1);
      } else {
        fireSpell.transform.localScale = new Vector3(1, 1, 1);
      }

      spellStart = fireSpell.transform.Find("Start").gameObject;
      spellStart.SetActive(false);

      spellCharge = fireSpell.transform.Find("Charge").gameObject;
      spellCharge.SetActive(true);
    }

  }

  private void HandleSpellCast(GameObject triggered) {
    if (castingState == 0 && triggered.name == "Start") {
      // Begin the spell cast
      castingState = 1;
      spellStart.SetActive(false);
      spellCharge.SetActive(true);
    } else if (castingState == 1 && triggered.name == "Charge") {
      // charge the spell cast
      castingState = 2;
      spellStart.SetActive(true);
      spellCharge.SetActive(false);
    } else if (castingState == 2 && triggered.name == "Start") {
      /// successful charge
      numCharges += 1;

      if (NetworkManager.Singleton.IsServer){
        SetChargeEffectsClientRpc(element, numCharges);
      } else {
        ClientSetChargeEffectsServerRpc(element, numCharges);
      }

      if(numCharges < 3) {
        InitSpellCast();
      } else {
        // release the spell cast
        spellStart.SetActive(false);
        spellCharge.SetActive(false);

        // reset to base state
        castingState = 0;
        state = SpellState.None;
        elementSelect.SetActive(false);

        // turn off all spells for now, should be handled with polymorphic spells
        genericSpell.SetActive(false);
        fireSpell.SetActive(false);

        cleanupCallback();
      }



      // Vector3 projSpawn = spellStart.transform.position;
      // Ray camAim = Camera.main.ScreenPointToRay(new Vector3((Screen.width * .6f), (Screen.height * .6f), 0));
      // Vector3 targetPoint = camAim.GetPoint(100f);
      // Vector3 aimDir = targetPoint - projSpawn;
      // aimDir.y += 10f;


      // if (NetworkManager.Singleton.IsServer) {
      //   ReleaseSpellClientRpc(element, aimDir, projSpawn, Vector3.zero);
      // } else {
      //   ClientReleaseSpellServerRpc(element, aimDir, projSpawn, Vector3.zero);
      // }
    }

  }

  private void InitBowAction() {
// TEMP: very hacky reusing vars
    bowAction.SetActive(true);
    bowState = BowState.Ready;
    spellStart = bowAction.transform.Find("Start").gameObject;
    spellStart.SetActive(true);

    spellCharge = bowAction.transform.Find("Charge").gameObject;
    spellCharge.SetActive(false);
  }

  private bool waitForShootBtn = false;
  private void HandleBowAction(string triggerName, string handName) {
    if (bowState == BowState.Ready && triggerName == "Start" && handName == "Hand_L") {
      // left hand in place, 
      // Begin the spell cast
      bowState = BowState.Aim;
      spellStart.SetActive(false);
      spellCharge.SetActive(true);
    } else if (bowState == BowState.Aim && triggerName == "Charge" && handName == "Hand_R") {
      // start pulling back bow

      waitForShootBtn = true;
            heldDown = false;
      bowState = BowState.Fire;
      spellStart.SetActive(false);
      spellCharge.SetActive(false);
    }
  }

  [ServerRpc]
  void ClientSetChargeEffectsServerRpc(SpellElement elem, int chargeNumber) {
    SetChargeEffectsClientRpc(elem, chargeNumber);
  }

  [ClientRpc]
  void SetChargeEffectsClientRpc(SpellElement elem, int chargeNumber) {
    GameObject fx;
    chargeEffects.SetActive(true);

    switch(elem) {
      case SpellElement.Fire:
        fx = chargeEffects.transform.Find("FireCharge").gameObject;
        break;
      case SpellElement.Earth:
        fx = chargeEffects.transform.Find("EarthCharge").gameObject;
        break;
      case SpellElement.Wind:
        fx = chargeEffects.transform.Find("WindCharge").gameObject;
        break;
      case SpellElement.Water:
        fx = chargeEffects.transform.Find("WaterCharge").gameObject;
        break;
      default:
        fx = chargeEffects.transform.Find("EarthCharge").gameObject;
        break;  
    }


    switch(chargeNumber) {
      case 0:
        ClearChargeEffectsClientRpc();
        break;
      case 1:
        fx.SetActive(true);
        fx.transform.Find("Charge2").gameObject.SetActive(false);
        fx.transform.Find("Charge3").gameObject.SetActive(false);
        break;
      case 2:
        fx.SetActive(true);
        fx.transform.Find("Charge2").gameObject.SetActive(true);
        fx.transform.Find("Charge3").gameObject.SetActive(false);
        break;
      case 3:
        fx.SetActive(true);
        fx.transform.Find("Charge2").gameObject.SetActive(true);
        fx.transform.Find("Charge3").gameObject.SetActive(true);
        break;
      default:
        ClearChargeEffectsClientRpc();
        break;
    }
  }

  [ServerRpc]
  void ClientClearChargeEffectsServerRpc() {
    ClearChargeEffectsClientRpc();
  }

  [ClientRpc]
  void ClearChargeEffectsClientRpc() {
    foreach (Transform child in chargeEffects.transform)
      child.gameObject.SetActive(false);

    chargeEffects.SetActive(false);
  }

  [ServerRpc]
  void ClientReleaseSpellServerRpc(SpellElement elem, Vector3 aimDir, Vector3 projSpawn, float aimShift, float pullDist = 0) {
    ReleaseSpellClientRpc(elem, aimDir, projSpawn, aimShift, pullDist);
  }

    // TEMP: SHITE (note this dist maxes around .55 in this specific)
  [ClientRpc]
  void ReleaseSpellClientRpc(SpellElement elem, Vector3 aimDir, Vector3 projSpawn, float aimShift, float pullDist = 0) {
    // Debug.Log("FIRE ZE MISSILE");
    string projectilePrefab = "Projectile";

    switch(elem) {
      case SpellElement.Earth:
        projectilePrefab += ".Green";
        break;
      case SpellElement.Wind:
        projectilePrefab += ".Yellow";
        break;
      case SpellElement.Fire:
        projectilePrefab += ".Red";
        break;
      case SpellElement.Water:
        projectilePrefab += ".Blue";
        break;
      default: 
        break;
    }


    GameObject stone = Instantiate(Resources.Load(projectilePrefab), projSpawn, Quaternion.identity) as GameObject;

    Rigidbody rBody = stone.GetComponent<Rigidbody>();

    float force = 20f;

    /// TEMP mod the projectile based on bow
    if(pullDist > 0) {
      // Debug.Log(aimShift);
      force = pullDist * 2f * force;
      aimDir.y += aimShift * 10f;
    } 

    rBody.AddForce(aimDir * force);

    // NOTE: just a catch to clean up for now
    Destroy(stone, 20f);
  }
}