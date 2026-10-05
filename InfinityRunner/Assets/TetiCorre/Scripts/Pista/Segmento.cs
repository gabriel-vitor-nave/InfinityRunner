using System.Collections.Generic;
using UnityEngine;

namespace TetiCorre
{
    // Um pedaço da pista (chão, trilhos e cenário). O Generator coloca vários em fila
    // e recicla os que ficam pra trás. Os itens (moedas/obstáculos) que o gerador coloca
    // em cima ficam registrados aqui, pra serem devolvidos junto com o segmento.
    public class Segmento : MonoBehaviour
    {
        [Tooltip("Grupos de cenário alternativos: em cada grupo, só UM filho fica ativo (sorteado). " +
                 "Ex.: um grupo de prédios com 3 versões diferentes. Dá variedade sem criar mais objetos.")]
        [SerializeField] private Transform[] gruposDeVariacao = new Transform[0];

        private readonly List<ItemDaPista> itens = new List<ItemDaPista>();
        private ObjectPool<Segmento> poolDeOrigem;

        public float InicioZ => transform.position.z;

        public void Preparar(ObjectPool<Segmento> pool)
        {
            poolDeOrigem = pool;

            for (int g = 0; g < gruposDeVariacao.Length; g++)
            {
                Transform grupo = gruposDeVariacao[g];
                int escolhido = Random.Range(0, grupo.childCount);
                for (int i = 0; i < grupo.childCount; i++)
                {
                    grupo.GetChild(i).gameObject.SetActive(i == escolhido);
                }
            }
        }

        public void AdicionarItem(ItemDaPista item)
        {
            itens.Add(item);
        }

        // Devolve todos os itens deste segmento e depois o próprio segmento pros seus pools.
        public void Reciclar()
        {
            for (int i = 0; i < itens.Count; i++)
            {
                itens[i].DevolverAoPool();
            }
            itens.Clear();
            poolDeOrigem.Devolver(this);
        }
    }
}
