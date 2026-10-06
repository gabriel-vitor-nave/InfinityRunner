using System.Collections;
using UnityEngine;

namespace TetiCorre
{
    public enum EstadoDoJogo
    {
        Menu,     // Teti dançando, botão Play
        Jogando,  // correndo
        Pausado,
        Morto     // bateu: animação de queda e depois volta pro menu
    }

    // O "cérebro" do jogo: guarda o estado atual (máquina de estados) e coordena
    // jogador, pista, câmera, áudio e interface. Tudo acontece numa cena só, sem recarregar:
    // ao voltar pro menu a pista é reciclada (pools) e a Teti volta pro início.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instancia { get; private set; }

        [SerializeField] private ConfiguracaoDoJogo config;
        [SerializeField] private PlayerMovement jogador;
        [SerializeField] private Generator gerador;
        [SerializeField] private CameraFollow cameraDoJogo;
        [SerializeField] private UIManager interfaceDoJogo;
        [SerializeField] private EfeitosVisuais efeitos;

        private const string ChaveRecordeDistancia = "TetiCorre_RecordeDistancia";
        private const string ChaveRecordeCentavos = "TetiCorre_RecordeCentavos";

        public ConfiguracaoDoJogo Config => config;
        public PlayerMovement Jogador => jogador;
        public EstadoDoJogo Estado { get; private set; }
        public float Velocidade { get; private set; }
        public int Moedas { get; private set; }

        // A Teti sempre começa no Z = 0, então a distância é a própria posição Z.
        public int Distancia => Mathf.Max(0, Mathf.FloorToInt(jogador.transform.position.z));
        public int Centavos => Moedas * config.centavosPorMoeda;

        private int recordeDistancia;
        private int recordeCentavos;
        private ResultadoDaCorrida ultimaCorrida;

        private void Awake()
        {
            Instancia = this;
            recordeDistancia = PlayerPrefs.GetInt(ChaveRecordeDistancia, 0);
            recordeCentavos = PlayerPrefs.GetInt(ChaveRecordeCentavos, 0);
        }

        private void Start()
        {
            IrParaMenu();
        }

        private void Update()
        {
            switch (Estado)
            {
                case EstadoDoJogo.Menu:
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Jogar();
                    break;

                case EstadoDoJogo.Jogando:
                    // A velocidade aumenta aos poucos até o máximo: é isso que deixa o jogo mais difícil.
                    Velocidade = Mathf.Min(Velocidade + config.aceleracao * Time.deltaTime, config.velocidadeMaxima);
                    interfaceDoJogo.AtualizarJogo(Distancia, Centavos, jogador.ImaAtivo, jogador.FracaoImaRestante);
                    efeitos.AtualizarVelocidade(Mathf.InverseLerp(config.velocidadeInicial, config.velocidadeMaxima, Velocidade));
                    break;

                case EstadoDoJogo.Pausado:
                    if (ApertouPausa()) Continuar();
                    break;
            }

        }

        private static bool ApertouPausa()
        {
            return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
        }

        // ---------- Transições de estado ----------

        public void Jogar()
        {
            if (Estado != EstadoDoJogo.Menu) return;

            Estado = EstadoDoJogo.Jogando;
            Moedas = 0;
            Velocidade = config.velocidadeInicial;

            AudioManager.Instancia.TocarClique();
            AudioManager.Instancia.TocarMusicaJogo(); // para a música do menu e toca a do jogo em loop
            jogador.ComecarACorrer();
            cameraDoJogo.IrParaJogo();
            interfaceDoJogo.MostrarJogo();
        }

        public void Pausar()
        {
            if (Estado != EstadoDoJogo.Jogando) return;

            Estado = EstadoDoJogo.Pausado;
            Time.timeScale = 0f; // congela tudo que usa Time.deltaTime
            AudioManager.Instancia.PausarMusica(true);
            interfaceDoJogo.MostrarPausa(true);
        }

        public void Continuar()
        {
            if (Estado != EstadoDoJogo.Pausado) return;

            Estado = EstadoDoJogo.Jogando;
            Time.timeScale = 1f;
            AudioManager.Instancia.TocarClique();
            AudioManager.Instancia.PausarMusica(false);
            interfaceDoJogo.MostrarPausa(false);
        }

        // Botão "Menu" da pausa: desiste da corrida (mas o recorde ainda conta).
        public void DesistirEVoltarAoMenu()
        {
            if (Estado != EstadoDoJogo.Pausado) return;

            Estado = EstadoDoJogo.Morto; // evita "despausar" durante o fade
            Velocidade = 0f;
            Time.timeScale = 1f;
            AudioManager.Instancia.TocarClique();
            RegistrarResultado();
            StartCoroutine(TrocarParaMenuComFade());
        }

        public void JogadorBateu()
        {
            if (Estado != EstadoDoJogo.Jogando) return;

            Estado = EstadoDoJogo.Morto;
            Velocidade = 0f;

            jogador.Morrer();
            AudioManager.Instancia.PararMusica();
            AudioManager.Instancia.TocarBatida();
            cameraDoJogo.Tremer(0.35f, 0.6f);
            efeitos.Batida(jogador.CentroDoCorpo);
            efeitos.AtualizarVelocidade(0f);

            RegistrarResultado();
            StartCoroutine(SequenciaDeMorte());
        }

        private IEnumerator SequenciaDeMorte()
        {
            // Slow-motion rapidinho pra dar peso na batida.
            Time.timeScale = 0.3f;
            yield return new WaitForSecondsRealtime(0.6f);
            Time.timeScale = 1f;

            // Tempo pra ver a Teti caindo (animação Stumble Backwards).
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, config.tempoAteVoltarAoMenu - 0.6f));

            yield return TrocarParaMenuComFade();
        }

        private IEnumerator TrocarParaMenuComFade()
        {
            yield return interfaceDoJogo.Escurecer(true);
            IrParaMenu();
            if (ultimaCorrida.novoRecorde) AudioManager.Instancia.TocarRecorde();
            yield return interfaceDoJogo.Escurecer(false);
        }

        private void IrParaMenu()
        {
            Estado = EstadoDoJogo.Menu;
            Time.timeScale = 1f;
            Velocidade = 0f;

            jogador.PrepararParaMenu();
            gerador.Reiniciar();
            cameraDoJogo.IrParaMenu();
            efeitos.AtualizarVelocidade(0f);
            interfaceDoJogo.MostrarMenu(ultimaCorrida, recordeDistancia, recordeCentavos);
            AudioManager.Instancia.TocarMusicaMenu();
        }

        // ---------- Pontuação ----------

        public void MoedaColetada(Vector3 posicao)
        {
            Moedas++;
            AudioManager.Instancia.TocarMoeda();
            efeitos.Moeda(posicao);
        }

        public void ImaColetado(Vector3 posicao)
        {
            jogador.AtivarIma();
            AudioManager.Instancia.TocarIma();
            efeitos.Ima(posicao);
        }

        private void RegistrarResultado()
        {
            bool recordeDeDistancia = Distancia > recordeDistancia;
            bool recordeDeNota = Centavos > recordeCentavos;

            if (recordeDeDistancia)
            {
                recordeDistancia = Distancia;
                PlayerPrefs.SetInt(ChaveRecordeDistancia, recordeDistancia);
            }
            if (recordeDeNota)
            {
                recordeCentavos = Centavos;
                PlayerPrefs.SetInt(ChaveRecordeCentavos, recordeCentavos);
            }
            // No WebGL o PlayerPrefs é salvo no navegador (IndexedDB); Save() garante que grava agora.
            PlayerPrefs.Save();

            ultimaCorrida = new ResultadoDaCorrida
            {
                jogou = true,
                distancia = Distancia,
                centavos = Centavos,
                novoRecorde = recordeDeDistancia || recordeDeNota
            };
        }

        // ---------- Outros ----------

        public void AlternarSom()
        {
            AudioManager.Instancia.AlternarMudo();
            interfaceDoJogo.AtualizarIconeDeSom(AudioManager.Instancia.Mudo);
        }

        // No navegador, se trocar de aba ou clicar fora, pausa sozinho.
        private void OnApplicationFocus(bool temFoco)
        {
            if (!temFoco) Pausar();
        }
    }

    // Dados da última corrida, mostrados no menu.
    public struct ResultadoDaCorrida
    {
        public bool jogou;
        public int distancia;
        public int centavos;
        public bool novoRecorde;
    }
}
