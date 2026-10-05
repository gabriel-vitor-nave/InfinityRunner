using UnityEngine;

namespace TetiCorre
{
    // Cuida só da parte visual da Teti: qual animação tocar e a inclinação ao trocar de faixa.
    // As transições são feitas por código (CrossFade), então o Animator Controller é bem simples:
    // só tem os estados, sem setas. Fica fácil de entender o que toca e quando.
    public class AnimacaoDoJogador : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        [Tooltip("Modelo 3D da Teti (filho do Jogador). É ele que inclina ao trocar de faixa.")]
        [SerializeField] private Transform modelo;

        [SerializeField] private float tempoDeTransicao = 0.15f;

        [Header("Menu")]
        [Tooltip("De quantos em quantos segundos a Teti troca de dança no menu.")]
        [SerializeField] private float intervaloEntreDancas = 7f;

        [Header("Inclinação ao trocar de faixa")]
        [SerializeField] private float anguloMaximo = 18f;
        [SerializeField] private float suavidade = 12f;

        private bool dancando;
        private float proximaTrocaDeDanca;
        private int dancaAtual;
        private float anguloAtual;

        public void Dancar()
        {
            dancando = true;
            dancaAtual = Random.Range(0, NomesDasAnimacoes.HashDancas.Length);
            Tocar(NomesDasAnimacoes.HashDancas[dancaAtual], 0f);
            proximaTrocaDeDanca = Time.time + intervaloEntreDancas;
            anguloAtual = 0f;
            modelo.localRotation = Quaternion.identity;
        }

        public void Correr()
        {
            dancando = false;
            Tocar(NomesDasAnimacoes.HashCorrendo, tempoDeTransicao);
        }

        public void Pular()
        {
            Tocar(NomesDasAnimacoes.HashPulando, 0.05f);
        }

        public void Aterrissar()
        {
            Tocar(NomesDasAnimacoes.HashCorrendo, tempoDeTransicao);
        }

        public void Morrer()
        {
            dancando = false;
            Tocar(NomesDasAnimacoes.HashMorto, 0.05f);
        }

        // velocidadeLateral em m/s: negativo = indo pra esquerda.
        public void Inclinar(float velocidadeLateral, float velocidadeMaximaLateral)
        {
            float alvo = Mathf.Clamp(velocidadeLateral / velocidadeMaximaLateral, -1f, 1f) * anguloMaximo;
            anguloAtual = Mathf.Lerp(anguloAtual, alvo, 1f - Mathf.Exp(-suavidade * Time.deltaTime));
            modelo.localRotation = Quaternion.Euler(0f, anguloAtual, -anguloAtual * 0.3f);
        }

        private void Update()
        {
            if (!dancando || Time.time < proximaTrocaDeDanca) return;

            // Sorteia uma dança diferente da atual.
            int quantidade = NomesDasAnimacoes.HashDancas.Length;
            dancaAtual = (dancaAtual + Random.Range(1, quantidade)) % quantidade;
            Tocar(NomesDasAnimacoes.HashDancas[dancaAtual], 0.4f);
            proximaTrocaDeDanca = Time.time + intervaloEntreDancas;
        }

        private void Tocar(int estado, float transicao)
        {
            animator.CrossFadeInFixedTime(estado, transicao);
        }
    }
}
