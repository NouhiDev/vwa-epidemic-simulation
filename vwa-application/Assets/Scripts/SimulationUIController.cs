using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace nouhidev
{
    public class SimulationUIController : MonoBehaviour
    {
        public static SimulationUIController instance { get; private set; }

        [Header("Settings Container")]
        [SerializeField] private bool settingsOn = false;
        [SerializeField] private GameObject settingsContainer;
        [SerializeField] private Animator settingsContainerAnim;
        [SerializeField] private Sprite settingsSprite;
        [SerializeField] private Sprite settingsArrow;
        [SerializeField] private Image settingsIcon;

        [Header("Graph Container")]
        public GraphContainer graphContainer;
        [SerializeField] private bool graphOn = false;
        [SerializeField] private Animator graphContainerAnim;
        [SerializeField] private Sprite graphSprite;
        [SerializeField] private Sprite graphArrow;
        [SerializeField] private Image graphIcon;

        public TextMeshProUGUI timeText;

        [Header("Simulation Parameters")]
        [SerializeField] private Slider timeStepSlider;
        [SerializeField] private Slider cameraZoomSlider;

        [Header("Disease Parameters")]
        [SerializeField] private Slider transmissionRadiusSlider;
        [SerializeField] private Slider transmissionProbabilitySlider;
        [SerializeField] private Slider recoveryTimeSlider;
        [SerializeField] private Slider groundBrightnessSlider;
        [SerializeField] private Slider populationSizeSlider;
        [SerializeField] private Slider infectiousOnStartSlider;
        [SerializeField] private Slider timeStepsUntilResusceptibleSlider;
        [SerializeField] private Slider timeStepIntervalUntilReinfectionSlider;
        [SerializeField] private Slider extReinfectionSlider;
        [SerializeField] private Slider resusceptibilitySlider;
        [SerializeField] private Slider socialDistancingAmountSlider;
        [SerializeField] private Slider socialDistancingAbidingSlider;
        [SerializeField] private Slider pointOfInterestSlider;
        [SerializeField] private Slider pointOfInterestTravelPossibilitySlider;
        [SerializeField] private Slider vaccinationSlider;
        [SerializeField] private Slider vaccinationFractionSlider;
        [SerializeField] private Slider vaccinationDelaySlider;
        [SerializeField] private Slider quarantineSlider;
        [SerializeField] private Slider quarantineDelaySlider;
        [SerializeField] private Slider departmentsSlider;
        [SerializeField] private Slider departmentsTravelPossibilitySlider;

        [Header("Significant Points")]
        public GameObject breakingPointContainer;
        public GameObject recoveryPointContainer;
        public GameObject survivalPointContainer;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void Start() => UpdateParameterSliders();

        public void UpdateParameterSliders()
        {
            SimulationController instance = SimulationController.instance;

            timeStepSlider.value = instance.timeStepLengthInSeconds;
            populationSizeSlider.value = instance.populationSize;

            transmissionRadiusSlider.value = instance.transmissionRadius;
            transmissionProbabilitySlider.value = instance.transmissionProbability;
            recoveryTimeSlider.value = instance.recoveryTimeInTimeSteps;
            infectiousOnStartSlider.value = instance.infectiousOnStart;
            timeStepsUntilResusceptibleSlider.value = instance.timeStepsUntilResusceptibility;
            timeStepIntervalUntilReinfectionSlider.value = instance.timeStepIntervalUntilExternalReinfection;
            extReinfectionSlider.value = Convert.ToInt32(instance.externalReinfection);
            resusceptibilitySlider.value = Convert.ToInt32(instance.resusceptibility);

            socialDistancingAmountSlider.value = instance.socialDistancingAmount;
            socialDistancingAbidingSlider.value = instance.socialDistancingAbiding;

            pointOfInterestSlider.value = Convert.ToInt32(instance.pointOfInterest);
            pointOfInterestTravelPossibilitySlider.value = instance.pointOfInterestTravelProbability;

            vaccinationSlider.value = Convert.ToInt32(instance.vaccination);
            vaccinationFractionSlider.value = instance.vaccinatedFraction;
            vaccinationDelaySlider.value = instance.timeStepsUntilVaccineIntroduction;

            quarantineSlider.value = Convert.ToInt32(instance.quarantine);
            quarantineDelaySlider.value = instance.timeStepsUntilQuarantine;

            departmentsSlider.value = Convert.ToInt32(instance.departments);
            departmentsTravelPossibilitySlider.value = instance.departmentsTravelProbability;
            cameraZoomSlider.value = Camera.main.fieldOfView;

            UpdateValueTexts();
        }

        private void UpdateValueTexts()
        {
            foreach (Slider slider in parameterSliders())
            {
                TextMeshProUGUI valueText = slider.transform.Find("Value").GetComponent<TextMeshProUGUI>();
                float roundedValue = Mathf.Round(slider.value * 100f) / 100f;
                valueText.text = roundedValue.ToString("0.00");
            }
        }

        private List<Slider> parameterSliders()
        {
            return settingsContainer.GetComponentsInChildren<Slider>(includeInactive: true).ToList();
        }

        public void UpdateSimulationParameter(float value, Action<float> parameterSetter)
        {
            parameterSetter(value);
            UpdateValueTexts();
        }

        public void TimeStepSlider() => UpdateSimulationParameter(timeStepSlider.value, v => SimulationController.instance.timeStepLengthInSeconds = v);

        public void TransmissionRadiusSlider() => UpdateSimulationParameter(transmissionRadiusSlider.value, v => SimulationController.instance.transmissionRadius = v);

        public void TransmissionProbabilitySlider() => UpdateSimulationParameter(transmissionProbabilitySlider.value, v => SimulationController.instance.transmissionProbability = v);

        public void RecoveryTimeSlider() => UpdateSimulationParameter(recoveryTimeSlider.value, v => SimulationController.instance.recoveryTimeInTimeSteps = (int)v);

        public void GroundBrightness() => UpdateSimulationParameter(groundBrightnessSlider.value, v => SimulationController.instance.ChangeGroundBrightness(!Convert.ToBoolean(v)));

        public void PopulationSizeSlider() => UpdateSimulationParameter(populationSizeSlider.value, v => SimulationController.instance.populationSize = (int)v);

        public void InfectiousOnStartSlider() => UpdateSimulationParameter(infectiousOnStartSlider.value, v => SimulationController.instance.infectiousOnStart = v);

        public void SocialDistancingAbiding() => UpdateSimulationParameter(socialDistancingAbidingSlider.value, v => SimulationController.instance.socialDistancingAbiding = v);

        public void PointOfInterestProbability() => UpdateSimulationParameter(pointOfInterestTravelPossibilitySlider.value, v => SimulationController.instance.pointOfInterestTravelProbability = v);

        public void Vaccination() => UpdateSimulationParameter(vaccinationSlider.value, v => SimulationController.instance.vaccination = Convert.ToBoolean(v));

        public void VaccinationFraction() => UpdateSimulationParameter(vaccinationFractionSlider.value, v => SimulationController.instance.vaccinatedFraction = v);

        public void VaccinationIntroductionDelay() => UpdateSimulationParameter(vaccinationDelaySlider.value, v => SimulationController.instance.timeStepsUntilVaccineIntroduction = (int)v);

        public void DepartmentsProbability() => UpdateSimulationParameter(departmentsTravelPossibilitySlider.value, v => SimulationController.instance.departmentsTravelProbability = v);

        public void CameraZoom() => UpdateSimulationParameter(cameraZoomSlider.value, v => Camera.main.fieldOfView = v);

        public void TimeStepsUntilResusceptible() => UpdateSimulationParameter(timeStepsUntilResusceptibleSlider.value, v => SimulationController.instance.timeStepsUntilResusceptibility = (int)v);

        public void TimeStepsUntilExtReinfection() => UpdateSimulationParameter(timeStepIntervalUntilReinfectionSlider.value, v => SimulationController.instance.timeStepIntervalUntilExternalReinfection = (int)v);

        public void ExtReinfection() => UpdateSimulationParameter(extReinfectionSlider.value, v => SimulationController.instance.externalReinfection = Convert.ToBoolean(v));

        public void Resusceptibility() => UpdateSimulationParameter(resusceptibilitySlider.value, v => SimulationController.instance.resusceptibility = Convert.ToBoolean(v));

        public void QuarantineDelay() => UpdateSimulationParameter(quarantineDelaySlider.value, v => SimulationController.instance.timeStepsUntilQuarantine = (int)v);

        public void SocialDistancingAmount()
        {
            UpdateSimulationParameter(socialDistancingAmountSlider.value, v => SimulationController.instance.socialDistancingAmount = v);
            SimulationController.instance.UpdateSocialDistancing();
        }

        public void PointOfInterest()
        {
            UpdateSimulationParameter(pointOfInterestSlider.value, v => SimulationController.instance.pointOfInterest = Convert.ToBoolean(v));
            departmentsSlider.interactable = !Convert.ToBoolean(pointOfInterestSlider.value);
            SimulationController.instance.TogglePointOfInterest(Convert.ToBoolean(pointOfInterestSlider.value));
        }

        public void Quarantine()
        {
            UpdateSimulationParameter(quarantineSlider.value, v => SimulationController.instance.quarantine = Convert.ToBoolean(v));
            SimulationController.instance.ToggleQuarantine(Convert.ToBoolean(quarantineSlider.value));
        }

        public void Departments()
        {
            UpdateSimulationParameter(departmentsSlider.value, v => SimulationController.instance.departments = Convert.ToBoolean(v));
            pointOfInterestSlider.interactable = !Convert.ToBoolean(departmentsSlider.value);
            SimulationController.instance.ToggleDepartments(Convert.ToBoolean(departmentsSlider.value));
        }

        public void UpdateGraph(int susceptibles, int infectiouses, int recovereds, int vaccinateds)
        {
            graphContainer.valueList.Add(infectiouses);
            graphContainer.valueListS.Add(susceptibles);
            graphContainer.valueListR.Add(recovereds);
            graphContainer.valueListV.Add(vaccinateds);

            if (SimulationController.instance.timeStepsPassed < 11)
                graphContainer.ShowGraph(graphContainer.valueList, -1, (int _i) => "Day " + (_i + 1), (float _f) => Mathf.RoundToInt(_f).ToString());
            else if (SimulationController.instance.timeStepsPassed > 30)
                graphContainer.ShowGraph(graphContainer.valueList, -1, (int _i) => "", (float _f) => Mathf.RoundToInt(_f).ToString());
            else
                graphContainer.ShowGraph(graphContainer.valueList, -1, (int _i) => (_i + 1).ToString(), (float _f) => Mathf.RoundToInt(_f).ToString());
        }

        public void ClearGraph()
        {
            graphContainer.valueList.Clear();
            graphContainer.valueListR.Clear();
            graphContainer.valueListS.Clear();
            graphContainer.valueListV.Clear();

            survivalPointContainer.SetActive(false);
            recoveryPointContainer.SetActive(false);
            breakingPointContainer.SetActive(false);
        }

        private void UIButton(string identifier, Animator animator, Image icon, Sprite onSprite, Sprite offSprite, ref bool toggleState)
        {
            string animationName = toggleState ? "close" + identifier : "open" + identifier;
            icon.sprite = toggleState ? onSprite : offSprite;
            animator.Play(animationName);
            toggleState = !toggleState;
        }

        public void GraphButton() => UIButton("Graph", graphContainerAnim, graphIcon, graphSprite, graphArrow, ref graphOn);

        public void SettingsButton() => UIButton("Settings", settingsContainerAnim, settingsIcon, settingsSprite, settingsArrow, ref settingsOn);

        public void ResetBtn() => SimulationController.instance.InitializeSimulation();

        public void RestartBtn() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}