using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TetiCorre.Editor
{
    // Tudo que depende de UnityEditor fica nesta pasta e não entra no executável.
    public static class MontadorDoJogo
    {
        private const string Raiz = "Assets/TetiCorre";
        private const string Cena = Raiz + "/Cenas/TetiCorre.unity";
        private const string Gerados = Raiz + "/Gerados";
        private static TMP_FontAsset fonte;

        [MenuItem("InfinityRunner/Montar Jogo")]
        public static void Montar()
        {
            if (PrepararRecursos(Montar)) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            // Cria uma cena nova em memória antes de salvar; não modifica outras cenas.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Directory.CreateDirectory(Gerados);
            AssetDatabase.Refresh();
            fonte = AssetDatabase.FindAssets("t:TMP_FontAsset").Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>).FirstOrDefault();
            if (fonte == null)
            {
                var font = AssetDatabase.FindAssets("t:Font").Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<Font>).FirstOrDefault();
                if (font == null) throw new InvalidOperationException("Importe os recursos essenciais de TextMesh Pro.");
                fonte = TMP_FontAsset.CreateFontAsset(font);
                AssetDatabase.CreateAsset(fonte, Gerados + "/Fonte.asset");
                AssetDatabase.AddObjectToAsset(fonte.material, fonte);
                foreach (var atlas in fonte.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, fonte);
            }
            var config = Asset<ConfiguracaoDoJogo>("Configuracao.asset");
            config.comprimentoSegmento = 50f; config.larguraFaixa = 3.5f;
            EditorUtility.SetDirty(config);
            var asfalto = Material("Asfalto", new Color(.13f, .18f, .25f));
            var azul = Material("Azul", new Color(.1f, .55f, .8f));
            var amarelo = Material("Ouro", new Color(1f, .72f, .12f));
            var vermelho = Material("Barreira", new Color(.95f, .24f, .2f));
            var branco = Material("Faixas", new Color(.8f, .9f, .95f));

            var segmentoObj = new GameObject("Segmento");
            var segmento = segmentoObj.AddComponent<Segmento>();
            Cubo("Pista 10,5 x 50", segmentoObj.transform, new Vector3(0, -.2f, 25), new Vector3(10.5f, .4f, 50), asfalto);
            for (int z = 5; z < 50; z += 10)
                ModeloImportado("Roads/Road Lane_03.prefab", segmentoObj.transform,
                    new Vector3(0, -.06f, z), new Vector3(10.5f, .06f, 10));
            foreach (float x in new[] { -1.75f, 1.75f })
                for (int z = 1; z < 50; z += 4)
                    Cubo("Marca", segmentoObj.transform, new Vector3(x, .015f, z), new Vector3(.08f, .02f, 2), branco);
            CriarCidade(segmentoObj.transform);
            segmento = SalvarPrefab(segmento, "Segmento");
            string[] carros = { "Vehicle_Car_color01", "Vehicle_Car_color02", "Vehicle_Car_color03", "Vehicle_Taxi", "Vehicle_Police Car", "Vehicle_SUV_color02" };
            var carrosBaixos = carros.Select((nome, i) => CriarObstaculo(i == 0 ? "Barreira" : "Carro_" + i,
                new Vector3(2.6f, 1.1f, 4.5f), vermelho, TipoObstaculo.Pulavel, nome)).ToArray();
            string[] grandes = { "Vehicle_Bus_color01", "Vehicle_Bus_color02", "Vehicle_Truck_color03" };
            var carrosAltos = grandes.Select((nome, i) => CriarObstaculo(i == 0 ? "Vagao" : "VeiculoAlto_" + i,
                new Vector3(2.8f, 3, 7), azul, TipoObstaculo.SoDesviar, nome)).ToArray();
            var barreira = carrosBaixos[0]; var vagao = carrosAltos[0];
            var moedaObj = new GameObject("Moeda");
            var moeda = moedaObj.AddComponent<Moeda>();
            moedaObj.AddComponent<SphereCollider>().radius = .38f;
            moedaObj.GetComponent<SphereCollider>().isTrigger = true;
            var modeloMoeda = AssetDatabase.LoadAssetAtPath<GameObject>(Raiz + "/Arte/Modelos/Moeda/Moeda.fbx");
            var disco = modeloMoeda != null ? (GameObject)PrefabUtility.InstantiatePrefab(modeloMoeda) : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disco.name = "Disco";
            disco.transform.SetParent(moedaObj.transform, false);
            AjustarModelo(disco, .65f, false);
            foreach (var collider in disco.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
            var materialMoeda = AssetDatabase.LoadAssetAtPath<Material>(Raiz + "/Arte/Materiais/Moeda/Moeda.mat");
            if (materialMoeda != null)
            {
                // A textura antiga de normal é uma cópia da cor: não a usamos como relevo.
                materialMoeda.SetTexture("_BumpMap", null); materialMoeda.DisableKeyword("_NORMALMAP");
                EditorUtility.SetDirty(materialMoeda);
            }
            foreach (var renderer in disco.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = materialMoeda != null ? materialMoeda : amarelo;
            var rotacao = moedaObj.AddComponent<CollectableRotate>(); Ligar(rotacao, "visual", disco.transform);
            moeda = SalvarPrefab(moeda, "Moeda");
            var imaObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            imaObj.name = "Ima";
            imaObj.transform.localScale = Vector3.one * .7f;
            imaObj.GetComponent<Renderer>().sharedMaterial = azul;
            imaObj.GetComponent<Collider>().isTrigger = true;
            var ima = SalvarPrefab(imaObj.AddComponent<Ima>(), "Ima");

            var prefabJogador = AssetDatabase.LoadAssetAtPath<GameObject>(Raiz + "/Prefabs/Jogador/Teti.prefab");
            var jogadorObj = new GameObject("Jogador");
            jogadorObj.name = "Jogador — Teti";
            var jogador = jogadorObj.GetComponent<PlayerMovement>();
            if (jogador == null) jogador = jogadorObj.AddComponent<PlayerMovement>();
            var capsula = jogadorObj.GetComponent<CapsuleCollider>();
            capsula.height = 1.7f; capsula.radius = .3f; capsula.center = new Vector3(0, .85f, 0);
            var modeloAsset = AssetDatabase.LoadAssetAtPath<GameObject>(Raiz + "/Arte/Modelos/Teti/TetiFiluz@Running.fbx");
            if (modeloAsset == null) throw new InvalidOperationException("Modelo da Teti não encontrado.");
            var modelo = (GameObject)PrefabUtility.InstantiatePrefab(prefabJogador != null ? prefabJogador : modeloAsset, jogadorObj.transform);
            // O modelo visual é o prefab INTEIRO: ossos e malhas têm de receber
            // a mesma escala/posição. O controlador de movimento fica só no pai.
            foreach (var componente in modelo.GetComponentsInChildren<PlayerMovement>()) UnityEngine.Object.DestroyImmediate(componente);
            foreach (var componente in modelo.GetComponentsInChildren<Rigidbody>()) UnityEngine.Object.DestroyImmediate(componente);
            foreach (var componente in modelo.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(componente);
            modelo.name = "Teti";
            AjustarModelo(modelo, 1.7f, true);
            var animator = modelo.GetComponentInChildren<Animator>();
            if (animator == null) animator = modelo.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = CriarAnimacoes();
            var animacao = jogadorObj.AddComponent<AnimacaoDoJogador>();
            Ligar(animacao, "animator", animator); Ligar(animacao, "modelo", modelo.transform);
            Ligar(jogador, "config", config); Ligar(jogador, "animacao", animacao);

            var cameraObj = new GameObject("Camera", typeof(Camera), typeof(AudioListener));
            cameraObj.tag = "MainCamera";
            var camera = cameraObj.GetComponent<Camera>();
            camera.backgroundColor = new Color(.45f, .72f, .9f); camera.clearFlags = CameraClearFlags.Skybox;
            RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/Pacotes/Day-Night Skyboxes/Materials/SkyBrightMorning.mat");
            camera.farClipPlane = 220;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = camera.backgroundColor; RenderSettings.fogStartDistance = 100; RenderSettings.fogEndDistance = 200;
            var seguir = cameraObj.AddComponent<CameraFollow>(); Ligar(seguir, "alvo", jogadorObj.transform);
            seguir.IrParaMenu();
            var luz = new GameObject("Sol", typeof(Light)).GetComponent<Light>();
            luz.type = LightType.Directional; luz.intensity = 1.2f; luz.transform.rotation = Quaternion.Euler(45, -30, 0);
            RenderSettings.ambientLight = new Color(.65f, .7f, .8f);
            var gerador = new GameObject("Gerador").AddComponent<Generator>();
            Ligar(gerador, "config", config); Ligar(gerador, "jogador", jogadorObj.transform);
            LigarArray(gerador, "prefabsSegmento", segmento);
            LigarArray(gerador, "prefabsBarreira", carrosBaixos); LigarArray(gerador, "prefabsVagao", carrosAltos);
            Ligar(gerador, "prefabMoeda", moeda); Ligar(gerador, "prefabIma", ima);
            CriarPreview(segmento, moeda, barreira, vagao);
            var audio = new GameObject("Audio").AddComponent<AudioManager>();
            string[] sons = { "musicaMenu", "musicaJogo", "somMoeda", "somPulo", "somTrocaDeFaixa", "somBatida", "somIma", "somClique", "somRecorde" };
            for (int i = 0; i < sons.Length; i++) Ligar(audio, sons[i], CriarSom(sons[i], 220 + i * 80, i < 2 ? 4f : .18f));
            Ligar(audio, "musicaMenu", AssetDatabase.LoadAssetAtPath<AudioClip>(Raiz + "/Audio/Musicas/Musica_Menu.mp3"));
            Ligar(audio, "musicaJogo", AssetDatabase.LoadAssetAtPath<AudioClip>(Raiz + "/Audio/Musicas/Musica_Jogo.mp3"));
            var efeitos = new GameObject("Efeitos").AddComponent<EfeitosVisuais>();
            Ligar(efeitos, "brilhoMoeda", Particulas("BrilhoMoeda", amarelo.color));
            Ligar(efeitos, "brilhoIma", Particulas("BrilhoIma", azul.color));
            Ligar(efeitos, "impactoBatida", Particulas("Impacto", vermelho.color));
            var ui = CriarInterface();
            var jogo = new GameObject("Jogo").AddComponent<GameManager>();
            Ligar(jogo, "config", config); Ligar(jogo, "jogador", jogador); Ligar(jogo, "gerador", gerador);
            Ligar(jogo, "cameraDoJogo", seguir); Ligar(jogo, "interfaceDoJogo", ui); Ligar(jogo, "efeitos", efeitos);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Cena);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Cena, true) };
            PlayerSettings.productName = "Teti Corre!";
            PlayerSettings.defaultWebScreenWidth = 960; PlayerSettings.defaultWebScreenHeight = 600;
            AssetDatabase.SaveAssets();
            Debug.Log("Teti Corre: cena montada e salva. Aperte Play para jogar.");
        }

        private static AnimatorController CriarAnimacoes()
        {
            string path = Gerados + "/Teti.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine = controller.layers[0].stateMachine;
            foreach (var state in machine.states) machine.RemoveState(state.state);
            string[] nomes = { NomesDasAnimacoes.Correndo, NomesDasAnimacoes.Pulando, NomesDasAnimacoes.Morto,
                NomesDasAnimacoes.DancaBoba, NomesDasAnimacoes.DancaTwerk, NomesDasAnimacoes.DancaFeliz };
            string[] arquivos = { "Running", "Jump", "Stumble Backwards", "Silly Dancing", "Dancing Twerk", "Happy Idle" };
            for (int i = 0; i < nomes.Length; i++)
            {
                string origem = Raiz + "/Arte/Modelos/Teti/TetiFiluz@" + arquivos[i] + ".fbx";
                var clip = AssetDatabase.LoadAllAssetsAtPath(origem).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview"));
                string destino = Gerados + "/" + nomes[i] + ".anim";
                var copia = AssetDatabase.LoadAssetAtPath<AnimationClip>(destino);
                if (copia == null) { copia = UnityEngine.Object.Instantiate(clip); AssetDatabase.CreateAsset(copia, destino); }
                var settings = AnimationUtility.GetAnimationClipSettings(copia);
                settings.loopTime = i == 0 || i >= 3;
                settings.keepOriginalPositionXZ = true;
                AnimationUtility.SetAnimationClipSettings(copia, settings);
                var estado = machine.AddState(nomes[i]); estado.motion = copia;
                if (i == 0) machine.defaultState = estado;
            }
            return controller;
        }

        private static void CriarCidade(Transform pai)
        {
            const string pasta = "Assets/Pacotes/SimplePoly City - Low Poly Assets/Prefab/Buildings";
            var modelos = AssetDatabase.FindAssets("t:Prefab", new[] { pasta }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToArray();
            if (modelos.Length == 0) throw new InvalidOperationException("Prefabs de cidade não encontrados.");
            int indice = 0;
            foreach (float lado in new[] { -1f, 1f })
                for (int z = 5; z < 50; z += 10)
                {
                    var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelos[indice++ % modelos.Length]), pai);
                    AjustarModelo(obj, 7f, true);
                    obj.transform.localPosition += new Vector3(lado * 9f, 0, z);
                    obj.transform.localRotation = Quaternion.Euler(0, lado > 0 ? -90 : 90, 0);
                    foreach (var collider in obj.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
                }
            foreach (float lado in new[] { -1f, 1f })
                for (int z = 8; z < 50; z += 15)
                    ModeloImportado("Props/Props_Street Light.prefab", pai,
                        new Vector3(lado * 5.8f, 0, z), new Vector3(.8f, 4, 1));
        }

        private static GameObject ModeloImportado(string caminho, Transform pai, Vector3 posicao, Vector3 tamanho)
        {
            const string pasta = "Assets/Pacotes/SimplePoly City - Low Poly Assets/Prefab/";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(pasta + caminho);
            if (asset == null) throw new InvalidOperationException("Prefab não encontrado: " + caminho);
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(asset, pai);
            var renderers = obj.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            var escala = obj.transform.localScale;
            obj.transform.localScale = new Vector3(escala.x * tamanho.x / bounds.size.x,
                escala.y * tamanho.y / bounds.size.y, escala.z * tamanho.z / bounds.size.z);
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            obj.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) - obj.transform.position;
            obj.transform.localPosition += posicao;
            foreach (var collider in obj.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
            return obj;
        }

        private static void CriarPreview(Segmento segmento, Moeda moeda, Obstaculo barreira, Obstaculo vagao)
        {
            var preview = new GameObject("Cenário visível no Editor (preview)");
            preview.AddComponent<PreviewDaPista>();
            for (int i = 0; i < 4; i++)
            {
                var setor = (GameObject)PrefabUtility.InstantiatePrefab(segmento.gameObject, preview.transform);
                setor.name = "Setor " + (i + 1) + " — 10,5 x 50";
                setor.transform.localPosition = Vector3.forward * (i * 50);
                for (int z = 5; z < 50; z += 3)
                {
                    var obj = (GameObject)PrefabUtility.InstantiatePrefab(moeda.gameObject, setor.transform);
                    obj.transform.localPosition = new Vector3(0, .9f, z);
                }
                if (i == 0) continue;
                for (int n = 0; n < 3; n++)
                {
                    var asset = n % 2 == 0 ? barreira.gameObject : vagao.gameObject;
                    var obj = (GameObject)PrefabUtility.InstantiatePrefab(asset, setor.transform);
                    obj.transform.localPosition = new Vector3(n % 2 == 0 ? -3.5f : 3.5f, 0, 10 + n * 15);
                }
            }
        }

        // Normaliza modelos com unidades e pivôs diferentes sem alterar seus arquivos de origem.
        private static void AjustarModelo(GameObject obj, float altura, bool apoiarNoChao)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var limites = renderers[0].bounds;
            foreach (var renderer in renderers) limites.Encapsulate(renderer.bounds);
            float medida = apoiarNoChao ? limites.size.y : Mathf.Max(limites.size.x, limites.size.y, limites.size.z);
            if (medida < .001f) return;
            obj.transform.localScale *= altura / medida;
            limites = renderers[0].bounds;
            foreach (var renderer in renderers) limites.Encapsulate(renderer.bounds);
            var deslocamento = new Vector3(limites.center.x, apoiarNoChao ? limites.min.y : limites.center.y, limites.center.z) - obj.transform.position;
            obj.transform.position -= deslocamento;
        }

        private static UIManager CriarInterface()
        {
            var canvasObj = new GameObject("Interface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObj.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 600); scaler.matchWidthOrHeight = .5f;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            var ui = canvasObj.AddComponent<UIManager>();
            var menu = Painel("Menu", canvasObj.transform); Ligar(ui, "painelMenu", menu);
            Texto("Teti Corre!", menu.transform, new Vector2(0, 230), 52);
            Ligar(ui, "botaoJogar", Botao("JOGAR", menu.transform, new Vector2(0, -30)));
            Ligar(ui, "textoRecorde", Texto("Recorde", menu.transform, new Vector2(300, 130), 24));
            Ligar(ui, "textoUltimaCorrida", Texto("Última corrida", menu.transform, new Vector2(300, 30), 22));
            Ligar(ui, "seloNovoRecorde", Texto("NOVO RECORDE!", menu.transform, new Vector2(300, -60), 24).gameObject);
            Ligar(ui, "textoControles", Texto("Controles", menu.transform, new Vector2(0, -190), 20));
            var hud = Painel("HUD", canvasObj.transform); Ligar(ui, "painelJogo", hud);
            Ligar(ui, "textoDistancia", Texto("0 m", hud.transform, new Vector2(-300, 240), 30));
            Ligar(ui, "textoNota", Texto("0", hud.transform, new Vector2(0, 240), 30));
            Ligar(ui, "botaoPausa", Botao("PAUSA", hud.transform, new Vector2(320, 240)));
            var indicador = Texto("ÍMÃ", hud.transform, new Vector2(-300, 170), 22);
            Ligar(ui, "indicadorIma", indicador.gameObject);
            var barra = new GameObject("BarraIma", typeof(RectTransform), typeof(Image));
            barra.transform.SetParent(indicador.transform, false);
            Rect(barra, new Vector2(0, -40), new Vector2(150, 12));
            var imagem = barra.GetComponent<Image>(); imagem.color = Color.cyan;
            // Sprite branco para o preenchimento horizontal funcionar.
            imagem.sprite = SpriteBranco(); imagem.type = Image.Type.Filled; imagem.fillMethod = Image.FillMethod.Horizontal;
            Ligar(ui, "barraIma", imagem);
            var pausa = Painel("Pausa", canvasObj.transform); Ligar(ui, "painelPausa", pausa);
            Fundo(pausa.gameObject, new Color(.05f, .08f, .15f, .92f));
            Texto("PAUSADO", pausa.transform, new Vector2(0, 120), 42);
            Ligar(ui, "botaoContinuar", Botao("CONTINUAR", pausa.transform, Vector2.zero));
            Ligar(ui, "botaoMenu", Botao("MENU", pausa.transform, new Vector2(0, -90)));
            var som = Botao("SOM (M)", canvasObj.transform, new Vector2(320, -240));
            Ligar(ui, "botaoSom", som); Ligar(ui, "iconeSom", som.GetComponent<Image>());
            Ligar(ui, "spriteSomLigado", SpriteBranco()); Ligar(ui, "spriteSomDesligado", SpriteBranco());
            var fade = Painel("Transicao", canvasObj.transform); Fundo(fade.gameObject, Color.black); Ligar(ui, "telaPreta", fade);
            hud.gameObject.SetActive(false); pausa.gameObject.SetActive(false); fade.gameObject.SetActive(false);
            return ui;
        }

        private static CanvasGroup Painel(string nome, Transform pai)
        {
            var obj = new GameObject(nome, typeof(RectTransform), typeof(CanvasGroup)); obj.transform.SetParent(pai, false);
            var r = (RectTransform)obj.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            return obj.GetComponent<CanvasGroup>();
        }
        private static TMP_Text Texto(string valor, Transform pai, Vector2 pos, int tamanho)
        {
            var obj = new GameObject(valor, typeof(RectTransform), typeof(TextMeshProUGUI)); obj.transform.SetParent(pai, false);
            Rect(obj, pos, new Vector2(340, 100)); var t = obj.GetComponent<TextMeshProUGUI>();
            t.font = fonte; t.text = valor; t.fontSize = tamanho; t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;
            return t;
        }
        private static Button Botao(string texto, Transform pai, Vector2 pos)
        {
            var obj = new GameObject(texto, typeof(RectTransform), typeof(Image), typeof(Button)); obj.transform.SetParent(pai, false);
            Rect(obj, pos, new Vector2(210, 60)); obj.GetComponent<Image>().color = new Color(.05f, .35f, .55f);
            var button = obj.GetComponent<Button>(); button.targetGraphic = obj.GetComponent<Image>();
            var label = Texto(texto, obj.transform, Vector2.zero, 24); Rect(label.gameObject, Vector2.zero, new Vector2(200, 55));
            return button;
        }
        private static void Rect(GameObject obj, Vector2 pos, Vector2 tamanho)
        { var r = (RectTransform)obj.transform; r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.anchoredPosition = pos; r.sizeDelta = tamanho; }
        private static void Fundo(GameObject obj, Color cor) { var image = obj.AddComponent<Image>(); image.color = cor; }
        private static Sprite SpriteBranco()
        {
            string path = Gerados + "/Branco.png";
            if (!File.Exists(path))
            {
                var t = new Texture2D(2, 2); t.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white }); t.Apply();
                File.WriteAllBytes(path, t.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(t); AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite; importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static ParticleSystem Particulas(string nome, Color cor)
        {
            var p = new GameObject(nome).AddComponent<ParticleSystem>(); var main = p.main;
            main.playOnAwake = false; main.loop = false; main.startLifetime = .5f; main.startSpeed = 3; main.startSize = .12f;
            main.startColor = cor; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = p.emission; emission.rateOverTime = 0; p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var material = AssetDatabase.LoadAssetAtPath<Material>(Gerados + "/Particulas.mat");
            if (material == null) { material = new Material(Shader.Find("Particles/Standard Unlit")); AssetDatabase.CreateAsset(material, Gerados + "/Particulas.mat"); }
            p.GetComponent<ParticleSystemRenderer>().sharedMaterial = material; return p;
        }
        private static AudioClip CriarSom(string nome, float frequencia, float duracao)
        {
            string path = Gerados + "/" + nome + ".wav";
            if (!File.Exists(path))
            {
                const int taxa = 22050; int amostras = (int)(duracao * taxa);
                using (var writer = new BinaryWriter(File.Create(path)))
                {
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + amostras * 2);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1);
                    writer.Write((short)1); writer.Write(taxa); writer.Write(taxa * 2); writer.Write((short)2); writer.Write((short)16);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(amostras * 2);
                    for (int i = 0; i < amostras; i++)
                    {
                        double tempo = i / (double)taxa;
                        double envelope = duracao > 1 ? .12 : .25 * (1 - tempo / duracao);
                        double nota = duracao > 1 ? frequencia * new[] { 1d, 1.25, 1.5, 2 }[(int)(tempo * 4) % 4] : frequencia;
                        writer.Write((short)(Math.Sin(2 * Math.PI * nota * tempo) * envelope * short.MaxValue));
                    }
                }
                AssetDatabase.ImportAsset(path);
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        private static Obstaculo CriarObstaculo(string nome, Vector3 tamanho, Material mat, TipoObstaculo tipo, string veiculo)
        {
            var obj = new GameObject(nome); var obstaculo = obj.AddComponent<Obstaculo>();
            Cubo(nome, obj.transform, new Vector3(0, tamanho.y / 2, 0), tamanho, mat, true);
            UnityEngine.Object.DestroyImmediate(obj.GetComponentInChildren<MeshRenderer>());
            UnityEngine.Object.DestroyImmediate(obj.GetComponentInChildren<MeshFilter>());
            ModeloImportado("Vehicles/Vehicle with Static Wheels/" + veiculo + ".prefab", obj.transform, Vector3.zero, tamanho);
            var s = new SerializedObject(obstaculo); s.FindProperty("tipo").enumValueIndex = (int)tipo;
            s.FindProperty("comprimento").floatValue = tamanho.z; s.ApplyModifiedPropertiesWithoutUndo();
            return SalvarPrefab(obstaculo, nome);
        }
        private static void Cubo(string nome, Transform pai, Vector3 pos, Vector3 tamanho, Material mat, bool colisao = false)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = nome; obj.transform.SetParent(pai, false);
            obj.transform.localPosition = pos; obj.transform.localScale = tamanho; obj.GetComponent<Renderer>().sharedMaterial = mat;
            if (colisao) obj.GetComponent<Collider>().isTrigger = true; else UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
        }
        private static T SalvarPrefab<T>(T componente, string nome) where T : Component
        { var prefab = PrefabUtility.SaveAsPrefabAsset(componente.gameObject, Gerados + "/" + nome + ".prefab"); UnityEngine.Object.DestroyImmediate(componente.gameObject); return prefab.GetComponent<T>(); }
        private static T Asset<T>(string nome) where T : ScriptableObject
        { var asset = AssetDatabase.LoadAssetAtPath<T>(Gerados + "/" + nome); if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, Gerados + "/" + nome); } return asset; }
        private static Material Material(string nome, Color cor)
        { var mat = AssetDatabase.LoadAssetAtPath<Material>(Gerados + "/" + nome + ".mat"); if (mat == null) { mat = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(mat, Gerados + "/" + nome + ".mat"); } mat.color = cor; return mat; }
        private static void Ligar(UnityEngine.Object alvo, string campo, UnityEngine.Object valor)
        { var s = new SerializedObject(alvo); s.FindProperty(campo).objectReferenceValue = valor; s.ApplyModifiedPropertiesWithoutUndo(); }
        private static void LigarArray(UnityEngine.Object alvo, string campo, params UnityEngine.Object[] valores)
        { var s = new SerializedObject(alvo); var p = s.FindProperty(campo); p.arraySize = valores.Length; for (int i = 0; i < valores.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = valores[i]; s.ApplyModifiedPropertiesWithoutUndo(); }

        [MenuItem("InfinityRunner/Build WebGL (itch.io)")]
        public static void BuildWebGL()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL)) throw new InvalidOperationException("Instale WebGL Build Support no Unity Hub.");
            Directory.CreateDirectory("Builds");
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            var resultado = BuildPipeline.BuildPlayer(new[] { Cena }, "Builds/WebGL", BuildTarget.WebGL, BuildOptions.None);
            if (resultado.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Build WebGL falhou: veja o Console.");
            string zip = "Builds/TetiCorre-WebGL.zip";
            if (File.Exists(zip)) File.Delete(zip);
            ZipFile.CreateFromDirectory("Builds/WebGL", zip, System.IO.Compression.CompressionLevel.Optimal, false);
            Debug.Log("WebGL e ZIP prontos em Builds. TetiCorre-WebGL.zip pode ser enviado ao itch.io.");
        }
        [MenuItem("InfinityRunner/Exportar UnityPackage")]
        public static void Exportar()
        {
            Directory.CreateDirectory("Builds");
            AssetDatabase.ExportPackage(Raiz, "Builds/TetiCorre.unitypackage", ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);
            Debug.Log("Pacote pronto em Builds/TetiCorre.unitypackage.");
        }
        private static bool PrepararRecursos(Action continuar)
        {
            if (Shader.Find("TextMeshPro/Mobile/Distance Field") != null && AssetDatabase.FindAssets("t:TMP_FontAsset").Any()) return false;
            // ImportPackage é assíncrono: só montar depois do evento de conclusão.
            AssetDatabase.ImportPackageCallback callback = null;
            callback = nome =>
            {
                AssetDatabase.importPackageCompleted -= callback;
                EditorApplication.delayCall += () =>
                {
                    try { continuar(); }
                    catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
                };
            };
            AssetDatabase.importPackageCompleted += callback;
            var pacote = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_FontAsset).Assembly);
            AssetDatabase.ImportPackage(Path.Combine(pacote.resolvedPath, "Package Resources/TMP Essential Resources.unitypackage"), false);
            return true;
        }
        // Execute sem -quit: a importação dos recursos precisa terminar primeiro.
        public static void MontarEExportar()
        {
            if (PrepararRecursos(MontarEExportar)) return;
            Montar(); Exportar();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
