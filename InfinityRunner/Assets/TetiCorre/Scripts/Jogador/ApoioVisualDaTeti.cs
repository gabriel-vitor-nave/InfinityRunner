using System.Collections.Generic;
using UnityEngine;

namespace TetiCorre
{
    // O pivot e os bounds do FBX não representam a sola dos pés na animação.
    // Apoia a malha da pose atual; o salto continua no objeto Jogador.
    [ExecuteAlways]
    public sealed class ApoioVisualDaTeti : MonoBehaviour
    {
        [System.Serializable]
        private struct Influencia
        {
            public int indice;
            public Vector3 ponto;
            public float peso;
            public Vector3 Mundo(Matrix4x4[] matrizes) => peso > 0f ? matrizes[indice].MultiplyPoint3x4(ponto) * peso : Vector3.zero;
        }
        [System.Serializable]
        private struct PontoDaSola
        {
            public Influencia a, b, c, d;
            public Vector3 Mundo(Matrix4x4[] matrizes) => a.Mundo(matrizes) + b.Mundo(matrizes) + c.Mundo(matrizes) + d.Mundo(matrizes);
        }
        [SerializeField, HideInInspector] private PontoDaSola[] pontosDaSola;
        [SerializeField, HideInInspector] private Transform[] ossosDeApoio;
        private Matrix4x4[] matrizesDosOssos;
        public int QuantidadePontosDeApoio => pontosDaSola != null ? pontosDaSola.Length : 0;
        [SerializeField] private Transform modelo;
        [SerializeField] private Animator animator;
        [SerializeField] private AnimationClip danca;
        private Mesh malha;
        private readonly List<Vector3> vertices = new List<Vector3>();
        private SkinnedMeshRenderer[] renderers;

        // Executado pela montagem no Editor. Salva só os vértices dos pés e seus
        // vínculos aos ossos; a corrida não faz BakeMesh nem percorre o corpo inteiro.
        public void PrepararPontos()
        {
            var pontos = new List<PontoDaSola>();
            var listaOssos = new List<Transform>();
            foreach (var pele in modelo.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!pele.enabled) continue;
                var mesh = pele.sharedMesh;
                var posicoes = mesh.vertices;
                var pesos = mesh.boneWeights;
                var poses = mesh.bindposes;
                var ossos = pele.bones;
                for (int i = 0; i < posicoes.Length; i++)
                {
                    var peso = pesos[i];
                    if (!DoPe(ossos, peso.boneIndex0, peso.weight0) && !DoPe(ossos, peso.boneIndex1, peso.weight1)
                        && !DoPe(ossos, peso.boneIndex2, peso.weight2) && !DoPe(ossos, peso.boneIndex3, peso.weight3)) continue;
                    pontos.Add(new PontoDaSola {
                        a = Criar(listaOssos, ossos, poses, posicoes[i], peso.boneIndex0, peso.weight0),
                        b = Criar(listaOssos, ossos, poses, posicoes[i], peso.boneIndex1, peso.weight1),
                        c = Criar(listaOssos, ossos, poses, posicoes[i], peso.boneIndex2, peso.weight2),
                        d = Criar(listaOssos, ossos, poses, posicoes[i], peso.boneIndex3, peso.weight3) });
                }
                pele.updateWhenOffscreen = true;
            }
            pontosDaSola = pontos.ToArray();
            ossosDeApoio = listaOssos.ToArray();
        }

        private static bool DoPe(Transform[] ossos, int indice, float peso)
        {
            if (peso < .05f || indice >= ossos.Length || ossos[indice] == null) return false;
            string nome = ossos[indice].name;
            return nome.Contains("Foot") || nome.Contains("Toe");
        }
        private static Influencia Criar(List<Transform> lista, Transform[] ossos, Matrix4x4[] poses, Vector3 ponto, int indice, float peso)
        {
            if (peso <= 0f || indice >= ossos.Length) return default;
            int destino = lista.IndexOf(ossos[indice]);
            if (destino < 0) { destino = lista.Count; lista.Add(ossos[indice]); }
            return new Influencia { indice = destino, ponto = poses[indice].MultiplyPoint3x4(ponto), peso = peso };
        }

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
            if (pontosDaSola != null && pontosDaSola.Length > 0)
            {
                float sola = float.PositiveInfinity;
                if (matrizesDosOssos == null || matrizesDosOssos.Length != ossosDeApoio.Length)
                    matrizesDosOssos = new Matrix4x4[ossosDeApoio.Length];
                for (int i = 0; i < ossosDeApoio.Length; i++) matrizesDosOssos[i] = ossosDeApoio[i].localToWorldMatrix;
                for (int i = 0; i < pontosDaSola.Length; i++) sola = Mathf.Min(sola, pontosDaSola[i].Mundo(matrizesDosOssos).y);
                modelo.position += Vector3.up * (transform.position.y + .005f - sola);
                return;
            }
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
