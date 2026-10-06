using UnityEngine;

namespace TetiCorre
{
    // Mostra os setores, moedas e obstáculos na cena sem precisar dar Play.
    // Ao jogar, o gerador assume a pista e recicla seus próprios segmentos.
    public class PreviewDaPista : MonoBehaviour
    {
        private void Awake() { gameObject.SetActive(false); }
    }
}
