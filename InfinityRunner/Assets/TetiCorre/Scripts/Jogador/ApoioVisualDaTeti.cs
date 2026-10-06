using System.Collections.Generic;
using UnityEngine;

namespace TetiCorre
{
    // O pivot e os bounds do FBX não representam a sola dos pés na animação.
    // Apoia a malha da pose atual; o salto continua no objeto Jogador.
    [ExecuteAlways]
    public sealed class ApoioVisualDaTeti : MonoBehaviour
    {
        [SerializeField] private Transform modelo;
        [SerializeField] private Animator animator;
        [SerializeField] private AnimationClip danca;
        private Mesh malha;
        private readonly List<Vector3> vertices = new List<Vector3>();
        private SkinnedMeshRenderer[] renderers;

        private void OnEnable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += AtualizarEditor;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= AtualizarEditor;
#endif
            if (malha != null) { if (Application.isPlaying) Destroy(malha); else DestroyImmediate(malha); }
            malha = null;
        }

        private void LateUpdate()
        {
            if (Application.isPlaying) Apoiar();
        }

        public void AtualizarPreview(float tempo)
        {
            if (modelo == null || animator == null || danca == null) return;
            danca.SampleAnimation(animator.gameObject, tempo % danca.length);
            Apoiar();
        }

#if UNITY_EDITOR
        private double proximoFrame;
        private void AtualizarEditor()
        {
            if (Application.isPlaying || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode
                || this == null || !gameObject.scene.IsValid() || !isActiveAndEnabled) return;
            double agora = UnityEditor.EditorApplication.timeSinceStartup;
            if (agora < proximoFrame) return;
            proximoFrame = agora + 1d / 30d;
            AtualizarPreview((float)agora);
            UnityEditor.SceneView.RepaintAll();
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        }
#endif

        private void Apoiar()
        {
            if (modelo == null) return;
            if (renderers == null) renderers = modelo.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (malha == null) malha = new Mesh { name = "Pose da Teti (temporária)", hideFlags = HideFlags.HideAndDontSave };
            float minimo = float.PositiveInfinity;
            foreach (var renderer in renderers)
            {
                if (!renderer.enabled) continue;
                renderer.updateWhenOffscreen = true;
                renderer.BakeMesh(malha, false);
                malha.GetVertices(vertices);
                foreach (var vertice in vertices)
                    // BakeMesh(false) já inclui a escala dos ossos. O renderer do
                    // prefab tem escala própria (~45), que não deve ser aplicada de novo.
                    minimo = Mathf.Min(minimo, (renderer.transform.position + renderer.transform.rotation * vertice).y);
            }
            if (!float.IsInfinity(minimo))
                modelo.position += Vector3.up * (transform.position.y + .005f - minimo);
        }
    }
}
