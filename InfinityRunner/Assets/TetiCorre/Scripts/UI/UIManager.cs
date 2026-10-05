using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TetiCorre
{
    // Interface: menu (botão Play), HUD da corrida, pausa e a tela preta das transições.
    // Os botões são ligados aqui por código (AddListener), então dá pra ver tudo num lugar só.
    public class UIManager : MonoBehaviour
    {
        [Header("Menu")]
        [SerializeField] private CanvasGroup painelMenu;
        [SerializeField] private Button botaoJogar;
        [SerializeField] private TMP_Text textoUltimaCorrida;
        [SerializeField] private TMP_Text textoRecorde;
        [SerializeField] private GameObject seloNovoRecorde;
        [SerializeField] private TMP_Text textoControles;

        [Header("Corrida (HUD)")]
        [SerializeField] private CanvasGroup painelJogo;
        [SerializeField] private TMP_Text textoDistancia;
        [SerializeField] private TMP_Text textoNota;
        [SerializeField] private GameObject indicadorIma;
        [SerializeField] private Image barraIma;
        [SerializeField] private Button botaoPausa;

        [Header("Pausa")]
        [SerializeField] private CanvasGroup painelPausa;
        [SerializeField] private Button botaoContinuar;
        [SerializeField] private Button botaoMenu;

        [Header("Geral")]
        [SerializeField] private Button botaoSom;
        [SerializeField] private Image iconeSom;
        [SerializeField] private Sprite spriteSomLigado;
        [SerializeField] private Sprite spriteSomDesligado;
        [SerializeField] private CanvasGroup telaPreta;
        [SerializeField] private float duracaoFade = 0.35f;

        // Guardamos o último valor mostrado: o texto só é refeito quando o número muda.
        // (Montar texto todo frame gera lixo de memória à toa.)
        private int distanciaMostrada = -1;
        private int centavosMostrados = -1;
        private bool imaMostrado = true;

        private void Awake()
        {
            botaoJogar.onClick.AddListener(() => GameManager.Instancia.Jogar());
            botaoPausa.onClick.AddListener(() => GameManager.Instancia.Pausar());
            botaoContinuar.onClick.AddListener(() => GameManager.Instancia.Continuar());
            botaoMenu.onClick.AddListener(() => GameManager.Instancia.DesistirEVoltarAoMenu());
            botaoSom.onClick.AddListener(() => GameManager.Instancia.AlternarSom());

            textoControles.text = Application.isMobilePlatform
                ? "Deslize pros lados pra trocar de faixa\nDeslize pra cima pra pular"
                : "A / D  ou  SETAS: trocar de faixa\nESPAÇO / W: pular     S: descer rápido\nESC: pausar     M: som";

            Mostrar(telaPreta, false);
        }

        private void Start()
        {
            AtualizarIconeDeSom(AudioManager.Instancia.Mudo);
        }

        public void MostrarMenu(ResultadoDaCorrida ultimaCorrida, int recordeDistancia, int recordeCentavos)
        {
            Mostrar(painelMenu, true);
            Mostrar(painelJogo, false);
            Mostrar(painelPausa, false);

            textoUltimaCorrida.gameObject.SetActive(ultimaCorrida.jogou);
            if (ultimaCorrida.jogou)
            {
                textoUltimaCorrida.text = $"Última corrida\n<b>{Formatacao.Distancia(ultimaCorrida.distancia)}</b>  ·  <b>{Formatacao.Nota(ultimaCorrida.centavos)}</b>";
            }
            seloNovoRecorde.SetActive(ultimaCorrida.novoRecorde);

            textoRecorde.text = $"Recorde\n<b>{Formatacao.Distancia(recordeDistancia)}</b>  ·  <b>{Formatacao.Nota(recordeCentavos)}</b>";
        }

        public void MostrarJogo()
        {
            Mostrar(painelMenu, false);
            Mostrar(painelJogo, true);
            Mostrar(painelPausa, false);
            distanciaMostrada = -1;
            centavosMostrados = -1;
            AtualizarJogo(0, 0, false, 0f);
        }

        public void MostrarPausa(bool pausado)
        {
            Mostrar(painelPausa, pausado);
        }

        public void AtualizarJogo(int distancia, int centavos, bool imaAtivo, float fracaoIma)
        {
            if (distancia != distanciaMostrada)
            {
                distanciaMostrada = distancia;
                textoDistancia.text = Formatacao.Distancia(distancia);
            }

            if (centavos != centavosMostrados)
            {
                centavosMostrados = centavos;
                textoNota.text = Formatacao.Nota(centavos);
            }

            if (imaAtivo != imaMostrado)
            {
                imaMostrado = imaAtivo;
                indicadorIma.SetActive(imaAtivo);
            }
            if (imaAtivo) barraIma.fillAmount = fracaoIma;
        }

        public void AtualizarIconeDeSom(bool mudo)
        {
            iconeSom.sprite = mudo ? spriteSomDesligado : spriteSomLigado;
        }

        // Escurece (ou clareia) a tela. Usa tempo "real" pra funcionar mesmo com o jogo pausado/lento.
        public IEnumerator Escurecer(bool escurecer)
        {
            telaPreta.gameObject.SetActive(true);
            telaPreta.blocksRaycasts = true;

            float inicio = telaPreta.alpha;
            float fim = escurecer ? 1f : 0f;
            for (float t = 0f; t < duracaoFade; t += Time.unscaledDeltaTime)
            {
                telaPreta.alpha = Mathf.Lerp(inicio, fim, t / duracaoFade);
                yield return null;
            }
            telaPreta.alpha = fim;

            if (!escurecer) Mostrar(telaPreta, false);
        }

        private static void Mostrar(CanvasGroup painel, bool visivel)
        {
            painel.alpha = visivel ? 1f : 0f;
            painel.interactable = visivel;
            painel.blocksRaycasts = visivel;
            painel.gameObject.SetActive(visivel);
        }
    }
}
