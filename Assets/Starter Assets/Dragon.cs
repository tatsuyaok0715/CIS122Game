using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dragon : MonoBehaviour
{
    public int HP = 100;
    public Animator animator;
    private bool isanimaldead = false;

    public void TakeDamage(int damageAmount)
    {
        

        HP -= damageAmount;

        if (HP <= 0)
        {
            
            if (isanimaldead == false)
            {
                animator.SetTrigger("die");
                isanimaldead = true;
            }
            
            Debug.Log("monster die");
        }
        else
        {
            animator.SetTrigger("damage");
        }
        Debug.Log("health: " + HP);
    }






}
