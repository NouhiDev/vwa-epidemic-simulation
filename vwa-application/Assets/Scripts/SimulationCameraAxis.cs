using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace nouhidev
{
    public class SimulationCameraAxis : MonoBehaviour
    {
        [SerializeField] private Vector3 rotationVector;
        void Update()
        {
            transform.Rotate(rotationVector * Time.deltaTime);
        }
    }
}
