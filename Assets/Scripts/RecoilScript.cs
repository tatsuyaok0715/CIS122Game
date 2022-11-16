using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecoilScript : MonoBehaviour
{
    public GameObject M40A3_Rifle;
    void Start()
    {
        
    }

    
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartCoroutine(StartRecoil());
        }
    }

    IEnumerator StartRecoil()
    {
        M40A3_Rifle.GetComponent<Animator>().Play("Recoil");
        yield return new WaitForSeconds(0.20f);
        M40A3_Rifle.GetComponent<Animator>().Play("New State");
    }
}
