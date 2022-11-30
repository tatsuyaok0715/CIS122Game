using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Respawn : MonoBehaviour
{
    public GameObject monster;

    private void Update()
    {
        if (GameObject.Find("NightmareDragon") == null){

            Instantiate(monster);

        }



    }

}