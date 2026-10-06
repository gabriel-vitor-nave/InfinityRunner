using UnityEngine;

namespace TetiCorre
{
    // Todos os números que controlam o "feeling" do jogo ficam aqui, num asset (ScriptableObject).
    // Assim dá pra ajustar velocidade, pulo, dificuldade etc. pelo Inspector, sem mexer no código.
    // O asset fica em Assets/TetiCorre/Configuracoes/ConfiguracaoDoJogo.asset
    [CreateAssetMenu(fileName = "ConfiguracaoDoJogo", menuName = "Teti Corre/Configuração do Jogo")]
    public class ConfiguracaoDoJogo : ScriptableObject
    {
        [Header("Faixas")]
        [Tooltip("Distância (em metros) entre o centro de uma faixa e a outra.")]
        public float larguraFaixa = 3.5f;
        [Tooltip("Velocidade lateral (m/s) ao trocar de faixa.")]
        public float velocidadeTrocaFaixa = 16f;

        [Header("Corrida")]
        public float velocidadeInicial = 10f;
        public float velocidadeMaxima = 24f;
        [Tooltip("Quanto a velocidade aumenta por segundo (m/s²).")]
        public float aceleracao = 0.18f;

        [Header("Pulo")]
        [Tooltip("Altura máxima do pulo, em metros.")]
        public float alturaPulo = 2.2f;
        [Tooltip("Gravidade usada no pulo (negativa = pra baixo).")]
        public float gravidade = -40f;
        [Tooltip("Gravidade quando aperta pra baixo no ar (cai mais rápido).")]
        public float gravidadeDescidaRapida = -140f;

        [Header("Pista")]
        [Tooltip("Comprimento de cada pedaço (segmento) da pista. Tem que bater com o prefab do segmento.")]
        public float comprimentoSegmento = 50f;
        [Tooltip("Quantos segmentos ficam prontos na frente do jogador.")]
        public int segmentosNaFrente = 6;
        [Tooltip("Quantos segmentos ficam atrás (aparecem de fundo no menu).")]
        public int segmentosAtras = 2;
        [Tooltip("Primeiros segmentos sem obstáculo, pra dar tempo de se acostumar.")]
        public int segmentosSemObstaculo = 2;

        [Header("Dificuldade (0 = começo, 1 = máxima)")]
        [Tooltip("Distância (m) em que a dificuldade chega no máximo.")]
        public float distanciaDificuldadeMaxima = 1500f;
        [Range(0f, 1f)] public float chanceObstaculoInicial = 0.35f;
        [Range(0f, 1f)] public float chanceObstaculoMaxima = 0.8f;
        [Tooltip("Chance de um obstáculo ser vagão (só dá pra desviar) em vez de barreira (dá pra pular).")]
        [Range(0f, 1f)] public float chanceVagaoInicial = 0.3f;
        [Range(0f, 1f)] public float chanceVagaoMaxima = 0.6f;
        [Tooltip("Chance de aparecer uma barreira na faixa livre (aí tem que pular).")]
        [Range(0f, 1f)] public float chanceBarreiraNaFaixaLivre = 0.35f;

        [Header("Moedas (centavos de ponto)")]
        [Tooltip("Cada moeda vale quantos centavos de ponto na nota. 1 = 0,01 ponto.")]
        public int centavosPorMoeda = 1;
        [Range(0f, 1f)] public float chanceFileiraDeMoedas = 0.7f;
        [Range(0f, 1f)] public float chanceMoedasEmOutraFaixa = 0.25f;

        [Header("Ímã")]
        [Range(0f, 1f)] public float chanceIma = 0.06f;
        public float duracaoIma = 10f;
        public float raioIma = 8f;

        [Header("Morte")]
        [Tooltip("Segundos entre bater e voltar pro menu.")]
        public float tempoAteVoltarAoMenu = 2.5f;

        // Velocidade vertical inicial pra alcançar 'alturaPulo' com essa gravidade.
        // Vem da física: v² = 2·g·h  →  v = √(2·g·h)
        public float VelocidadeDoPulo => Mathf.Sqrt(2f * -gravidade * alturaPulo);

        // Tempo que o personagem fica no ar num pulo normal (sobe e desce).
        public float TempoNoAr => 2f * VelocidadeDoPulo / -gravidade;
    }
}
