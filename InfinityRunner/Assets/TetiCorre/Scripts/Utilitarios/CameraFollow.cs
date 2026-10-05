using UnityEngine;

namespace TetiCorre
{
    // Câmera com dois "enquadramentos":
    //  - Menu: de frente pra Teti (ela dançando, com a pista de fundo).
    //  - Jogo: atrás e acima dela, olhando pra frente.
    // Ao apertar Play, a câmera dá a volta em órbita (pela direita) até ficar atrás dela.
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform alvo;

        [Header("Enquadramento do jogo (atrás)")]
        [SerializeField] private float distanciaJogo = 5.5f;
        [SerializeField] private float alturaJogo = 3.2f;
        [Tooltip("Ponto pra onde a câmera olha, relativo à Teti (um pouco à frente dela).")]
        [SerializeField] private Vector3 olharJogo = new Vector3(0f, 1.2f, 6f);

        [Header("Enquadramento do menu (de frente)")]
        [SerializeField] private float distanciaMenu = 3.6f;
        [SerializeField] private float alturaMenu = 1.4f;
        [Tooltip("Ângulo em volta da Teti: 0 = bem de frente. Um pouquinho de lado fica mais bonito.")]
        [SerializeField] private float anguloMenu = 18f;
        [SerializeField] private Vector3 olharMenu = new Vector3(0f, 1.05f, 0f);

        [Header("Movimento")]
        [SerializeField] private float duracaoTransicao = 1.3f;
        [Tooltip("Quanto do movimento lateral da Teti a câmera acompanha (menos de 1 dá sensação de profundidade).")]
        [Range(0f, 1f)] [SerializeField] private float seguirLateral = 0.75f;
        [Range(0f, 1f)] [SerializeField] private float seguirPulo = 0.35f;
        [SerializeField] private float suavidade = 8f;

        private float progresso;      // 0 = menu, 1 = jogo
        private float progressoAlvo;
        private float xSuave;
        private float ySuave;

        private float tremorForca;
        private float tremorDuracao;
        private float tremorRestante;

        public void IrParaMenu()
        {
            progresso = progressoAlvo = 0f;
            xSuave = ySuave = 0f;
            tremorRestante = 0f;
            Atualizar(0f);
        }

        public void IrParaJogo()
        {
            progressoAlvo = 1f;
        }

        public void Tremer(float forca, float duracao)
        {
            tremorForca = forca;
            tremorDuracao = duracao;
            tremorRestante = duracao;
        }

        // LateUpdate roda depois de todos os Update: a Teti já se moveu neste frame.
        private void LateUpdate()
        {
            // unscaledDeltaTime: a câmera continua suave mesmo durante o slow-motion da morte.
            Atualizar(Time.unscaledDeltaTime);
        }

        private void Atualizar(float dt)
        {
            Vector3 posicaoAlvo = alvo.position;

            progresso = Mathf.MoveTowards(progresso, progressoAlvo, dt / duracaoTransicao);
            float t = Mathf.SmoothStep(0f, 1f, progresso);

            // Segue a Teti "atrasada" no X e no Y (suavização exponencial, independe do FPS).
            float fator = 1f - Mathf.Exp(-suavidade * dt);
            xSuave = Mathf.Lerp(xSuave, posicaoAlvo.x * seguirLateral, fator);
            ySuave = Mathf.Lerp(ySuave, posicaoAlvo.y * seguirPulo, fator);
            Vector3 baseJogo = new Vector3(xSuave, ySuave, posicaoAlvo.z);

            // Órbita: interpolamos ângulo, distância e altura (e não a posição direto),
            // assim a câmera dá a volta em vez de atravessar a cabeça da Teti.
            float angulo = Mathf.Lerp(anguloMenu, 180f, t) * Mathf.Deg2Rad;
            float distancia = Mathf.Lerp(distanciaMenu, distanciaJogo, t);
            float altura = Mathf.Lerp(alturaMenu, alturaJogo, t);
            Vector3 centro = Vector3.Lerp(posicaoAlvo, baseJogo, t);
            Vector3 posicao = centro + new Vector3(Mathf.Sin(angulo) * distancia, altura, Mathf.Cos(angulo) * distancia);

            Vector3 olhar = Vector3.Lerp(posicaoAlvo + olharMenu, baseJogo + olharJogo, t);

            if (tremorRestante > 0f)
            {
                tremorRestante -= dt;
                float forca = tremorForca * Mathf.Clamp01(tremorRestante / tremorDuracao);
                posicao += Random.insideUnitSphere * forca;
            }

            transform.SetPositionAndRotation(posicao, Quaternion.LookRotation(olhar - posicao));
        }
    }
}
