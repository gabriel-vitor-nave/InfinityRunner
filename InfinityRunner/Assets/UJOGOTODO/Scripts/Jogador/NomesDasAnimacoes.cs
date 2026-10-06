using UnityEngine;

namespace TetiCorre
{
    // Nomes dos estados do Animator da Teti (Teti.controller).
    // O script de Editor "Montar Jogo" cria os estados com estes nomes, e o jogo toca por eles.
    // Ficam num lugar só pra nunca ter nome digitado errado em dois arquivos diferentes.
    public static class NomesDasAnimacoes
    {
        public const string Correndo = "Correndo";
        public const string Pulando = "Pulando";
        public const string Morto = "Morto";
        public const string DancaBoba = "Danca_Boba";    // Silly Dancing
        public const string DancaTwerk = "Danca_Twerk";  // Dancing Twerk
        public const string DancaFeliz = "Danca_Feliz";  // Happy Idle

        public static readonly string[] Dancas = { DancaBoba, DancaTwerk, DancaFeliz };

        // Animator.StringToHash transforma o nome num número uma vez só (comparar números é mais rápido que texto).
        public static readonly int HashCorrendo = Animator.StringToHash(Correndo);
        public static readonly int HashPulando = Animator.StringToHash(Pulando);
        public static readonly int HashMorto = Animator.StringToHash(Morto);
        public static readonly int[] HashDancas =
        {
            Animator.StringToHash(DancaBoba),
            Animator.StringToHash(DancaTwerk),
            Animator.StringToHash(DancaFeliz)
        };
    }
}
