# Como entender e apresentar Teti Corre!

O projeto usa uma única cena. `GameManager` coordena quatro estados: menu,
jogando, pausado e morto. O jogador começa no menu; depois da batida, a animação
de queda termina e a cena volta ao menu sem recarregar.

## Os seis requisitos da atividade

| Requisito | Código em `InfinityRunner/Assets/TetiCorre/Scripts/` | Como funciona |
| --- | --- | --- |
| Correr constantemente para frente | `Jogador/PlayerMovement.cs`, `Gerenciadores/GameManager.cs` | O eixo Z aumenta com a velocidade multiplicada por `Time.deltaTime`. |
| Pular | `Jogador/PlayerMovement.cs`, `Gerenciadores/ConfiguracaoDoJogo.cs` | Velocidade vertical inicial e gravidade produzem o arco do pulo; Y volta a zero ao pousar. |
| Trocar entre três faixas | `Jogador/PlayerMovement.cs` | A faixa é limitada a -1, 0 ou 1; o jogador se aproxima suavemente do X correspondente. |
| Coletar moedas | `Coletaveis/Moeda.cs`, `Jogador/PlayerMovement.cs` | Um trigger reconhece o componente Moeda e contabiliza a coleta uma única vez. |
| Encontrar e desviar de obstáculos | `Pista/Generator.cs`, `Pista/Obstaculo.cs` | O gerador distribui obstáculos e garante uma faixa sem obstáculos por linha. |
| Morrer ao bater | `Jogador/PlayerMovement.cs`, `Gerenciadores/GameManager.cs` | O trigger do obstáculo muda o estado para morto, interrompe controles e toca a queda. |

## Controles

- A/D ou setas esquerda/direita: trocar de faixa.
- Espaço, W ou seta para cima: pular.
- S ou seta para baixo no ar: descer mais rápido.
- Esc/P: pausar ou continuar; M: alternar o som.
- Enter no menu: jogar. Arrastar com mouse ou swipe permite os mesmos movimentos.

## Por que a pista não acaba?

Cada setor da rua mede **10,5 × 50 metros**, com três faixas de 3,5 m.
Há moedas em todos os setores e no máximo três obstáculos (veículos) por setor.
Prédios e postes do pacote importado ficam nas laterais da rua.

`Generator` verifica a posição do jogador e mantém vários segmentos à frente.
`ObjectPool` guarda objetos desativados para reutilizá-los. `Segmento` devolve
seus itens quando fica para trás. Isso evita criar uma pista inteira nova a cada
partida e mantém a quantidade de segmentos limitada durante a corrida.

A velocidade aumenta por `aceleracao` até `velocidadeMaxima`. Esses valores são
configuráveis em `Assets/TetiCorre/Gerados/Configuracao.asset` no Inspector.
O padrão é começar a 10 m/s e limitar a 24 m/s. A neblina esconde a borda distante
enquanto os próximos setores já ficam preparados além do alcance da câmera.

O objeto `Cenário visível no Editor (preview)` mostra a composição antes do Play.
Ele é desativado ao jogar; a pista dinâmica assume a geração e a reciclagem.

## Extras e ajustes

O recorde e a preferência de som usam `PlayerPrefs`. O ímã atrai moedas próximas
por um tempo limitado. A interface mostra distância, pontos e duração do ímã.
`CameraFollow` alterna entre o enquadramento do menu e da corrida, e treme na
batida. `EfeitosVisuais` emite partículas ao coletar e ao bater.

Os valores de velocidade, pulo e dificuldade ficam no asset de configuração
gerado pela montagem. Os comentários estão em português para facilitar a
explicação. Use IA como apoio e experimente mudar um valor de cada vez para
entender o efeito no jogo.

## Verificação antes da entrega

Teste Play, três faixas, pulo sobre barreira, coleta, colisão, pausa, retorno ao
menu, recorde após fechar/reabrir, som e swipe. Depois repita os testes no build
WebGL servido por HTTP, incluindo trocar de aba. Importar o `.unitypackage` em
um projeto limpo verifica se todos os assets necessários foram exportados.

Compilação sem erros é necessária, mas não substitui esses testes de jogo.
