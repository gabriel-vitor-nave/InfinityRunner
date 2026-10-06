using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TetiCorre.Editor
{
    // Teste de integração: roda em Play, com os componentes e os triggers reais.
    [InitializeOnLoad]
    public static class ValidacaoDoJogo
    {
        private const string Chave = "TetiCorre.Validando";
        private static int etapa;
        private static double inicio;
        private static float inicioSimulado;
        private static float zPausa;
        private static int moedasAntes;
        private static bool viuPulo;
        private static float alturaObservada;
        private static float alturaPuloInicial;
        private static int distanciaDaCorrida;
        private static string erro;

        static ValidacaoDoJogo() { EditorApplication.playModeStateChanged += AoMudarEstado; }

        public static void Executar()
        {
            EditorSceneManager.OpenScene("Assets/TetiCorre/Cenas/TetiCorre.unity");
            SessionState.SetBool(Chave, true);
            EditorApplication.isPlaying = true;
        }

        public static void MontarEExecutar()
        {
            MontadorDoJogo.Montar();
            Executar();
        }

        private static void AoMudarEstado(PlayModeStateChange estado)
        {
            if (!SessionState.GetBool(Chave, false)) return;
            if (estado != PlayModeStateChange.EnteredPlayMode) return;
            etapa = 0; erro = null; inicio = EditorApplication.timeSinceStartup;
            Application.logMessageReceived += RegistrarErro;
            EditorApplication.update += Verificar;
        }

        private static void RegistrarErro(string mensagem, string pilha, LogType tipo)
        { if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert) erro = mensagem + "\n" + pilha; }

        private static void Comando(PlayerMovement jogador, string nome, params object[] args)
        { typeof(PlayerMovement).GetMethod(nome, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(jogador, args); }

        private static void Proxima() { etapa++; inicio = EditorApplication.timeSinceStartup; inicioSimulado = Time.time; }

        private static void Verificar()
        {
            try
            {
                if (erro != null) throw new Exception(erro);
                double tempo = EditorApplication.timeSinceStartup - inicio;
                float tempoJogo = Time.time - inicioSimulado;
                if (tempo > (etapa == 0 ? 60 : 15)) throw new Exception("Tempo excedido na etapa " + etapa);
                var jogo = GameManager.Instancia;
                if (jogo == null) return;
                var jogador = jogo.Jogador;
                switch (etapa)
                {
                    case 0:
                        if (Time.timeSinceLevelLoad < .5f) return;
                        Exigir(jogo.Estado == EstadoDoJogo.Menu, "Estado inicial do menu");
                        Exigir(UnityEngine.Object.FindObjectsOfType<Camera>().Length == 1, "Uma única câmera ativa");
                        var malhas = jogador.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
                        var bounds = malhas[0].bounds;
                        foreach (var malha in malhas) bounds.Encapsulate(malha.bounds);
                        Vector3 deslocamento = bounds.center - jogador.transform.position;
                        Exigir(new Vector2(deslocamento.x, deslocamento.z).magnitude < 1.5f, "Modelo alinhado com o collider do jogador");
                        Exigir(!UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Button>(true)
                            .Any(b => b.name == "PAUSA" || b.name.StartsWith("SOM")), "Interface sem botões de pausa e som");
                        float sola = float.PositiveInfinity;
                        var pose = new Mesh();
                        foreach (var pele in jogador.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {
                            if (!pele.enabled) continue;
                            pele.BakeMesh(pose);
                            foreach (var ponto in pose.vertices) sola = Mathf.Min(sola, (pele.transform.position + pele.transform.rotation * ponto).y);
                        }
                        UnityEngine.Object.DestroyImmediate(pose);
                        Exigir(Mathf.Abs(sola - jogador.transform.position.y) < .02f, "Pose da Teti apoiada no asfalto");
                        Exigir(jogador.GetComponent<ApoioVisualDaTeti>().QuantidadePontosDeApoio > 0, "Apoio com vértices dos pés pré-calculados");
                        var distancias = UnityEngine.Object.FindObjectsOfType<VisibilidadePorDistancia>();
                        Exigir(distancias.Any(v => v.NivelAtual == 1) && distancias.Any(v => v.NivelAtual == 2), "Cenário distante simplificado e ocultado");
                        foreach (var longe in distancias.Where(v => v.NivelAtual == 2))
                            Exigir(longe.GetComponentsInChildren<Renderer>().All(r => !r.enabled), "Renderização distante desativada");
                        Exigir(QualitySettings.shadowDistance <= 25.1f, "Sombras só perto do jogador");
                        Exigir(Shader.GetGlobalFloat("_TetiForcaCurva") > 0f, "Curvatura visual ativa");
                        foreach (var setor in UnityEngine.Object.FindObjectsOfType<Segmento>())
                        {
                            var itens = (List<ItemDaPista>)typeof(Segmento).GetField("itens", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(setor);
                            Exigir(itens.OfType<Obstaculo>().Count() <= 3, "Máximo de três obstáculos por setor");
                            Exigir(itens.OfType<Moeda>().Any(), "Moedas em todos os setores");
                            foreach (var carro in itens.OfType<Obstaculo>())
                            {
                                var pontos = carro.GetComponentsInChildren<MeshFilter>()
                                    .Where(f => f.GetComponent<Renderer>() != null)
                                    .SelectMany(f => f.sharedMesh.vertices.Select(v => f.transform.TransformPoint(v))).ToArray();
                                var minimo = pontos.Aggregate(Vector3.Min);
                                var maximo = pontos.Aggregate(Vector3.Max);
                                float centro = (minimo.x + maximo.x) / 2f;
                                Exigir(Mathf.Abs(centro - carro.transform.position.x) < .02f, "Carro centralizado na faixa");
                                Exigir(Mathf.Abs(minimo.y) < .02f, "Rodas apoiadas no asfalto");
                                Exigir(minimo.x > -5.25f && maximo.x < 5.25f, "Carro inteiro dentro da rua");
                            }
                        }
                        Exigir(jogo.Config.comprimentoSegmento == 50 && jogo.Config.larguraFaixa == 3.5f, "Setor de 10,5 x 50");
                        Capturar("Menu");
                        bool mudo = AudioManager.Instancia.Mudo;
                        jogo.AlternarSom(); Exigir(AudioManager.Instancia.Mudo != mudo, "Alternar som");
                        jogo.AlternarSom(); Exigir(AudioManager.Instancia.Mudo == mudo, "Restaurar som");
                        jogo.Jogar(); Proxima(); break;
                    case 1:
                        if (tempoJogo < .7f) return;
                        Exigir(jogador.transform.position.z > 2, "Corrida automática");
                        Capturar("Corrida");
                        jogo.Pausar(); zPausa = jogador.transform.position.z; Proxima(); break;
                    case 2:
                        if (tempo < .5) return;
                        Exigir(Mathf.Abs(jogador.transform.position.z - zPausa) < .001f, "Pausa congela movimento");
                        jogo.Continuar(); Comando(jogador, "PedirPulo"); viuPulo = false; alturaObservada = 0f; Proxima(); break;
                    case 3:
                        if (jogador.transform.position.y > .3f) viuPulo = true;
                        alturaObservada = Mathf.Max(alturaObservada, jogador.transform.position.y);
                        if (tempoJogo < jogo.Config.TempoNoArEm(jogo.Velocidade) + .15f) return;
                        Exigir(viuPulo && jogador.transform.position.y < .05f, "Pulo e aterrissagem");
                        alturaPuloInicial = alturaObservada;
                        Exigir(alturaObservada > 2.6f, "Salto inicial mais alto");
                        Comando(jogador, "TrocarFaixa", -1); Proxima(); break;
                    case 4:
                        if (tempoJogo < .3f) return;
                        Exigir(Mathf.Abs(jogador.transform.position.x + jogo.Config.larguraFaixa) < .05f, "Faixa esquerda");
                        Comando(jogador, "TrocarFaixa", 1); Comando(jogador, "TrocarFaixa", 1); Proxima(); break;
                    case 5:
                        if (tempoJogo < .5f) return;
                        Exigir(Mathf.Abs(jogador.transform.position.x - jogo.Config.larguraFaixa) < .05f, "Faixa direita");
                        moedasAntes = jogo.Moedas;
                        var moeda = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TetiCorre/Gerados/Moeda.prefab"));
                        moeda.transform.position = jogador.CentroDoCorpo + Vector3.forward * .8f;
                        Proxima(); break;
                    case 6:
                        if (tempoJogo < .3f) return;
                        Exigir(jogo.Moedas > moedasAntes, "Coleta por trigger");
                        var moedaDistante = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TetiCorre/Gerados/Moeda.prefab"));
                        moedaDistante.transform.position = jogador.CentroDoCorpo + Vector3.forward * 8;
                        Physics.SyncTransforms(); int total = jogo.Moedas;
                        Comando(jogador, "VerificarTrajeto", jogador.transform.position, jogador.transform.position + Vector3.forward * 12);
                        Exigir(jogo.Moedas > total, "Coleta ao atravessar o trajeto de um frame longo");
                        var ima = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TetiCorre/Gerados/Ima.prefab"));
                        ima.transform.position = jogador.CentroDoCorpo + Vector3.forward * .8f;
                        Proxima(); break;
                    case 7:
                        if (tempoJogo < .3f) return;
                        Exigir(jogador.ImaAtivo, "Ímã por trigger");
                        var obstaculo = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TetiCorre/Gerados/Vagao.prefab"));
                        obstaculo.transform.position = jogador.transform.position + Vector3.forward * 3;
                        Proxima(); break;
                    case 8:
                        if (tempoJogo < .4f) return;
                        Exigir(jogo.Estado == EstadoDoJogo.Morto, "Morte ao colidir");
                        distanciaDaCorrida = jogo.Distancia; Proxima(); break;
                    case 9:
                        if (jogo.Estado != EstadoDoJogo.Menu) return;
                        Exigir(jogador.transform.position.sqrMagnitude < .01f, "Reinício no menu");
                        Exigir(PlayerPrefs.GetInt("TetiCorre_RecordeDistancia", 0) >= distanciaDaCorrida, "Recorde salvo");
                        jogo.Jogar(); Proxima(); break;
                    case 10:
                        if (tempoJogo < .2f) return;
                        Exigir(jogo.Moedas == 0, "Pontuação reiniciada");
                        jogo.Pausar(); jogo.DesistirEVoltarAoMenu(); Proxima(); break;
                    case 11:
                        if (jogo.Estado != EstadoDoJogo.Menu) return;
                        Exigir(jogo.Velocidade == 0 && Time.timeScale == 1, "Retorno da pausa ao menu");
                        jogo.Jogar(); jogador.transform.position = new Vector3(0, 0, 105);
                        jogo.Pausar(); Proxima(); break;
                    case 12:
                        if (tempo < 1.5) return;
                        jogo.Continuar(); Capturar("Cenario");
                        jogo.Pausar(); UnityEngine.Object.FindObjectOfType<UIManager>().MostrarPausa(false);
                        var ambiente = UnityEngine.Object.FindObjectOfType<AmbienteDaCorrida>();
                        ambiente.AvancarCiclo(ambiente.DuracaoDoPeriodo - TempoCiclo(ambiente) + 8f);
                        Exigir(ambiente.Noite && ambiente.FracaoNoite > .99f, "Primeira noite após um minuto de dia");
                        Proxima(); break;
                    case 13:
                        if (tempo < .5) return;
                        Capturar("Noite");
                        var ciclo = UnityEngine.Object.FindObjectOfType<AmbienteDaCorrida>();
                        var luzes = UnityEngine.Object.FindObjectsOfType<LuzDoPoste>().Select(p => p.GetComponent<Light>()).ToArray();
                        Exigir(luzes.Count(l => l.enabled) > 0 && luzes.Count(l => l.enabled) <= 6, "Postes próximos acesos à noite, com limite de luzes");
                        ciclo.AvancarCiclo(ciclo.DuracaoDoPeriodo - TempoCiclo(ciclo) + 8f);
                        Exigir(!ciclo.Noite && ciclo.DuracaoDoPeriodo == 120f && ciclo.FracaoNoite < .01f, "Segundo dia dura dois minutos");
                        Proxima(); break;
                    case 14:
                        if (tempo < .5) return;
                        Capturar("Amanhecer");
                        Exigir(UnityEngine.Object.FindObjectsOfType<LuzDoPoste>().All(p => !p.GetComponent<Light>().enabled), "Postes desligados de dia");
                        var cicloLongo = UnityEngine.Object.FindObjectOfType<AmbienteDaCorrida>();
                        cicloLongo.AvancarCiclo(120f);
                        Exigir(cicloLongo.Noite && cicloLongo.DuracaoDoPeriodo == 120f, "Segunda noite dura dois minutos");
                        cicloLongo.AvancarCiclo(120f);
                        Exigir(!cicloLongo.Noite && cicloLongo.DuracaoDoPeriodo == 240f, "Próximo dia dura quatro minutos");
                        jogo.DesistirEVoltarAoMenu(); Proxima(); break;
                    case 15:
                        if (jogo.Estado != EstadoDoJogo.Menu) return;
                        jogo.Jogar(); typeof(GameManager).GetProperty("Velocidade").SetValue(jogo, jogo.Config.velocidadeMaxima);
                        VerificarArcos(jogo);
                        Comando(jogador, "PedirPulo"); alturaObservada = 0f; Proxima(); break;
                    case 16:
                        alturaObservada = Mathf.Max(alturaObservada, jogador.transform.position.y);
                        if (tempoJogo < jogo.Config.TempoNoArEm(jogo.Velocidade) + .15f) return;
                        Exigir(alturaObservada > alturaPuloInicial + .5f && jogador.transform.position.y < .05f, "Pulo cresce com a velocidade e aterrissa");
                        Terminar(true, "Jogabilidade, apoio dos pés, limites de renderização e sombras, pulo proporcional à velocidade, arcos de moedas e ciclo dia/noite passaram."); break;
                }
            }
            catch (Exception e) { Terminar(false, e.ToString()); }
        }

        private static void Exigir(bool condicao, string nome) { if (!condicao) throw new Exception("Falhou: " + nome); }
        private static float TempoCiclo(AmbienteDaCorrida ambiente) =>
            (float)typeof(AmbienteDaCorrida).GetField("tempoCiclo", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ambiente);

        private static void VerificarArcos(GameManager jogo)
        {
            var gerador = UnityEngine.Object.FindObjectOfType<Generator>();
            var setor = UnityEngine.Object.FindObjectsOfType<Segmento>().First(s => Mathf.Abs(s.InicioZ) < .1f);
            var method = typeof(Generator).GetMethod("ColocarMoedasEmArco", BindingFlags.NonPublic | BindingFlags.Instance);
            var ids = new HashSet<int>(UnityEngine.Object.FindObjectsOfType<Moeda>().Select(m => m.GetInstanceID()));
            typeof(GameManager).GetProperty("Velocidade").SetValue(jogo, jogo.Config.velocidadeInicial);
            method.Invoke(gerador, new object[] { setor, -3.5f, 20f });
            var lento = UnityEngine.Object.FindObjectsOfType<Moeda>().Where(m => !ids.Contains(m.GetInstanceID())).ToArray();
            foreach (var moeda in lento) ids.Add(moeda.GetInstanceID());
            typeof(GameManager).GetProperty("Velocidade").SetValue(jogo, jogo.Config.velocidadeMaxima);
            method.Invoke(gerador, new object[] { setor, 3.5f, 30f });
            var rapido = UnityEngine.Object.FindObjectsOfType<Moeda>().Where(m => !ids.Contains(m.GetInstanceID())).ToArray();
            Exigir(lento.Length == 5 && rapido.Length == 5, "Cinco moedas em cada arco");
            float alcanceLento = lento.Max(m => m.transform.position.z) - lento.Min(m => m.transform.position.z);
            float alcanceRapido = rapido.Max(m => m.transform.position.z) - rapido.Min(m => m.transform.position.z);
            Exigir(alcanceRapido > alcanceLento * 2f && rapido.Max(m => m.transform.position.y) > lento.Max(m => m.transform.position.y) + .5f,
                "Espaçamento e altura das moedas de pulo acompanham a velocidade");
        }
        private static void Capturar(string nome)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var camera = Camera.main;
            var canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            var anterior = RenderTexture.active;
            var alvo = new RenderTexture(960, 600, 24);
            camera.targetTexture = alvo;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = alvo;
            var imagem = new Texture2D(960, 600, TextureFormat.RGB24, false);
            imagem.ReadPixels(new Rect(0, 0, 960, 600), 0, 0); imagem.Apply();
            Directory.CreateDirectory("Builds/Previews");
            File.WriteAllBytes("Builds/Previews/" + nome + ".png", imagem.EncodeToPNG());
            camera.targetTexture = null; canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            RenderTexture.active = anterior; alvo.Release();
            UnityEngine.Object.Destroy(imagem); UnityEngine.Object.Destroy(alvo);
        }
        private static void Terminar(bool passou, string mensagem)
        {
            SessionState.SetBool(Chave, false); EditorApplication.update -= Verificar;
            Application.logMessageReceived -= RegistrarErro;
            Directory.CreateDirectory("Builds");
            File.WriteAllText("Builds/Validacao.txt", (passou ? "PASSOU\n" : "FALHOU\n") + mensagem);
            Debug.Log(mensagem);
            EditorApplication.Exit(passou ? 0 : 1);
        }
    }
}
