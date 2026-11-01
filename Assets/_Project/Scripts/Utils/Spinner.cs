using UnityEngine;

namespace EcoGame.Utils
{
    /// <summary>Spins this transform around a local axis while active (turbine sails, mill wheel).</summary>
    public class Spinner : MonoBehaviour
    {
        [SerializeField] private Vector3 localAxis = Vector3.forward;
        [SerializeField] private float degreesPerSecond = 60f;

        private void Update() => transform.Rotate(localAxis, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
