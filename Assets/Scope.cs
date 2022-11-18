using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scope : MonoBehaviour
{
    public Animator animator;
    public GameObject ScopeOverlay;

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
    }

    IEnumerator OnScoped()
    {
        yield return new WaitForSeconds(.15f);
        ScopeOverlay.SetActive(true);   
    }




}
