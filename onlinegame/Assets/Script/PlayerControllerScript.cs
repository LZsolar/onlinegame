using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using QFSW.QC.Actions;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerControllerScript : NetworkBehaviour
{
    public float speed = 5.0f;
    public float rotationSpeed = 10.0f;


    private Animator animator;
    private Rigidbody rb;
    private bool running;
    public bool isDie = false;

    public bool isPunching = true;
    GameObject dieMenu;


    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        running = false;
        dieMenu = GameObject.FindGameObjectWithTag("DeadMenu");
        dieMenu.SetActive(false);
    }

    void moveForward()
    {
        float verticalInput = Input.GetAxis("Vertical");
        if (Mathf.Abs(verticalInput) > 0.01f)
        {
            // move forward only
            if (verticalInput > 0.01f)
            {
                float translation = verticalInput * speed;
                translation *= Time.fixedDeltaTime;
                rb.MovePosition(rb.position + this.transform.forward * translation);

                if (!running)
                {
                    running = true;
                    animator.SetBool("Running", true);
                }
            }
        }
        else if (running)
        {
            running = false;
            animator.SetBool("Running", false);
        }
    }

    void turn()
    {
        float rotation = Input.GetAxis("Horizontal");
        if (rotation != 0)
        {
            rotation *= rotationSpeed;
            Quaternion turn = Quaternion.Euler(0f, rotation, 0f);
            rb.MoveRotation(rb.rotation * turn);
        }
        else
        {
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void FixedUpdate()
    {
        if(!IsOwner) { return; }
        moveForward();
        turn();
    }
    private void Update()
    {
        if (!IsOwner) { return; }
        if (Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(FreezeForPunch());
        }
        if (isDie)
        {
            dieMenu.SetActive(true);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(!IsOwner) return;
        if(collision.gameObject.tag == "Banana")
        {
            StartCoroutine(Freeze());
        }
        
    }
    private void OnCollisionStay(Collision collision)
    {
        if (!IsOwner) return;

        if (collision.gameObject.tag == "Player")
        {
            PlayerControllerScript thatPerson = collision.gameObject.GetComponent<PlayerControllerScript>();
            if (thatPerson.isPunching) { 
                thatPerson.animator.SetBool("isDie", true); 
                thatPerson.isDie = true;
            }
        }
    }

    IEnumerator Freeze()
    {
        animator.SetBool("Banana",true);
        float prvSpeed = speed;
        speed = 0f;
        yield return new WaitForSeconds(2);
        animator.SetBool("Banana", false);
        speed = prvSpeed;
    }
    IEnumerator FreezeForPunch()
    {
        animator.SetBool("isPunch", true); isPunching = true;
        float prvSpeed = speed;
        speed = 0f;
        yield return new WaitForSeconds(1);
        animator.SetBool("isPunch", false); speed = prvSpeed;// isPunching = false;
    }
}