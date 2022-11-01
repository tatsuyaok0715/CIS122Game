using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header ("References")]
    [SerializeField] private GunData gunData;
    [SerializeField] private Transform muzzle;

    float timeSinceLastShot;
    private void Start()
    {
        PlayerShoot.shootInput += Shoot();
    }

    private bool CanShoot() => !gunData.reloading && timeSinceLastShot > 1f 
        / (gunData.fireRate / 60f);
    private void Shoot()
    {
        if (gunData.currentAmmo >0)
        {
            if (CanShoot())
            {
                if (Physics.Raycast(muzzle.position, muzzle.foward, 
                    out RaycastHit hitInfo, gunData.maxDistance))
                {
                    Debug.Log(hitInfo.transform.name);
                }
                gunData.currentAmmo--;
                //reduce by 1 everytime
                timeSinceLastShot = 0;
                //OnGunShot(); can be for shooting things later
            }
        }

    }
    private void Update()
    {
        timeSinceLastShot += Time.deltaTime;

        Debug.DrawRay(muzzle.position, muzzle.foward * gunData.maxDistance);
        //to help visualize the muzzle
    }
}
