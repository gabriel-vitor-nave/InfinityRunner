# Teti Corre!

Jogo de corrida infinita em 3D para a atividade de Unity: correr automaticamente,
pular, trocar entre três faixas, coletar moedas, desviar de obstáculos e morrer ao bater.

Abra a pasta `InfinityRunner/` na **Unity 2022.3.62f3**. O projeto inclui suporte
a WebGL, TextMesh Pro e a interface da Unity.

## Organização

- `InfinityRunner/Assets/TetiCorre/`: scripts, personagem, animações, moedas, músicas e cena do jogo.
- `InfinityRunner/Assets/Pacotes/`: SimplePoly City e Day-Night Skyboxes, com seus arquivos `.meta` originais.
- `Ferramentas/`: ferramentas auxiliares do projeto.

O trabalho das branches anteriores foi consolidado na `main`. As pastas de cache,
logs e builds são ignoradas pelo Git; os assets e seus `.meta` devem permanecer juntos.

## Montagem e entrega (em desenvolvimento)

O menu **InfinityRunner → Montar Jogo** gera e salva uma cena com as referências
dos componentes, prefabs, interface e animações. Antes de executá-lo, salve suas
alterações: a ferramenta substitui a cena `Assets/TetiCorre/Cenas/TetiCorre.unity`.

Depois de montar e testar em Play:

1. **InfinityRunner → Build WebGL (itch.io)** gera `InfinityRunner/Builds/WebGL/`.
2. Compacte o conteúdo dessa pasta, com `index.html` na raiz do ZIP.
3. **InfinityRunner → Exportar UnityPackage** gera `InfinityRunner/Builds/TetiCorre.unitypackage`.

A montagem ainda precisa de validação visual e de jogabilidade. A existência dos
scripts e menus não significa que a versão final ou o build WebGL já estejam aprovados.
