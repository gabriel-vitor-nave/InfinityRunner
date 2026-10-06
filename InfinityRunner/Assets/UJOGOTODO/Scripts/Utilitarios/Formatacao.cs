namespace TetiCorre
{
    // Formata números do jeito brasileiro, sem depender da "cultura" do sistema
    // (no WebGL a cultura pode vir em inglês e mostrar 0.37 em vez de 0,37).
    public static class Formatacao
    {
        // 37 centavos → "0,37 ponto" · 250 centavos → "2,50 pontos"
        public static string Nota(int centavos)
        {
            int inteiro = centavos / 100;
            int resto = centavos % 100;
            string unidade = centavos < 200 ? "ponto" : "pontos";
            return $"{inteiro},{resto:00} {unidade}";
        }

        // 1234 → "1.234 m"
        public static string Distancia(int metros)
        {
            return $"{Milhar(metros)} m";
        }

        private static string Milhar(int valor)
        {
            if (valor < 1000) return valor.ToString();
            return $"{Milhar(valor / 1000)}.{valor % 1000:000}";
        }
    }
}
