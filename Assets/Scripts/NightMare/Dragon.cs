using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Dragon : MonoBehaviour
{
    public int HP = 100;
    public Slider healthBar;

    public GameObject monster;

    public Animator animator;

    public Vector3 position = new Vector3();

    public float x;
    public float y;
    public float z;



    private bool isanimaldead = false;

    private void Start()
    {
        monster = GameObject.Find("NightmareDragon");
        animator = monster.GetComponent<Animator>();
    }

    void Update(){
        monster = GameObject.Find("NightmareDragon");
        animator = monster.GetComponent<Animator>();
        healthBar.value = HP;
    }

    public void TakeDamage(int damageAmount)
    {
        

        HP -= damageAmount;

        if (HP <= 0)
        {
            
            if (isanimaldead == false)
            {
                AudioManager.instance.Play("DragonDeath");
                isanimaldead = true;
       
                animator.SetTrigger("die");

                x = Random.Range(-25, 26);
                y = 5;
                z = Random.Range(-25, 26);

                monster.transform.position = new Vector3(x,y,z);

                animator.ResetTrigger("die");
                animator.SetTrigger("Patrol State");
                HP = 100;
                isanimaldead = false;
                //spawn enemy
            }

            Debug.Log("monster die");
        }
        else
        {
            AudioManager.instance.Play("DragonDamage");
            animator.SetTrigger("damage");
        }
        Debug.Log("health: " + HP);
    }

}
