using System.Collections.Generic;
using UnityEngine;

namespace TetiCorre
{
    // Object Pooling: em vez de criar (Instantiate) e destruir (Destroy) objetos o tempo todo,
    // criamos alguns no começo e ficamos REUSANDO. Quando um objeto "sai de cena" ele só é
    // desativado e volta pra pilha de livres. Isso evita travadinhas do Garbage Collector,
    // o que é importante principalmente no WebGL e no celular.
    public class ObjectPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform pai;
        private readonly Stack<T> livres = new Stack<T>();

        public ObjectPool(T prefab, Transform pai, int quantidadeInicial)
        {
            this.prefab = prefab;
            this.pai = pai;

            // "Aquece" o pool: cria tudo agora, no carregamento, e não no meio da partida.
            for (int i = 0; i < quantidadeInicial; i++)
            {
                livres.Push(Criar());
            }
        }

        private T Criar()
        {
            T obj = Object.Instantiate(prefab, pai);
            obj.gameObject.SetActive(false);
            return obj;
        }

        public T Pegar(Vector3 posicao, Quaternion rotacao)
        {
            // Se acabaram os livres, cria mais um (o pool cresce sozinho se precisar).
            T obj = livres.Count > 0 ? livres.Pop() : Criar();
            obj.transform.SetPositionAndRotation(posicao, rotacao);
            obj.gameObject.SetActive(true);
            return obj;
        }

        public void Devolver(T obj)
        {
            obj.gameObject.SetActive(false);
            livres.Push(obj);
        }
    }
}
