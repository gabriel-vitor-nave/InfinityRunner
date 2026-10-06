using UnityEngine;

namespace TetiCorre
{
    // Toca as músicas (menu e jogo) e os efeitos sonoros.
    // Efeitos usam algumas AudioSources em rodízio, assim vários sons podem tocar ao mesmo tempo
    // (ex.: várias moedas seguidas) e cada um pode ter um "pitch" levemente diferente.
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instancia { get; private set; }

        [Header("Músicas (em loop)")]
        [SerializeField] private AudioClip musicaMenu;
        [SerializeField] private AudioClip musicaJogo;
        [Range(0f, 1f)] [SerializeField] private float volumeMusica = 0.55f;

        [Header("Efeitos")]
        [SerializeField] private AudioClip somMoeda;
        [SerializeField] private AudioClip somPulo;
        [SerializeField] private AudioClip somTrocaDeFaixa;
        [SerializeField] private AudioClip somBatida;
        [SerializeField] private AudioClip somIma;
        [SerializeField] private AudioClip somClique;
        [SerializeField] private AudioClip somRecorde;
        [Range(0f, 1f)] [SerializeField] private float volumeEfeitos = 0.8f;
        [SerializeField] private int canaisDeEfeito = 6;

        private const string ChaveMudo = "TetiCorre_Mudo";

        private AudioSource fonteMusica;
        private AudioSource[] fontesEfeito;
        private int proximaFonte;

        // Moedas seguidas sobem o tom (do-re-mi...), igual em muitos jogos. Volta ao normal após uma pausa.
        private int sequenciaDeMoedas;
        private float tempoUltimaMoeda;

        public bool Mudo { get; private set; }

        private void Awake()
        {
            Instancia = this;

            fonteMusica = gameObject.AddComponent<AudioSource>();
            fonteMusica.loop = true;
            fonteMusica.playOnAwake = false;
            fonteMusica.volume = volumeMusica;

            fontesEfeito = new AudioSource[canaisDeEfeito];
            for (int i = 0; i < canaisDeEfeito; i++)
            {
                fontesEfeito[i] = gameObject.AddComponent<AudioSource>();
                fontesEfeito[i].playOnAwake = false;
            }

            // A versão atual não tem comando de silenciar. Uma preferência da
            // versão antiga não pode deixar o jogo sem música e sem como ativá-la.
            Mudo = false;
            AudioListener.volume = 1f;
        }

        // ---------- Músicas ----------

        public void TocarMusicaMenu() => TrocarMusica(musicaMenu);
        public void TocarMusicaJogo() => TrocarMusica(musicaJogo);
        public void PararMusica() => fonteMusica.Stop();

        private void TrocarMusica(AudioClip musica)
        {
            // Para a música atual e começa a outra do início.
            fonteMusica.Stop();
            fonteMusica.clip = musica;
            if (musica != null) fonteMusica.Play();
        }

        public void PausarMusica(bool pausar)
        {
            if (pausar) fonteMusica.Pause();
            else fonteMusica.UnPause();
        }

        public void AlternarMudo()
        {
            Mudo = !Mudo;
            AudioListener.volume = Mudo ? 0f : 1f;
            PlayerPrefs.SetInt(ChaveMudo, Mudo ? 1 : 0);
            PlayerPrefs.Save();
        }

        // ---------- Efeitos ----------

        public void TocarMoeda()
        {
            sequenciaDeMoedas = Time.time - tempoUltimaMoeda < 0.6f ? Mathf.Min(sequenciaDeMoedas + 1, 8) : 0;
            tempoUltimaMoeda = Time.time;
            Tocar(somMoeda, 1f + sequenciaDeMoedas * 0.06f, 0.7f);
        }

        public void TocarPulo() => Tocar(somPulo, Random.Range(0.95f, 1.05f));
        public void TocarTrocaDeFaixa() => Tocar(somTrocaDeFaixa, Random.Range(0.9f, 1.1f), 0.5f);
        public void TocarBatida() => Tocar(somBatida, 1f);
        public void TocarIma() => Tocar(somIma, 1f);
        public void TocarClique() => Tocar(somClique, 1f);
        public void TocarRecorde() => Tocar(somRecorde, 1f);

        private void Tocar(AudioClip clip, float pitch, float volume = 1f)
        {
            if (clip == null) return;

            AudioSource fonte = fontesEfeito[proximaFonte];
            proximaFonte = (proximaFonte + 1) % fontesEfeito.Length;

            fonte.pitch = pitch;
            fonte.PlayOneShot(clip, volume * volumeEfeitos);
        }
    }
}
