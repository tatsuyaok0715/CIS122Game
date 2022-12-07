using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class spawnBullet : MonoBehaviour
{
    private Vector3 barrel_Pos;
    private float timeOfFlight = 0f; 

    [Header("Select Prefeb")]
    public GameObject bullet_prefab;

    public GameObject muzzle;

    // Start is called before the first frame update
    void Start()
    {   

        muzzle = GameObject.Find("muzzle_Ref");
        
    }

    // Update is called once per frame
    void Update()
    {       

        if (gameObject != null)
        {    
        
            //Destroy(bullet_prefab, 2);
        }   
        

        if (Input.GetMouseButtonDown(0)){

            //Debug.Log("Spawn");
            SpawnBullet();


        }
    }

    void SpawnBullet() {

        if (muzzle){

            barrel_Pos = muzzle.transform.position;
        }

        var bullet_Clone = Instantiate(bullet_prefab,barrel_Pos, Quaternion.identity);
        Destroy(bullet_Clone, 10f);
    
    }

}
