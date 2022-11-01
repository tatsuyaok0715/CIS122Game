using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    [SerializeField] GunData gunData;

    public void Shoot()
    {
        Debug.Log("Shoot Gun");
    }
}
