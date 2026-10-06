using UnityEngine;

namespace TetiCorre
{
    // Aperte F12 (só dentro da Unity) pra salvar um print em alta resolução na pasta "Capturas",
    // ao lado da pasta Assets. Útil pra montar a página do itch.io.
    public class CapturaDeTela : MonoBehaviour
    {
        [Tooltip("Multiplica a resolução do print (2 = dobro do tamanho da janela Game).")]
        [SerializeField] private int multiplicador = 2;

#if UNITY_EDITOR
        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.F12)) return;

            string pasta = System.IO.Path.Combine(Application.dataPath, "..", "Capturas");
            System.IO.Directory.CreateDirectory(pasta);
            string arquivo = System.IO.Path.Combine(pasta, $"TetiCorre_{System.DateTime.Now:yyyyMMdd_HHmmss}.png");
            ScreenCapture.CaptureScreenshot(arquivo, multiplicador);
            Debug.Log($"[CapturaDeTela] Print salvo em {arquivo}");
        }
#endif
    }
}
