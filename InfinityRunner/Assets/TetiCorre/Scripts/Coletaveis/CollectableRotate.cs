using UnityEngine;

namespace TetiCorre
{
    // Faz o coletável girar e flutuar.
    // Antes girava "1 grau por frame": a 30 FPS girava devagar e a 144 FPS girava rápido.
    // Agora multiplica por Time.deltaTime, então gira igual em qualquer computador.
    public class CollectableRotate : MonoBehaviour
    {
        [Tooltip("Graus por segundo.")]
        [SerializeField] private float rotationSpeed = 180f;

        [Header("Flutuar")]
        [SerializeField] private float alturaFlutuacao = 0.15f;
        [SerializeField] private float velocidadeFlutuacao = 3f;

        [Tooltip("Objeto que gira/flutua (o modelo). Se vazio, usa este mesmo objeto.")]
        [SerializeField] private Transform visual;

        private float alturaBase;
        private float fase;

        private void Awake()
        {
            if (visual == null) visual = transform;
            alturaBase = visual.localPosition.y;
        }

        private void OnEnable()
        {
            // Fase aleatória pra fileira de moedas não subir e descer toda sincronizada.
            fase = Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            visual.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);

            Vector3 posicao = visual.localPosition;
            posicao.y = alturaBase + Mathf.Sin(Time.time * velocidadeFlutuacao + fase) * alturaFlutuacao;
            visual.localPosition = posicao;
        }
    }
}
