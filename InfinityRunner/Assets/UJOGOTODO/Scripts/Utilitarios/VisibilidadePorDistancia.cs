using UnityEngine;
using UnityEngine.Rendering;

namespace TetiCorre
{
    // Detalhes próximos, silhueta simples ao longe e nenhum trabalho além da névoa.
    public sealed class VisibilidadePorDistancia : MonoBehaviour
    {
        [SerializeField] private Renderer[] detalhes;
        [SerializeField] private Renderer[] simplificados;
        [SerializeField] private bool itemDaPista;
        [SerializeField] private bool geraSombras = true;
        private Collider[] colliders;
        private Behaviour[] animacoes;
        private int nivel = -1;
        private bool sombras;
        public int NivelAtual => nivel;

        private void Awake()
        {
            if (detalhes == null || detalhes.Length == 0) detalhes = GetComponentsInChildren<Renderer>();
            colliders = itemDaPista ? GetComponentsInChildren<Collider>() : new Collider[0];
            var moeda = GetComponent<Moeda>();
            var giro = GetComponent<CollectableRotate>();
            animacoes = new Behaviour[] { moeda, giro };
        }
        private void OnEnable()
        {
            AmbienteDaCorrida.Registrar(this); nivel = -1;
            var jogo = GameManager.Instancia;
            if (jogo != null && jogo.Jogador != null) Atualizar(jogo.Jogador.transform.position.z, jogo.Config);
        }
        private void OnDisable() { AmbienteDaCorrida.Remover(this); }

        public void Atualizar(float zJogador, ConfiguracaoDoJogo config)
        {
            float z = transform.position.z - zJogador;
            float distancia = Mathf.Abs(z);
            float limite = itemDaPista ? config.distanciaItens : config.distanciaCenario;
            bool menu = GameManager.Instancia != null && GameManager.Instancia.Estado == EstadoDoJogo.Menu;
            bool foraDaVista = menu ? z > 18f : z < -18f;
            int novo = foraDaVista || distancia > limite ? 2 : itemDaPista || distancia <= config.distanciaDetalhes ? 0 : 1;
            bool mudou = novo != nivel;
            if (mudou)
            {
                nivel = novo;
                foreach (var r in detalhes) if (r != null) r.enabled = nivel == 0;
                if (simplificados != null) foreach (var r in simplificados) if (r != null) r.enabled = nivel == 1;
                foreach (var c in colliders) if (c != null) c.enabled = nivel == 0;
                foreach (var script in animacoes) if (script != null) script.enabled = nivel == 0;
            }
            bool novasSombras = geraSombras && nivel == 0 && distancia <= config.distanciaSombras;
            if (novasSombras == sombras && !mudou) return;
            sombras = novasSombras;
            foreach (var r in detalhes) if (r != null) r.shadowCastingMode = sombras ? ShadowCastingMode.On : ShadowCastingMode.Off;
            if (simplificados != null) foreach (var r in simplificados) if (r != null) r.shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}
