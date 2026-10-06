using UnityEngine;

namespace TetiCorre
{
    // Moeda = centavos de ponto na nota.
    // A coleta é detectada pelo PlayerMovement (OnTriggerEnter), que chama Coletar().
    public class Moeda : ItemDaPista
    {
        [Tooltip("Velocidade com que a moeda voa até a Teti quando o ímã está ativo.")]
        [SerializeField] private float velocidadeIma = 30f;

        private bool coletada;
        private bool sendoPuxada;

        protected override void AoEntrarNaPista()
        {
            coletada = false;
            sendoPuxada = false;
        }

        private void Update()
        {
            GameManager jogo = GameManager.Instancia;
            if (jogo == null || jogo.Estado != EstadoDoJogo.Jogando) return;

            PlayerMovement jogador = jogo.Jogador;
            if (!jogador.ImaAtivo && !sendoPuxada) return;

            // Ímã: se a moeda está perto, ela voa até o jogador.
            Vector3 alvo = jogador.CentroDoCorpo;
            Vector3 diferenca = alvo - transform.position;

            if (!sendoPuxada)
            {
                float raio = jogo.Config.raioIma;
                // sqrMagnitude evita a raiz quadrada (mais barato que .magnitude).
                if (diferenca.sqrMagnitude > raio * raio) return;
                sendoPuxada = true;
            }

            // Velocidade da moeda = a do jogador + a do ímã, senão ela nunca alcança.
            float passo = (velocidadeIma + jogo.Velocidade) * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, alvo, passo);
        }

        public void Coletar()
        {
            if (coletada) return;
            coletada = true;

            // Só desativa: quem devolve pro pool é o segmento, quando ficar pra trás.
            gameObject.SetActive(false);
            GameManager.Instancia.MoedaColetada(transform.position);
        }
    }
}
