using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BowTest : MonoBehaviour
{

    public enum BowState {
        None = 0,
        ElementSelect = 1,
        SpellCast = 2
    }

    private BowState state = BowState.None;
    public void ActivateBowAction() {

    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    void OnEnable() {
      state = BowState.None;
    }

    void OnDisable() {
      state = BowState.None;
    }
}
