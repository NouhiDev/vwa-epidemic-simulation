using UnityEngine;

namespace nouhidev
{
    // Handles the visual transmission pulse
    public class TransmissionPulse : MonoBehaviour
    {
        [SerializeField] private float speed = 2;
        private float radius;
        private float timeStep;

        private void Start()
        {
            radius = SimulationController.instance.transmissionRadius;
            timeStep = SimulationController.instance.timeStepLengthInSeconds;

            Destroy(gameObject, timeStep);
        }

        private void Update()
        {
            Vector3 targetScale = new Vector3(radius, radius, radius);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);
        }
    }
}
