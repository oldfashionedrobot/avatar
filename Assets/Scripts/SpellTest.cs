using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellTest : MonoBehaviour {

  public enum SpellState {
    None = 0,
    ElementSelect = 1,
    SpellCast = 2
  }

  public enum SpellElement {
    Fire = 0,
    Water = 1,
    Earth = 2,
    Air = 3
  }

  public GameObject elementSelect;
  public GameObject genericSpell;
  public GameObject fireSpell;

  private SpellState state = SpellState.None;
  private SpellElement element = SpellElement.Air;

  public void OnChildTriggerEntered(Collider other, GameObject triggered) {
    switch (state) {
      case SpellState.ElementSelect:
        SelectElement(triggered.name);
        break;
      case SpellState.SpellCast:
        HandleSpellCast(triggered);
        break;
      case SpellState.None:
      default:
        break;
    }
  }

  public bool ActivateElementSelect() {
    if (state != SpellState.ElementSelect) {
      state = SpellState.ElementSelect;
      elementSelect.SetActive(true);
      return true;
    } else {
      return false;
    }
  }

  // Start is called before the first frame update
  void Start() {

  }

  // Update is called once per frame
  void Update() {

  }

  void OnEnable() {
    state = SpellState.None;
    elementSelect.SetActive(false);
    genericSpell.SetActive(false);
  }

  void OnDisable() {
    state = SpellState.None;
    elementSelect.SetActive(false);
    genericSpell.SetActive(false);
  }

  private void SelectElement(string elementSelected) {
    Debug.Log("SELECTED " + elementSelected);

    state = SpellState.SpellCast;
    elementSelect.SetActive(false);

    switch (elementSelected) {
      case "Fire":
        element = SpellElement.Fire;
        fireSpell.SetActive(true);
        InitSpellCast();
        break;
      case "Water":
        element = SpellElement.Water;
        genericSpell.SetActive(true);
        InitSpellCast();
        break;
      case "Earth":
        element = SpellElement.Earth;
        genericSpell.SetActive(true);
        InitSpellCast();
        break;
      default:
        element = SpellElement.Earth;
        genericSpell.SetActive(true);
        InitSpellCast();
        break;
    }
  }

  // TODO: each spell should have its own script to drive
  // here is a basic logic for a generic "bowl" action

  private int castingState = 0;
  private GameObject spellStart;
  private GameObject spellCharge;

  private void InitSpellCast() {
    // NOTE: hacky stuff here, need to move all specific spell logic into their own
    // polymorphic spell thingies

    if (element != SpellElement.Fire) {
      castingState = 0;
      spellStart = genericSpell.transform.Find("Start").gameObject;
      spellStart.SetActive(true);

      spellCharge = genericSpell.transform.Find("Charge").gameObject;
      spellCharge.SetActive(false);
    } else {
      castingState = 1;
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
      // release the spell cast
      castingState = 2;
      spellStart.SetActive(false);
      spellCharge.SetActive(false);
      ReleaseSpellCast();
    }

  }

  private void ReleaseSpellCast() {
    GameObject stone = Instantiate(Resources.Load("Projectile"), spellStart.transform.position, Quaternion.identity) as GameObject;

    Rigidbody rBody = stone.GetComponent<Rigidbody>();
    Ray camAim = Camera.main.ScreenPointToRay(new Vector3((Screen.width * .6f), (Screen.height * .6f), 0));
    Vector3 targetPoint = camAim.GetPoint(100f);
    Vector3 aimDir = targetPoint - stone.transform.position;
    aimDir.y += 10f;

    rBody.AddForce(aimDir * 20f);

    // NOTE: just a catch to clean up for now
    Destroy(stone, 20f);

    // reset to base state
    castingState = 0;
    state = SpellState.None;
    elementSelect.SetActive(false);

    // turn off all spells for now, should be handled with polymorphic spells
    genericSpell.SetActive(false);
    fireSpell.SetActive(false);
  }

}