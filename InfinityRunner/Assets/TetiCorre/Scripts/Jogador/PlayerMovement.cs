using UnityEngine;

namespace TetiCorre
{
    // Controla a Teti: corre sozinha pra frente, troca entre 3 faixas, pula e detecta colisões.
    //
    // Por que Rigidbody cinemático + collider "trigger"?
    //  - A pista é plana, então não precisamos da física "de verdade" (empurrões, atrito...).
    //    A posição é calculada por nós mesmos: X = faixa, Y = pulo, Z = corrida.
    //  - Mesmo assim, o Rigidbody cinemático faz o Unity avisar (OnTriggerEnter) quando a Teti
    //    encosta numa moeda, num ímã ou num obstáculo.
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private ConfiguracaoDoJogo config;
        [SerializeField] private AnimacaoDoJogador animacao;

        [Header("Swipe (celular)")]
        [Tooltip("Tamanho mínimo do arrasto, em fração da altura da tela.")]
        [SerializeField] private float tamanhoMinimoSwipe = 0.05f;

        [Header("Sensação de controle")]
        [Tooltip("Se apertar pular um pouquinho antes de encostar no chão, o pulo ainda conta (segundos).")]
        [SerializeField] private float toleranciaPulo = 0.15f;

        // -1 = esquerda, 0 = meio, 1 = direita
        private int faixaAtual;
        private float velocidadeVertical;
        private bool noChao = true;
        private bool descidaRapida;
        private bool controlavel;
        private float tempoDesdePedidoDePulo = float.MaxValue;
        private float tempoImaRestante;

        private bool arrastando;
        private Vector2 inicioArrasto;

        private CapsuleCollider capsula;
        private readonly RaycastHit[] impactos = new RaycastHit[32];

        public bool ImaAtivo => tempoImaRestante > 0f;
        public float FracaoImaRestante => Mathf.Clamp01(tempoImaRestante / config.duracaoIma);
        public Vector3 CentroDoCorpo => transform.position + capsula.center;

        private void Awake()
        {
            capsula = GetComponent<CapsuleCollider>();
            capsula.isTrigger = true;

            Rigidbody corpo = GetComponent<Rigidbody>();
            corpo.isKinematic = true;
            corpo.useGravity = false;
        }

        // ---------- Estados (chamados pelo GameManager) ----------

        public void PrepararParaMenu()
        {
            controlavel = false;
            faixaAtual = 0;
            velocidadeVertical = 0f;
            noChao = true;
            descidaRapida = false;
            tempoImaRestante = 0f;
            arrastando = false;
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            animacao.Dancar();
        }

        public void ComecarACorrer()
        {
            controlavel = true;
            tempoDesdePedidoDePulo = float.MaxValue;
            animacao.Correr();
        }

        public void Morrer()
        {
            controlavel = false;
            tempoImaRestante = 0f;
            animacao.Morrer();
        }

        public void AtivarIma()
        {
            tempoImaRestante = config.duracaoIma;
        }

        // ---------- Loop ----------

        private void Update()
        {
            float dt = Time.deltaTime;

            if (controlavel && GameManager.Instancia.Estado == EstadoDoJogo.Jogando)
            {
                LerTeclado();
                LerSwipe();
            }

            Vector3 posicao = transform.position;

            // 1) FRENTE: corre sozinha. A velocidade vem do GameManager (aumenta com o tempo, e é 0 no menu/morte).
            posicao.z += GameManager.Instancia.Velocidade * dt;

            // 2) LADO: vai suavemente até o centro da faixa escolhida.
            float xAnterior = posicao.x;
            if (controlavel)
            {
                float alvoX = faixaAtual * config.larguraFaixa;
                posicao.x = Mathf.MoveTowards(posicao.x, alvoX, config.velocidadeTrocaFaixa * dt);
            }

            // 3) CIMA/BAIXO: pulo com gravidade (continua até cair no chão, mesmo depois de morrer).
            if (!noChao)
            {
                float gravidade = descidaRapida ? config.gravidadeDescidaRapida : config.gravidade;
                velocidadeVertical += gravidade * dt;
                posicao.y += velocidadeVertical * dt;

                if (posicao.y <= 0f)
                {
                    Aterrissar(ref posicao);
                }
            }

            // Verifica o trajeto inteiro: em FPS baixo, só testar a posição final
            // permitiria atravessar uma moeda ou barreira entre dois frames.
            if (controlavel && GameManager.Instancia.Estado == EstadoDoJogo.Jogando)
                VerificarTrajeto(transform.position, posicao);
            transform.position = posicao;

            if (controlavel && dt > 0f)
            {
                animacao.Inclinar((posicao.x - xAnterior) / dt, config.velocidadeTrocaFaixa);
            }

            // Pulo "guardado": se apertou pouco antes de pousar, pula assim que pousar.
            tempoDesdePedidoDePulo += dt;
            if (controlavel && noChao && tempoDesdePedidoDePulo <= toleranciaPulo)
            {
                Pular();
            }

            if (tempoImaRestante > 0f) tempoImaRestante -= dt;
        }

        private void Aterrissar(ref Vector3 posicao)
        {
            posicao.y = 0f;
            velocidadeVertical = 0f;
            noChao = true;
            descidaRapida = false;
            if (controlavel) animacao.Aterrissar();
        }

        // ---------- Comandos ----------

        private void TrocarFaixa(int direcao)
        {
            int novaFaixa = Mathf.Clamp(faixaAtual + direcao, -1, 1);
            if (novaFaixa == faixaAtual) return;

            faixaAtual = novaFaixa;
            AudioManager.Instancia.TocarTrocaDeFaixa();
        }

        private void PedirPulo()
        {
            tempoDesdePedidoDePulo = 0f;
            if (noChao) Pular();
        }

        private void Pular()
        {
            tempoDesdePedidoDePulo = float.MaxValue;
            noChao = false;
            descidaRapida = false;
            velocidadeVertical = config.VelocidadeDoPulo;
            animacao.Pular();
            AudioManager.Instancia.TocarPulo();
        }

        private void Descer()
        {
            // No ar: cai mais rápido (útil pra voltar logo pro chão e desviar de novo).
            if (!noChao) descidaRapida = true;
        }

        // ---------- Entrada: teclado ----------

        private void LerTeclado()
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) TrocarFaixa(-1);
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) TrocarFaixa(1);
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space)) PedirPulo();
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) Descer();
        }

        // ---------- Entrada: swipe (celular) e arrastar com o mouse ----------

        private void LerSwipe()
        {
            // No celular usamos o toque. O Unity também "simula" mouse com o toque,
            // então quando tem toque na tela ignoramos o mouse pra não contar duas vezes.
            if (Input.touchCount > 0)
            {
                Touch toque = Input.GetTouch(0);
                if (toque.phase == TouchPhase.Began)
                {
                    arrastando = true;
                    inicioArrasto = toque.position;
                }
                else if (arrastando)
                {
                    VerificarSwipe(toque.position);
                }

                if (toque.phase == TouchPhase.Ended || toque.phase == TouchPhase.Canceled)
                {
                    arrastando = false;
                }
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                arrastando = true;
                inicioArrasto = Input.mousePosition;
            }
            else if (arrastando && Input.GetMouseButton(0))
            {
                VerificarSwipe(Input.mousePosition);
            }

            if (Input.GetMouseButtonUp(0)) arrastando = false;
        }

        private void VerificarSwipe(Vector2 posicaoAtual)
        {
            Vector2 arrasto = posicaoAtual - inicioArrasto;
            float minimo = Screen.height * tamanhoMinimoSwipe;
            if (arrasto.sqrMagnitude < minimo * minimo) return;

            arrastando = false; // um gesto = um comando

            if (Mathf.Abs(arrasto.x) > Mathf.Abs(arrasto.y))
            {
                TrocarFaixa(arrasto.x > 0f ? 1 : -1);
            }
            else if (arrasto.y > 0f)
            {
                PedirPulo();
            }
            else
            {
                Descer();
            }
        }

        // ---------- Colisões ----------

        private void VerificarTrajeto(Vector3 inicio, Vector3 fim)
        {
            Vector3 movimento = fim - inicio;
            float distancia = movimento.magnitude;
            if (distancia < .0001f) return;
            Vector3 centro = inicio + capsula.center;
            float metade = Mathf.Max(0, capsula.height * .5f - capsula.radius);
            int quantidade = Physics.CapsuleCastNonAlloc(centro + Vector3.up * metade,
                centro - Vector3.up * metade, capsula.radius, movimento / distancia,
                impactos, distancia, ~0, QueryTriggerInteraction.Collide);
            // Ordenação pequena, sem alocar: uma barreira deve bloquear as moedas atrás dela.
            for (int i = 0; i < quantidade; i++)
                for (int j = i + 1; j < quantidade; j++)
                    if (impactos[j].distance < impactos[i].distance)
                    { var troca = impactos[i]; impactos[i] = impactos[j]; impactos[j] = troca; }
            for (int i = 0; i < quantidade && controlavel; i++)
            {
                var collider = impactos[i].collider;
                if (collider != null && collider != capsula && collider.gameObject.activeInHierarchy)
                    OnTriggerEnter(collider);
            }
        }

        private void OnTriggerEnter(Collider outro)
        {
            if (!controlavel || GameManager.Instancia.Estado != EstadoDoJogo.Jogando) return;

            // Identificamos pelo COMPONENTE (e não por Tag), assim funciona em qualquer projeto
            // onde o .unitypackage for importado, mesmo sem as tags configuradas.
            if (outro.TryGetComponent(out Moeda moeda))
            {
                moeda.Coletar();
            }
            else if (outro.TryGetComponent(out Ima ima))
            {
                ima.Coletar();
            }
            else if (outro.GetComponentInParent<Obstaculo>() != null)
            {
                GameManager.Instancia.JogadorBateu();
            }
        }
    }
}
