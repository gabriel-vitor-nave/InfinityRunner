namespace TetiCorre
{
    // Power-up extra: por alguns segundos, as moedas próximas voam até a Teti.
    // O efeito em si está no PlayerMovement (tempo restante) e na Moeda (ser puxada).
    public class Ima : ItemDaPista
    {
        private bool coletado;

        protected override void AoEntrarNaPista()
        {
            coletado = false;
        }

        public void Coletar()
        {
            if (coletado) return;
            coletado = true;

            gameObject.SetActive(false);
            GameManager.Instancia.ImaColetado(transform.position);
        }
    }
}
