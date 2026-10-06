using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TetiCorre
{
    public sealed class AmbienteDaCorrida : MonoBehaviour
    {
        [SerializeField] private Transform jogador;
        [SerializeField] private ConfiguracaoDoJogo config;
        [SerializeField] private Light sol;
        [SerializeField] private Material ceu;
        [SerializeField] private Material ceuNoite;
        [SerializeField] private Material luzDosPostes;
        private static readonly List<VisibilidadePorDistancia> objetos = new List<VisibilidadePorDistancia>();
        private static readonly List<Light> postes = new List<Light>();
        private float proximaAtualizacao;
        private float tempoCiclo;
        private float duracao;
        private bool noite;
        public float FracaoNoite { get; private set; }
        public bool Noite => noite;
        public float DuracaoDoPeriodo => duracao;
        public static void Registrar(VisibilidadePorDistancia objeto) { if (!objetos.Contains(objeto)) objetos.Add(objeto); }
        public static void Remover(VisibilidadePorDistancia objeto) { objetos.Remove(objeto); }
        public static void RegistrarPoste(Light luz) { if (!postes.Contains(luz)) postes.Add(luz); }
        public static void RemoverPoste(Light luz) { postes.Remove(luz); }

        private void Awake()
        {
            duracao = Mathf.Max(1f, config.duracaoInicialPeriodo);
            if (ceu != null) { ceu = new Material(ceu); RenderSettings.skybox = ceu; }
            if (ceuNoite != null) ceuNoite = new Material(ceuNoite);
            QualitySettings.shadowDistance = config.distanciaSombras;
            QualitySettings.shadowCascades = 0;
            QualitySettings.shadows = ShadowQuality.HardOnly;
            QualitySettings.shadowResolution = ShadowResolution.Low;
            QualitySettings.pixelLightCount = 2;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Shader.SetGlobalFloat("_TetiInicioCurva", config.inicioCurvatura);
            Shader.SetGlobalFloat("_TetiForcaCurva", config.forcaCurvatura);
            RenderSettings.ambientMode = AmbientMode.Flat;
        }

        private void LateUpdate()
        {
            Shader.SetGlobalFloat("_TetiZ", jogador.position.z);
            var jogo = GameManager.Instancia;
            if (jogo != null && jogo.Estado == EstadoDoJogo.Jogando) AvancarCiclo(Time.deltaTime);
            if (Time.unscaledTime < proximaAtualizacao) return;
            proximaAtualizacao = Time.unscaledTime + .2f;
            for (int i = objetos.Count - 1; i >= 0; i--)
            {
                if (objetos[i] == null) objetos.RemoveAt(i);
                else objetos[i].Atualizar(jogador.position.z, config);
            }
            AplicarLuz();
        }

        public void AvancarCiclo(float segundos)
        {
            tempoCiclo += segundos;
            while (tempoCiclo >= duracao)
            {
                tempoCiclo -= duracao;
                if (noite) duracao *= 2f; // Dobra só após completar dia + noite.
                noite = !noite;
            }
            float transicao = Mathf.Max(.01f, Mathf.Min(config.duracaoTransicaoLuz, duracao * .25f));
            float t = Mathf.SmoothStep(0f, 1f, tempoCiclo / transicao);
            // O primeiro dia já começa claro. As demais trocas fazem amanhecer/anoitecer.
            FracaoNoite = noite ? t : duracao <= config.duracaoInicialPeriodo ? 0f : 1f - t;
        }

        private void AplicarLuz()
        {
            float n = FracaoNoite;
            sol.color = Color.Lerp(new Color(1f, .91f, .78f), new Color(.45f, .57f, .95f), n);
            sol.intensity = Mathf.Lerp(1.05f, .16f, n);
            RenderSettings.ambientLight = Color.Lerp(new Color(.57f, .64f, .75f), new Color(.19f, .23f, .36f), n);
            RenderSettings.fogColor = Color.Lerp(new Color(.63f, .78f, .9f), new Color(.035f, .055f, .13f), n);
            if (ceu != null)
            {
                bool usarNoite = n >= .5f && ceuNoite != null;
                var materialCeu = usarNoite ? ceuNoite : ceu;
                RenderSettings.skybox = materialCeu;
                materialCeu.SetFloat("_Exposure", usarNoite
                    ? Mathf.Lerp(.06f, .9f, (n - .5f) * 2f)
                    : Mathf.Lerp(1f, .06f, n * 2f));
            }
            Shader.SetGlobalFloat("_TetiNoite", n);
            // Só postes perto do jogador iluminam objetos. Os demais mostram a lâmpada emissiva.
            int ligados = 0;
            for (int i = 0; i < postes.Count; i++)
            {
                var poste = postes[i];
                if (poste == null) continue;
                bool ligar = n > .15f && Mathf.Abs(poste.transform.position.z - jogador.position.z) < 22f && ligados < 6;
                poste.enabled = ligar;
                poste.intensity = n * 1.4f;
                if (ligar) ligados++;
            }
        }
        private void OnDestroy() { if (ceu != null) Destroy(ceu); if (ceuNoite != null) Destroy(ceuNoite); }
    }
}
