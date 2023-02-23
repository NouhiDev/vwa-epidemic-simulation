using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;

namespace nouhidev
{
    public enum State
    {
        Susceptible,
        Infectious,
        Recovered,
        Vaccinated
    }

    [RequireComponent(typeof(NavMeshAgent))]
    public class Individual : MonoBehaviour
    {
        [Header("Compartmental Settings")]
        // Stores the individuals state (Susceptible, Infectious, Recovered)
        public State IndividualState = State.Susceptible;
        // Determines the behaviour practiced by the individual 
        public bool pointOfInterest, departments, socialDistancing;

        [Header("AI Settings/General")]
        // Stores the NavMeshAgent (Agent that controls behaviour)
        [SerializeField] private NavMeshAgent agent;

        [Header("AI Settings/Pathfinding")]
        // Stores the temporary point the individual will move towards
        [SerializeField] private Vector3 walkPoint;
        // Determines whether the point the individual will move towards is set
        // and decides whether it is reachable
        [SerializeField] private bool walkPointSet;
        // Determines the range in which an individual can decide to move
        [SerializeField] private float walkPointRange = 20;
        // Used for checking if the point the individual will towards to is
        // reachable and on ground
        [SerializeField] Transform groundCheck;
        // Stores all the individuals physical parts
        [SerializeField] MeshRenderer[] individualBody;
        // Keeps track of how long the individual has been infected
        public int timeUnitsInfectious = 0;
        // Helper field storing the transmission range
        private float transmissionRange;
        // Helper field storing the ground layer
        [SerializeField] private LayerMask groundLayer;
        // Resusceptibility
        public int timeStepAtWhichResusceptibility = 9999;
        // Asymptomatic
        // public bool unwillingToCooperateProbability = false;

        [Header("Quarantine")]
        // Keeps track of the individuals quarantine status
        private bool isQuarantined = false;
        // Determines the range in which an individual can decide to move
        // in quarantine
        private float quarantineWalkPointRange = 5.0f;

        [Header("Departments")]
        public GameObject departmentIndividualIsOn;

        private void Awake()
        {
            SimulationController.instance.population.Add(this);
        }

        // Add individual to the population on initialization
        private void Start()
        {
            groundLayer = SimulationController.instance.GROUND_LAYER;
            ChangeState(IndividualState);
            if (socialDistancing) agent.radius = SimulationController.instance.socialDistancingAmount;
            transmissionRange = SimulationController.instance.transmissionRadius;
            DetermineDepartment();
        }

        private void Update()
        {
            IndividualBehaviour();
        }

        private void IndividualBehaviour()
        {
            IndividualMovement();
        }

        #region Individual Movement Related

        private void IndividualMovement()
        {
            RandomMovement();
        }

        private void RandomMovement()
        {
            // Searches for a walkpoint if none is selected
            if (!walkPointSet) SearchMovePoint();

            // Sets the individuals destination to the walkpoint
            if (walkPointSet) agent.SetDestination(walkPoint);

            // Calculates the distance from the individual to the walkpoint
            Vector3 distanceToWalkPoint = transform.position - walkPoint;

            // Determines whether the individual is within 1 unit of the walkpoint
            // and if it is it generates a new one
            if (distanceToWalkPoint.magnitude < 2.5f) walkPointSet = false;
        }

        private void SearchMovePoint()
        {
            // Generates a random X coordinate in the walk point range
            float randomX = UnityEngine.Random.Range(-walkPointRange, walkPointRange);
            // Generates a random Z coordinate in the walk point range
            float randomZ = UnityEngine.Random.Range(-walkPointRange, walkPointRange);

            // Combines the random coordinates to a Vector
            walkPoint = new Vector3(transform.position.x + randomX, 0.5f, transform.position.z + randomZ);

            RaycastHit hit = new RaycastHit();

            // Checks whether the walk point is valid
            if (Physics.Raycast(walkPoint, -transform.up, out hit, groundLayer))
            {
                if (departments)
                {
                    if (departmentIndividualIsOn == null) return;

                    if (hit.transform.tag == departmentIndividualIsOn.transform.tag)
                    {
                        walkPointSet = true;
                        if (!isQuarantined) ChangeSocialDistancing(true);
                    }
                }
                else
                {
                    walkPointSet = true;
                    if (!isQuarantined) ChangeSocialDistancing(true);
                }
            }

            if (isQuarantined) return;
            if (departments) return;
            // Walkpoint Error Corrections
            if (Math.Abs(walkPoint.x) > 26 || Math.Abs(walkPoint.z) > 26)
            {
                walkPointSet = false;
            }
        }

        public void DetermineDepartment()
        {
            if (!departments) return;

            RaycastHit hit;
            if (Physics.Raycast(transform.position + new Vector3(0, 1, 0), -Vector3.up, out hit, groundLayer))
            {
                departmentIndividualIsOn = hit.transform.gameObject;
            }
        }

        public void PointOfInterestTravel()
        {
            if (!pointOfInterest) return;
            if (isQuarantined) return;

            float randProb = UnityEngine.Random.Range(0.0f, 1.0f);
            if (randProb <= SimulationController.instance.pointOfInterestTravelProbability)
            {
                walkPoint = Vector3.zero;
                walkPointSet = true;
                agent.radius = 0.1f;
            }
        }

        public void DepartmentsTravel()
        {
            if (!departments) return;
            if (isQuarantined) return;

            float randProb = UnityEngine.Random.Range(0.0f, 1.0f);
            if (randProb <= SimulationController.instance.departmentsTravelProbability)
            {
                // Determine Department to travel to
                GameObject departmentToTravelTo = null;
                List<GameObject> possibleDepartments = new List<GameObject>(SimulationController.instance.departmentsList);
                possibleDepartments.Remove(departmentIndividualIsOn);
                departmentToTravelTo = possibleDepartments[UnityEngine.Random.Range(0, possibleDepartments.Count)];

                // Move Agent to Department
                agent.enabled = false;
                transform.position = departmentToTravelTo.transform.position;
                agent.enabled = true;
                walkPointSet = false;

                DetermineDepartment();
            }
        }

        #endregion

        #region Individual Transmission Related

        public void TransmissionImpulse()
        {
            // Check if individual recovers
            if (CheckForRecovery()) return;

            // Increase individual's time being infected
            timeUnitsInfectious++;

            // Visual feedback of transmission impulse
            TransmissionVisualImpulse();

            // Check for People close to oneself
            Collider[] peopleInRange = Physics.OverlapSphere(transform.position, transmissionRange);

            foreach (var person in peopleInRange)
            {
                // Return if collided object is not an individual
                if (!person.GetComponent<Individual>()) return;
                // Select collided object as an individual
                Individual selectedPerson = person.GetComponent<Individual>();

                // Check individual is susceptible
                if (selectedPerson.IndividualState == State.Susceptible)
                {
                    //print("ATTEMPTING INFECTION.");
                    // Attempt infection
                    float rand = UnityEngine.Random.Range(0.0f, 1.0f);

                    if (rand <= SimulationController.instance.transmissionProbability)
                    {
                        person.GetComponent<Individual>().ChangeState(State.Infectious);
                        //rand = UnityEngine.Random.Range(0.0f, 1.0f);
                        //if (rand < SimulationController.instance.unwillingToCooperateProbability)
                        //{
                        //    person.GetComponent<Individual>().unwillingToCooperateProbability = true;
                        //}
                    }
                }
            }

        }

        private void TransmissionVisualImpulse()
        {
            // Visual Pulse
            Instantiate(SimulationController.instance.transmissionPulseObj, transform.position + new Vector3(0, 1.5f, 0),
                Quaternion.identity, transform);
        }


        public void ChangeState(State _state)
        {
            // Update individuals state to the one passed by the method
            IndividualState = _state;
            foreach (MeshRenderer renderer in individualBody)
            {
                switch (_state)
                {
                    case State.Susceptible:
                        renderer.material = SimulationController.instance.susceptibleMat;
                        break;
                    case State.Infectious:
                        renderer.material = SimulationController.instance.infectiousMat;
                        break;
                    case State.Recovered:
                        renderer.material = SimulationController.instance.recoveredMat;
                        if (isQuarantined) LeaveQuarantine();
                        timeStepAtWhichResusceptibility = SimulationController.instance.timeStepsPassed + SimulationController.instance.timeStepsUntilResusceptibility;
                        break;
                    case State.Vaccinated:
                        renderer.material = SimulationController.instance.vaccinatedMat;
                        break;
                }
            }
        }

        private bool CheckForRecovery()
        {
            if (timeUnitsInfectious >= SimulationController.instance.recoveryTimeInTimeSteps)
            {
                ChangeState(State.Recovered);
                return true;
            }
            else return false;
        }

        public void CheckForQuarantine()
        {
            if (timeUnitsInfectious >= SimulationController.instance.timeStepsUntilQuarantine && !isQuarantined)
            {
                agent.radius = 0.1f;
                groundLayer = SimulationController.instance.QGROUND_LAYER;
                isQuarantined = true;
                transmissionRange = 0.1f;
                walkPointRange = quarantineWalkPointRange;
                agent.enabled = false;
                transform.position = SimulationController.instance.quarantineObj.transform.GetChild(0).position;
                agent.enabled = true;
                walkPointSet = false;
            }
        }

        public void LeaveQuarantine()
        {
            groundLayer = SimulationController.instance.GROUND_LAYER;
            transmissionRange = SimulationController.instance.transmissionRadius;
            walkPointRange = 20;
            agent.enabled = false;
            if (!departments)
            {
                transform.position = SimulationController.instance.groundObj.transform.position;
            }
            else
            {
                transform.position = SimulationController.instance.departmentsList[UnityEngine.Random.Range(0, SimulationController.instance.departmentsList.Count)].transform.position;
            }
            agent.enabled = true;
            walkPointSet = false;
            agent.radius = SimulationController.instance.socialDistancingAmount;
        }

        #endregion

        public void ChangeSocialDistancing(bool toggle)
        {
            if (toggle)
            {
                agent.radius = SimulationController.instance.socialDistancingAmount;
                socialDistancing = true;
            }
            else
            {
                agent.radius = 0.1f;
                socialDistancing = false;
            }
        }

        public void CheckForResusceptibility()
        {
            if (SimulationController.instance.timeStepsPassed == timeStepAtWhichResusceptibility)
            {
                ChangeState(State.Susceptible);
                timeUnitsInfectious = 0;
            }
        }
    }
}