# Teti Corre!

Jogo de corrida infinita em 3D para a atividade de Unity: correr automaticamente,
pular, trocar entre três faixas, coletar moedas, desviar de obstáculos e morrer ao bater.

Abra a pasta `InfinityRunner/` na **Unity 2022.3.62f3**. O projeto inclui suporte
a WebGL, TextMesh Pro e a interface da Unity.

## Organização

- `InfinityRunner/Assets/UJOGOTODO/`: scripts, personagem, animações, moedas, músicas e cena do jogo.
- `InfinityRunner/Assets/Pacotes/`: SimplePoly City e Day-Night Skyboxes, com seus arquivos `.meta` originais.
- `Ferramentas/`: ferramentas auxiliares do projeto.

O trabalho das branches anteriores foi consolidado na `main`. As pastas de cache,
logs e builds são ignoradas pelo Git; os assets e seus `.meta` devem permanecer juntos.

## Jogar e configurar

Abra `Assets/UJOGOTODO/Cenas/TetiCorre.unity` e dê Play. A cena já está montada,
com a Teti dançando no menu, rua infinita, carros, prédios, moedas e áudio.
Use A/D ou setas para trocar de faixa e Espaço/W para pular. Também há swipe.

`Assets/UJOGOTODO/Gerados/Configuracao.asset` controla a velocidade máxima,
altura do salto, distâncias de renderização, sombras, curvatura e ciclo dia/noite.
Consulte `GUIA.md` para entender as mecânicas e os ajustes.

## Montagem e entrega

O menu **InfinityRunner → Montar Jogo** gera e salva uma cena com as referências
dos componentes, prefabs, interface e animações. Antes de executá-lo, salve suas
alterações: a ferramenta substitui a cena `Assets/UJOGOTODO/Cenas/TetiCorre.unity`.

Depois de montar e testar em Play:

1. **InfinityRunner → Build WebGL (itch.io)** gera `InfinityRunner/Builds/WebGL/`
   e `InfinityRunner/Builds/TetiCorre-WebGL.zip`, com `index.html` na raiz.
2. **InfinityRunner → Exportar UnityPackage** gera `InfinityRunner/Builds/TetiCorre.unitypackage`.

A cena passou por testes de integração em Play na Unity, incluindo movimento,
coleta, colisão, salto, menu e iluminação. O build WebGL também foi validado no
navegador: menu, corrida, moedas, troca de faixa, salto, retorno após colisão,
recorde após recarregar e tela cheia. O ZIP contém `index.html` na raiz e passou
pela conferência de integridade e dos limites do itch.io.
