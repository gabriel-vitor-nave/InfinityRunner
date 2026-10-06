using UnityEngine;
namespace TetiCorre
{
    [RequireComponent(typeof(Light))]
    public sealed class LuzDoPoste : MonoBehaviour
    {
        private Light luz;
        private void Awake() { luz = GetComponent<Light>(); luz.enabled = false; }
        private void OnEnable() { if (luz == null) luz = GetComponent<Light>(); AmbienteDaCorrida.RegistrarPoste(luz); }
        private void OnDisable() { AmbienteDaCorrida.RemoverPoste(luz); }
    }
}
