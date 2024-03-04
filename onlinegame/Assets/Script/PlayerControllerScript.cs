using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerControllerScript : NetworkBehaviour
{
    public float speed = 5.0f;
    public float rotationSpeed = 10.0f;


    private Animator animator;
    private Rigidbody rb;
    private bool running;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        running = false;
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

        moveForward();
        turn();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Banana")
        {
            StartCoroutine(Freeze());
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
}