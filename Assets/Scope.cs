using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scope : MonoBehaviour
{
    public Animator animator;
    public GameObject ScopeOverlay;
    public GameObject WeaponCamera;
    public Camera mainCamera;

    public float scopeFOV = 15f;
    private float normalFOV;
    private bool isScoped = false;
    

    void Update ()
    {
        if (Input.GetButtonDown("Fire2"))
        {
            isScoped = !isScoped;
            animator.SetBool("IsScoped", isScoped);

            ScopeOverlay.SetActive(isScoped);

            if (isScoped)
                StartCoroutine(OnScoped());
            else
                OnUnScoped();
        }
    }


    void OnUnScoped()
    {
        ScopeOverlay.SetActive(false);
        WeaponCamera.SetActive(true);
        mainCamera.fieldOfView = normalFOV;
    }

    IEnumerator OnScoped()
    {
        yield return new WaitForSeconds(.15f);
        ScopeOverlay.SetActive(true);
        WeaponCamera.SetActive(false);

        normalFOV = mainCamera.fieldOfView;
        mainCamera.fieldOfView = scopeFOV;
    }




}
