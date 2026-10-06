using System.Collections.Generic;
using UnityEngine;

namespace TetiCorre
{
    // Gera a pista infinita.
    //
    // Versão antiga: criava um segmento a cada 3 segundos e nunca apagava nenhum
    // (a memória só crescia, e quando a velocidade aumentava o jogador passava da pista).
    //
    // Versão nova:
    //  1. Gera pela DISTÂNCIA do jogador, não pelo tempo: sempre há N segmentos na frente.
    //  2. Usa OBJECT POOLING: os segmentos que ficam pra trás são reciclados e reaparecem na frente.
    //  3. Coloca obstáculos e moedas em "linhas", garantindo que SEMPRE exista uma faixa livre.
    public class Generator : MonoBehaviour
    {
        [SerializeField] private ConfiguracaoDoJogo config;
        [SerializeField] private Transform jogador;

        [Header("Prefabs")]
        [SerializeField] private Segmento[] prefabsSegmento;
        [Tooltip("Obstáculos baixos (dá pra pular).")]
        [SerializeField] private Obstaculo[] prefabsBarreira;
        [Tooltip("Obstáculos altos (só desviando).")]
        [SerializeField] private Obstaculo[] prefabsVagao;
        [SerializeField] private Moeda prefabMoeda;
        [SerializeField] private Ima prefabIma;

        [Header("Moedas")]
        [Tooltip("Altura das moedas (mais ou menos na altura do peito da Teti).")]
        [SerializeField] private float alturaMoeda = 0.9f;
        [SerializeField] private int moedasPorFileira = 5;
        [SerializeField] private float espacoEntreMoedas = 2.5f;

        // Cada setor de 50 m tem três pontos de distribuição, separados por 15 m.
        // O orçamento abaixo permite no máximo três veículos no setor inteiro.
        private static readonly float[] posicoesDasLinhas = { 0.2f, 0.5f, 0.8f };

        private ObjectPool<Segmento>[] poolsSegmento;
        private ObjectPool<ItemDaPista>[] poolsBarreira;
        private ObjectPool<ItemDaPista>[] poolsVagao;
        private ObjectPool<ItemDaPista> poolMoeda;
        private ObjectPool<ItemDaPista> poolIma;

        // Fila: o primeiro é o mais antigo (mais atrás), o último é o mais à frente.
        private readonly Queue<Segmento> segmentosAtivos = new Queue<Segmento>();

        private float proximoZ;
        private int segmentosGerados;
        private int faixaLivre;

        private void Awake()
        {
            int segmentosTotais = config.segmentosNaFrente + config.segmentosAtras + 2;

            // Pais organizados na Hierarchy (só pra ficar arrumado na cena).
            Transform paiSegmentos = CriarPai("Segmentos");
            Transform paiItens = CriarPai("Itens");

            poolsSegmento = new ObjectPool<Segmento>[prefabsSegmento.Length];
            for (int i = 0; i < prefabsSegmento.Length; i++)
            {
                poolsSegmento[i] = new ObjectPool<Segmento>(prefabsSegmento[i], paiSegmentos,
                    Mathf.CeilToInt((float)segmentosTotais / prefabsSegmento.Length));
            }

            poolsBarreira = CriarPools(prefabsBarreira, paiItens, 6);
            poolsVagao = CriarPools(prefabsVagao, paiItens, 6);
            poolMoeda = new ObjectPool<ItemDaPista>(prefabMoeda, paiItens, 80);
            poolIma = new ObjectPool<ItemDaPista>(prefabIma, paiItens, 2);

            ValidarComprimentoDosVagoes();
        }

        private Transform CriarPai(string nome)
        {
            Transform pai = new GameObject(nome).transform;
            pai.SetParent(transform, false);
            return pai;
        }

        private static ObjectPool<ItemDaPista>[] CriarPools(Obstaculo[] prefabs, Transform pai, int quantidadeTotal)
        {
            var pools = new ObjectPool<ItemDaPista>[prefabs.Length];
            for (int i = 0; i < prefabs.Length; i++)
            {
                pools[i] = new ObjectPool<ItemDaPista>(prefabs[i], pai, Mathf.CeilToInt((float)quantidadeTotal / prefabs.Length));
            }
            return pools;
        }

        // Se um vagão for comprido demais, ele pode "fechar" o caminho entre uma linha e outra.
        private void ValidarComprimentoDosVagoes()
        {
            float espacoEntreLinhas = config.comprimentoSegmento * 0.5f;
            foreach (Obstaculo vagao in prefabsVagao)
            {
                if (vagao.Comprimento > espacoEntreLinhas - 6f)
                {
                    Debug.LogWarning($"[Generator] O vagão '{vagao.name}' tem {vagao.Comprimento} m e pode deixar pouco espaço pra desviar.");
                }
            }
        }

        private void Update()
        {
            float zJogador = jogador.position.z;
            float comprimento = config.comprimentoSegmento;

            // Gera na frente até ter 'segmentosNaFrente' prontos.
            while (proximoZ < zJogador + comprimento * config.segmentosNaFrente)
            {
                GerarSegmento();
            }

            // Recicla os que ficaram pra trás (com uma folga pra câmera não ver sumindo).
            float limiteAtras = zJogador - comprimento * config.segmentosAtras;
            while (segmentosAtivos.Count > 0 && segmentosAtivos.Peek().InicioZ + comprimento < limiteAtras)
            {
                segmentosAtivos.Dequeue().Reciclar();
            }
        }

        // Volta a pista pro estado inicial (usado ao voltar pro menu, sem recarregar a cena).
        public void Reiniciar()
        {
            while (segmentosAtivos.Count > 0)
            {
                segmentosAtivos.Dequeue().Reciclar();
            }

            proximoZ = jogador.position.z - config.comprimentoSegmento * config.segmentosAtras;
            segmentosGerados = 0;
            faixaLivre = 0;

            // Já deixa a pista pronta (inclusive os segmentos de trás, que aparecem de fundo no menu).
            Update();
        }

        private void GerarSegmento()
        {
            int variacao = Random.Range(0, poolsSegmento.Length);
            ObjectPool<Segmento> pool = poolsSegmento[variacao];

            Segmento segmento = pool.Pegar(new Vector3(0f, 0f, proximoZ), Quaternion.identity);
            segmento.Preparar(pool);
            segmentosAtivos.Enqueue(segmento);

            // Os segmentos atrás do início + os primeiros na frente ficam vazios.
            bool podeTerObstaculos = segmentosGerados >= config.segmentosAtras + config.segmentosSemObstaculo;
            int obstaculosRestantes = 3;
            for (int i = 0; i < posicoesDasLinhas.Length; i++)
            {
                GerarLinha(segmento, proximoZ + config.comprimentoSegmento * posicoesDasLinhas[i],
                    podeTerObstaculos, ref obstaculosRestantes);
            }

            proximoZ += config.comprimentoSegmento;
            segmentosGerados++;
        }

        // Uma "linha" = uma posição Z onde cada uma das 3 faixas pode ter obstáculo, moedas ou nada.
        private void GerarLinha(Segmento segmento, float z, bool podeTerObstaculos, ref int obstaculosRestantes)
        {
            float dificuldade = Mathf.Clamp01(z / config.distanciaDificuldadeMaxima);
            float chanceObstaculo = Mathf.Lerp(config.chanceObstaculoInicial, config.chanceObstaculoMaxima, dificuldade);
            float chanceVagao = Mathf.Lerp(config.chanceVagaoInicial, config.chanceVagaoMaxima, dificuldade);

            // REGRA DE OURO: a faixa livre só muda no máximo 1 faixa por linha.
            // Assim o jogador sempre consegue chegar nela a tempo, e nunca existe uma linha impossível.
            faixaLivre = podeTerObstaculos ? Mathf.Clamp(faixaLivre + Random.Range(-1, 2), -1, 1) : 0;

            for (int faixa = -1; faixa <= 1; faixa++)
            {
                float x = faixa * config.larguraFaixa;

                if (faixa == faixaLivre)
                {
                    // Esta faixa nunca recebe obstáculos: há sempre uma rota por desvio.
                    ColocarFileiraDeMoedas(segmento, x, z);
                    if (Random.value < config.chanceIma)
                    {
                        ColocarItem(segmento, poolIma, new Vector3(x, alturaMoeda, z));
                    }
                }
                else if (podeTerObstaculos && obstaculosRestantes > 0 && Random.value < chanceObstaculo)
                {
                    obstaculosRestantes--;
                    if (Random.value < chanceVagao)
                    {
                        ColocarObstaculo(segmento, poolsVagao, x, z);
                    }
                    else
                    {
                        ColocarObstaculo(segmento, poolsBarreira, x, z);
                        if (Random.value < 0.3f) ColocarMoedasEmArco(segmento, x, z);
                    }
                }
                else if (Random.value < config.chanceMoedasEmOutraFaixa)
                {
                    ColocarFileiraDeMoedas(segmento, x, z);
                }
            }
        }

        private void ColocarObstaculo(Segmento segmento, ObjectPool<ItemDaPista>[] pools, float x, float z)
        {
            ObjectPool<ItemDaPista> pool = pools[Random.Range(0, pools.Length)];
            ColocarItem(segmento, pool, new Vector3(x, 0f, z));
        }

        private void ColocarFileiraDeMoedas(Segmento segmento, float x, float zCentro)
        {
            float zInicio = zCentro - (moedasPorFileira - 1) * espacoEntreMoedas * 0.5f;
            for (int i = 0; i < moedasPorFileira; i++)
            {
                ColocarItem(segmento, poolMoeda, new Vector3(x, alturaMoeda, zInicio + i * espacoEntreMoedas));
            }
        }

        // Moedas em arco por cima de uma barreira, seguindo a mesma parábola do pulo.
        // Quem pula na hora certa pega todas.
        private void ColocarMoedasEmArco(Segmento segmento, float x, float zBarreira)
        {
            // Estimativa da velocidade quando o jogador chegar aqui.
            float velocidade = Mathf.Min(EstimarVelocidadeEm(zBarreira), config.velocidadeMaxima);
            float distanciaDoPulo = velocidade * config.TempoNoAr;

            for (int i = 0; i < moedasPorFileira; i++)
            {
                float t = i / (float)(moedasPorFileira - 1);           // 0 → 1 ao longo do pulo
                float altura = config.alturaPulo * 4f * t * (1f - t);   // parábola: 0 → altura máx → 0
                float z = zBarreira + (t - 0.5f) * distanciaDoPulo;
                ColocarItem(segmento, poolMoeda, new Vector3(x, alturaMoeda + altura, z));
            }
        }

        private float EstimarVelocidadeEm(float z)
        {
            GameManager jogo = GameManager.Instancia;
            float velocidadeAtual = jogo != null && jogo.Velocidade > 0f ? jogo.Velocidade : config.velocidadeInicial;
            float tempoAteLa = Mathf.Max(0f, z - jogador.position.z) / velocidadeAtual;
            return velocidadeAtual + config.aceleracao * tempoAteLa;
        }

        private static void ColocarItem(Segmento segmento, ObjectPool<ItemDaPista> pool, Vector3 posicao)
        {
            ItemDaPista item = pool.Pegar(posicao, Quaternion.identity);
            item.EntrarNaPista(pool);
            segmento.AdicionarItem(item);
        }
    }
}
