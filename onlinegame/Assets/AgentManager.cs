using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class AgentManager : MonoBehaviour
{
    public GameObject agentPrefab;
    public int numberOfAgentOnBegin = 10;

    void Start()
    {
        for (int i = 0; i < numberOfAgentOnBegin; i++)
        {
            GameObject _agent = Instantiate(agentPrefab, this.transform);
            NavMeshAgent _agentComponent = _agent.GetComponent<NavMeshAgent>();
            _agentComponent.baseOffset = _agentComponent.height / 2;
            _agentComponent.avoidancePriority = 99 - i;
            _agentComponent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        }
    }
}
