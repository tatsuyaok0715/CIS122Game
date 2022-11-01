using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    public static Action shootInput;

    private void Update()
    {
        if (Input.GetMousebutton(0))
        {
            if (shootInput.GetMouseButton(0))
            shootInput?.Invoke();


            //? avoids null error exception
        }
    }



}
