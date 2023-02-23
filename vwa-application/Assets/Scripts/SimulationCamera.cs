using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace nouhidev
{
    public class SimulationCamera : MonoBehaviour
    {
        [SerializeField] private Transform lookAt;

        // Update is called once per frame
        void Update()
        {
            if (lookAt != null) transform.LookAt(lookAt);
        }
    }
}
