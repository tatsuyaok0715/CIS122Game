using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecoilScript : MonoBehaviour
{
    public GameObject M40A3_Holder;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartCoroutine(StartRecoil());
        }
    }

    IEnumerator StartRecoil()
    {
        M40A3_Holder.GetComponent<Animator>().Play("Recoil");
        yield return new WaitForSeconds(0.1000f);
        M40A3_Holder.GetComponent<Animator>().Play("New State");
    }
}