using UnityEngine;

namespace TetiCorre
{
    public enum TipoObstaculo
    {
        Pulavel,   // barreira baixa: dá pra pular OU desviar
        SoDesviar  // vagão: alto demais, só trocando de faixa
    }

    // Marca um objeto como obstáculo. Quando o jogador encosta no collider (trigger) dele, morre.
    // A detecção acontece no PlayerMovement.OnTriggerEnter, que procura este componente.
    public class Obstaculo : ItemDaPista
    {
        [SerializeField] private TipoObstaculo tipo = TipoObstaculo.Pulavel;

        [Tooltip("Comprimento do obstáculo no eixo Z (frente), usado pelo gerador pra não fechar o caminho.")]
        [SerializeField] private float comprimento = 1f;

        public TipoObstaculo Tipo => tipo;
        public float Comprimento => comprimento;
    }
}
