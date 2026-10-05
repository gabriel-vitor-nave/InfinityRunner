using UnityEngine;

namespace TetiCorre
{
    // Classe base pra tudo que o gerador coloca em cima da pista (moedas, obstáculos, ímã).
    // Cada item sabe de qual pool veio, então o segmento consegue devolver todos de uma vez
    // quando fica pra trás do jogador.
    public abstract class ItemDaPista : MonoBehaviour
    {
        private ObjectPool<ItemDaPista> poolDeOrigem;

        // Chamado pelo Generator toda vez que o item é colocado na pista.
        public void EntrarNaPista(ObjectPool<ItemDaPista> pool)
        {
            poolDeOrigem = pool;
            AoEntrarNaPista();
        }

        // Cada tipo de item "reseta" o próprio estado aqui (ex.: moeda volta a ser não coletada).
        protected virtual void AoEntrarNaPista() { }

        public void DevolverAoPool()
        {
            poolDeOrigem.Devolver(this);
        }
    }
}
