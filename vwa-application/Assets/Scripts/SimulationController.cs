using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.AI;

namespace nouhidev
{
    public class SimulationController : MonoBehaviour
    {
        public static SimulationController instance { get; private set; }

        [Header("Simulation Parameters")]
        // How many individuals should be spawned on initialization
        [Range(10, 500)] public int populationSize = 100;
        // As death is neglected in the simulation, the population is constant.
        // Likewise, neither age nor sex or other characteristics are assigned to the individuals
        // part of the system. There are also no immune systems interfering with the disease.
        // Each person is only distinguishable by their position and state at a given time step.
        public List<Individual> population = new List<Individual>();
        // A time step can be interpreted as a singular day. Thus, the results provided
        // by this model are by the day. This forms an indivisible unit of time. During such a time
        // step, a person cannot experience more than one change of state. Changes in state only
        // become apparent the following day; if a susceptible person is infected, it becomes solely
        // visible the next day.
        [Range(1.0f, 10.0f)] public float timeStepLengthInSeconds = 1.0f;
        // Keeps track of the time steps passed
        public int timeStepsPassed = 0;

        [Header("Disease Parameters")]
        // How many percent of individuals should be infectious intially
        [Range(0.0f, 1.0f)] public float infectiousOnStart = 0.01f;
        // The radius within which susceptible individuals have a chance
        // of turning infectious.This parameter could be interpreted as a quantification of social
        // activity or social distancing
        [Range(1.0f, 20.0f)] public float transmissionRadius = 3;
        // The probability of a susceptible individual becoming infectious. 
        // This parameter could be interpreted as the level of hygiene or health standard in a
        // country. Non-transferable Immunity: Individuals part of the recovered state are neither
        // able to contract the disease thus neither able to transmit it
        [Range(0.0f, 1.0f)] public float transmissionProbability = 0.2f;
        // Determines whether recovered individuals will become susceptible again 
        public bool resusceptibility = false;
        // Determines the number of time steps after recovering are needed
        // to become susceptible again
        [Range(10, 30)] public int timeStepsUntilResusceptibility = 20;
        // fter the contraction of the disease, a duration is randomly set between
        //two constants that determines when the person recovers
        [Range(0, 60)] public int recoveryTimeInTimeSteps = 25;
        // Determines whether external reinfection should occur
        public bool externalReinfection = false;
        // Time step interval until external reinfection
        [Range(30, 120)] public int timeStepIntervalUntilExternalReinfection = 60;

        [Header("Social Distancing Parameters")]
        // Amount of social distancing practiced
        [Range(0.1f, 2.0f)] public float socialDistancingAmount = 0.5f;
        // Amount of individuals abiding social distancing
        [Range(0.0f, 1.0f)] public float socialDistancingAbiding = 0.9f;

        [Header("Point of Interest Parameters")]
        // Stores the point of interest game object
        [SerializeField] private GameObject pointOfInterestObj;
        // Handles point of interest introduction
        public bool pointOfInterest = false;
        // The probability of traveling to a point of interest each time step
        [Range(0.0f, 1.0f)] public float pointOfInterestTravelProbability = 0.04f;

        [Header("Vaccination Parameters")]
        // Handles point of interest introduction
        public bool vaccination = false;
        // The fraction of individuals being administered the vaccine each time step
        [Range(0.0f, 1.0f)] public float vaccinatedFraction = 0.05f;
        // Amount of time steps until introduction of vaccine
        [Range(0, 120)] public int timeStepsUntilVaccineIntroduction = 10;

        [Header("Quarantine Parameters")]
        // Handles quarantine introduction
        public bool quarantine = false;
        // Amount of time steps until infectious individual is put in quarantine
        [Range(0, 20)] public int timeStepsUntilQuarantine = 4;

        [Header("Departments Parameters")]
        // Handles departments introduction
        public bool departments = false;
        // The probability of traveling to a department each time step
        [Range(0.0f, 1.0f)] public float departmentsTravelProbability = 0.02f;
        // Stores all departments
        public List<GameObject> departmentsList = new List<GameObject>();

        [Header("Materials")]
        public Material infectiousMat;
        public Material recoveredMat;
        public Material susceptibleMat;
        public Material vaccinatedMat;
        [SerializeField] private Material lightGround, darkGround;


        [Header("Important Prefabs")]
        public GameObject transmissionPulseObj;
        [SerializeField] private GameObject individualPrefab;
        [SerializeField] private MeshRenderer groundMeshRenderer;

        [Header("Important References")]
        public GameObject quarantineObj;
        public GameObject groundObj;

        [Header("Pathfinding")]
        public LayerMask GROUND_LAYER;
        public LayerMask QGROUND_LAYER;

        [Header("Misc")]
        public List<NavMeshSurface> surfaces = new List<NavMeshSurface>();

        // Private Fields
        private Coroutine simulationRoutine;
        private bool breakingPointReached, recoveryPointReached, survivalPointReached;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void Start() => InitializeSimulation();

        private void SimulationBehaviour()
        {
            // Check if external reinfection is enabled and call the corresponding method
            if (externalReinfection)
            {
                ExternalReinfection();
            }

            // Check if the population is not empty, if enough time has passed and if vaccination is enabled, 
            // then administer the vaccine to individuals
            if (population.Any() && timeStepsPassed >= timeStepsUntilVaccineIntroduction && vaccination)
            {
                AdministerVaccine();
            }

            // Iterate over each individual in the population and perform their actions
            population.ForEach(individual =>
            {
                // Check if resusceptibility is enabled and call the corresponding method
                if (resusceptibility)
                {
                    individual.CheckForResusceptibility();
                }

                // Call the methods responsible for the individual's travel behavior
                individual.PointOfInterestTravel();
                individual.DepartmentsTravel();

                // Let the individual determine it's current department
                individual.DetermineDepartment();

                // Check if the individual is infectious and call the corresponding method to transmit the disease
                if (individual.IndividualState == State.Infectious)
                {
                    individual.TransmissionImpulse();

                    // Check if quarantine is enabled and call the corresponding method to quarantine the individual
                    if (quarantine)
                    {
                        individual.CheckForQuarantine();
                    }
                }
            });
        }

        IEnumerator SimulationRoutine()
        {
            // Calls simulation behaviour
            SimulationBehaviour();

            // Handles time step increase
            IncreaseTimeStep();

            // Determines whether any significant points have been reached
            CalculateSignificantPoints();

            // Let a time step pass
            yield return new WaitForSeconds(timeStepLengthInSeconds);

            // Loop the routine
            simulationRoutine = StartCoroutine(SimulationRoutine());
        }

        private void IncreaseTimeStep()
        {
            // Increase the time step
            timeStepsPassed++;

            // Update the time step indicator
            SimulationUIController.instance.timeText.text = timeStepsPassed.ToString();

            // Update the graph with the four states
            SimulationUIController.instance.UpdateGraph(
                GetIndividualsByState(State.Susceptible),
                GetIndividualsByState(State.Infectious),
                GetIndividualsByState(State.Recovered),
                GetIndividualsByState(State.Vaccinated));
        }

        public void ChangeGroundBrightness(bool dark)
        {
            groundMeshRenderer.material = dark ? darkGround : lightGround;
        }

        public void InitializeSimulation()
        {
            // Reset simulation each time it is initialized to clear it out
            ResetSimulation();

            // Set the sliders to their according parameter value
            SimulationUIController.instance.UpdateParameterSliders();

            // Handles spawning the individuals on the plane(s)
            SpawnIndividuals();

            // Update the current simulation routine to this one
            simulationRoutine = StartCoroutine(SimulationRoutine());
        }

        // This method spawns the initial population of the simulation by creating a number of individuals
        // equal to the population size. It generates random spawn coordinates within a square centered on the
        // origin with side length 50. It then instantiates an individual prefab at each set of coordinates.
        // Finally, it calls two methods to infect a portion of the population and select individuals who will
        // not abide by social distancing measures
        private void SpawnIndividuals()
        {
            // Determine random spawn coordinates
            var spawnCoords = Enumerable.Range(0, populationSize)
                .Select(_ => new Vector3(Random.Range(-25.0f, 25.0f), 0.0f, Random.Range(-25.0f, 25.0f)))
                .ToArray();
            
            if (departments)
            {
                spawnCoords = Enumerable.Range(0, populationSize)
                .Select(_ => new Vector3(Random.Range(-52.0f, 52.0f), 0.0f, Random.Range(-52.0f, 52.0f)))
                .ToArray();
            }

            // Spawn individuals
            for (int i = 0; i < populationSize; i++)
            {
                GameObject individual = Instantiate(individualPrefab, spawnCoords[i], Quaternion.identity);
            }

            // If departments are enabled update individuals
            if (departments)
            {
                // Set the point of interest flag for each individual in the population
                foreach (var individual in population)
                {
                    individual.departments = true;
                }
            }

            // Infect a fraction of the population
            Invoke("InfectInitially", 0.001f);

            // Select a fraction of the population that do not practice social distancing
            Invoke("SelectSocialDistancingNotAbiders", 0.001f);
        }

        // This method initializes the disease by infecting a portion of the population based on the
        // infectiousOnStart parameter. It finds the specified number of individuals who are initially
        // susceptible and changes their state to infectious. It then handles the point of interest
        private void InfectInitially()
        {
            // Determine number of individuals to initially infect
            int amountOfInfectiousOnStart = Mathf.RoundToInt(populationSize * infectiousOnStart);

            // Store them in a list
            var infectiousIndividuals = population.Where(i => i.IndividualState == State.Susceptible)
                                                  .Take(amountOfInfectiousOnStart)
                                                  .ToList();

            // Infect the individuals from the list
            infectiousIndividuals.ForEach(i => i.ChangeState(State.Infectious));
        }

        private void SelectSocialDistancingNotAbiders()
        {
            // Calculate the number of individuals who will not abide by social distancing measures
            int notAbidingSocialDistancing = Mathf.RoundToInt(populationSize * (1 - socialDistancingAbiding));

            // Select the first 'notAbidingSocialDistancing' individuals from the population
            var individualsToChange = population.Take(notAbidingSocialDistancing);

            // Change the social distancing behavior of the selected individuals
            foreach (var individual in individualsToChange)
            {
                individual.ChangeSocialDistancing(false);
            }
        }

        public void TogglePointOfInterest(bool toggle)
        {
            // Set the point of interest flag for each individual in the population
            foreach (var individual in population)
            {
                individual.pointOfInterest = toggle;
            }

            // Activate or deactivate the point of interest object
            pointOfInterestObj.SetActive(toggle);
        }

        public void ToggleQuarantine(bool toggle)
        {
            // Toggle the quarantine flag
            quarantine = toggle;

            // Activate or deactivate the quarantine object
            quarantineObj.SetActive(toggle);

            // Update the NavMesh so NavMeshAgents don't get confused
            RebuildNavMesh();
        }

        public void UpdateSocialDistancing()
        {
            // Update the individuals social distancing amount
            foreach (var individual in population)
            {
                individual.GetComponent<NavMeshAgent>().radius = socialDistancingAmount;
            }
        }

        private void AdministerVaccine()
        {
            // Determine desired amount of susceptibles to administer the vaccine to
            int amountOfVaccinesToAdminister = Mathf.RoundToInt(populationSize * vaccinatedFraction);

            // Find all susceptible individuals
            var susceptibles = population.Where(individual => individual.IndividualState == State.Susceptible);

            // Administer vaccines to susceptibles up to the desired amount
            foreach (var susceptible in susceptibles.Take(amountOfVaccinesToAdminister))
            {
                susceptible.ChangeState(State.Vaccinated);
            }
        }

        // Resets the simulation, stopping the simulation coroutine and removing all individuals.
        private void ResetSimulation()
        {
            // Stop the simulation coroutine if it's currently running.
            if (simulationRoutine != null)
            {
                StopCoroutine(simulationRoutine);
            }

            // Remove all individuals from the population.
            RemoveAllIndividuals();

            // Resets time steps
            timeStepsPassed = 0;

            // Reset Graph
            SimulationUIController.instance.ClearGraph();

            breakingPointReached = false;
            survivalPointReached = false;
            recoveryPointReached = false;
        }

        // Removes all individual game objects and clears the population list.
        private void RemoveAllIndividuals()
        {
            // Destroy each individual game object and remove it from the population list.
            for (int i = 0; i < population.Count; i++)
            {
                Destroy(population[i].gameObject);
            }

            // Clear the population list.
            population.Clear();
        }

        public int GetIndividualsByState(State state)
        {
            int count = population.Count(individual => individual.IndividualState == state);
            return count;
        }

        private void CalculateSignificantPoints()
        {
            // Calculate Breaking Point
            if (GetIndividualsByState(State.Infectious) > GetIndividualsByState(State.Susceptible))
            {
                if (!breakingPointReached)
                {
                    SimulationUIController.instance.breakingPointContainer.SetActive(true);
                    SimulationUIController.instance.breakingPointContainer.GetComponentInChildren<TextMeshProUGUI>().text = timeStepsPassed.ToString();
                    breakingPointReached = true;
                }
            }
            // Calculate Recovery Point
            if (GetIndividualsByState(State.Recovered) > GetIndividualsByState(State.Infectious))
            {
                if (!recoveryPointReached)
                {
                    SimulationUIController.instance.recoveryPointContainer.SetActive(true);
                    SimulationUIController.instance.recoveryPointContainer.GetComponentInChildren<TextMeshProUGUI>().text = timeStepsPassed.ToString();
                    recoveryPointReached = true;
                }
            }
            // Calculate Survival Point
            if (GetIndividualsByState(State.Infectious) == 0 && timeStepsPassed > recoveryTimeInTimeSteps)
            {
                if (!survivalPointReached)
                {
                    SimulationUIController.instance.survivalPointContainer.SetActive(true);
                    SimulationUIController.instance.survivalPointContainer.GetComponentInChildren<TextMeshProUGUI>().text = timeStepsPassed.ToString();
                    survivalPointReached = true;
                }
            }
        }

        public void ToggleDepartments(bool toggle)
        {
            // Set the departments flag for each individual in the population
            foreach (var individual in population)
            {
                individual.departments = toggle;
            }

            // Disable the single ground plane
            groundObj.SetActive(!toggle);

            // Enable the department planes
            foreach (GameObject department in departmentsList)
            {
                department.SetActive(toggle);
            }

            RebuildNavMesh();

            // Move the quarantine accordingly so it does not overlap
            if (toggle)
            {
                InitializeSimulation();
                quarantineObj.transform.position += new Vector3(-30, 0, 0);
            }
            else
            {
                InitializeSimulation();
                quarantineObj.transform.position = new Vector3(0, 0, 0);
            }
        }

        private void RebuildNavMesh()
        {
            // Rebuild the NavMesh
            NavMesh.RemoveAllNavMeshData();

            // Update the Navmesh
            foreach (NavMeshSurface surface in surfaces)
            {
                surface.BuildNavMesh();
            }
        }

        // In real-life scenarios, diseases can be externally introduced through various means, for instance, human travel.
        // Therefore, if enabled, after a number of time steps, infectious individuals will be
        // introduced into the system. This is supposed to resemble the concept of human travel. 
        // This method handles that concept.
        private void ExternalReinfection()
        {
            if (timeStepsPassed % timeStepIntervalUntilExternalReinfection == 0)
            {
                int amountOfInfectiousOnStart = Mathf.RoundToInt(populationSize * infectiousOnStart);

                var spawnCoords = new Vector3(Random.Range(-25.0f, 25.0f), 0.0f, Random.Range(-25.0f, 25.0f));
                if (departments) new Vector3(Random.Range(-52.0f, 52.0f), 0.0f, Random.Range(-52.0f, 52.0f));

                for (int i = 0; i < amountOfInfectiousOnStart; i++)
                {
                    GameObject individual = Instantiate(individualPrefab, spawnCoords, Quaternion.identity);
                    individual.GetComponent<Individual>().ChangeState(State.Infectious);
                }

                populationSize += amountOfInfectiousOnStart;
            }
        }
    }

}