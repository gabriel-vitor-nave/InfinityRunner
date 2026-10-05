using UnityEngine;

namespace TetiCorre
{
    // Partículas do jogo. Em vez de criar um efeito novo a cada moeda,
    // usamos UM sistema de partículas só e mandamos ele "emitir" no ponto certo.
    // (ParticleSystem.Emit não cria objetos novos: é bem leve.)
    public class EfeitosVisuais : MonoBehaviour
    {
        [SerializeField] private ParticleSystem brilhoMoeda;
        [SerializeField] private ParticleSystem brilhoIma;
        [SerializeField] private ParticleSystem impactoBatida;

        [Tooltip("Partículas presas na câmera que dão sensação de velocidade.")]
        [SerializeField] private ParticleSystem linhasDeVelocidade;

        public void Moeda(Vector3 posicao) => Emitir(brilhoMoeda, posicao, 10);
        public void Ima(Vector3 posicao) => Emitir(brilhoIma, posicao, 30);
        public void Batida(Vector3 posicao) => Emitir(impactoBatida, posicao, 40);

        // 0 = parado, 1 = velocidade máxima
        public void AtualizarVelocidade(float intensidade)
        {
            if (linhasDeVelocidade == null) return;

            ParticleSystem.EmissionModule emissao = linhasDeVelocidade.emission;
            emissao.rateOverTimeMultiplier = Mathf.Lerp(0f, 60f, intensidade * intensidade);
        }

        private static void Emitir(ParticleSystem sistema, Vector3 posicao, int quantidade)
        {
            if (sistema == null) return;

            var parametros = new ParticleSystem.EmitParams
            {
                position = posicao,
                applyShapeToPosition = true
            };
            sistema.Emit(parametros, quantidade);
        }
    }
}
