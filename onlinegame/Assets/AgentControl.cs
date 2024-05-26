using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public enum AgentState
{
    Idle,
    Move
}

public class AgentControl : MonoBehaviour
{
    [Header("Properties")]
    public float minProcessDelayTime; // Mininum time (seconds) before processing next action
    public float maxProcessDelayTime; // Maximum time (seconds) before processing next action
    public float sphereCheckRadius; // Radius of sphere to check for new goal position

    [Header("Status")]
    public NavMeshAgent agentComponent;
    public AgentState currentState;

    private float delayTime;
    private float delayTimer = 0f;
    private Vector3 goalPosition;

    private Animator animator;


    void Start()
    {
        agentComponent = GetComponent<NavMeshAgent>();
        currentState = AgentState.Idle;
        animator = GetComponentInChildren<Animator>();

    }

    void FindNewGoalPosition()
    {
        // Choose random spot of the navmesh
        while (true)
        {
            Vector3 randomPoint = transform.position + Random.insideUnitSphere * sphereCheckRadius;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, agentComponent.height, NavMesh.AllAreas))
            {
                goalPosition = hit.position;
                break;
            }
        }

         // Set as target for agent
         agentComponent.SetDestination(goalPosition);
    }

    void Update()
    {
        if (currentState == AgentState.Idle && !agentComponent.hasPath)
        {

            delayTimer += Time.deltaTime;

            if (delayTimer >= delayTime)
            {
                delayTimer = delayTimer -= delayTime;
                currentState = AgentState.Move;
                FindNewGoalPosition();
                animator.SetBool("Running", true);
            }
        }
        else if(currentState == AgentState.Move && !agentComponent.hasPath)
        {
            delayTime = Random.Range(minProcessDelayTime, maxProcessDelayTime);
            currentState = AgentState.Idle;
            animator.SetBool("Running", false);

        }
    }
}
