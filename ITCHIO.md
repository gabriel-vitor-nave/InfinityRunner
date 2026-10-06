# Entrega de Teti Corre! no itch.io

## Texto da página

**Teti Corre!**

Corra pela cidade, salte sobre carros baixos e troque entre três faixas para pegar
moedas. Um ímã ajuda na coleta; a velocidade aumenta conforme você avança.
Até onde você consegue chegar?

Projeto acadêmico desenvolvido na Unity com apoio de inteligência artificial.

**Controles:** A/D ou setas para trocar de faixa, Espaço/W para pular, S para
descer rápido. Também aceita swipe
ou arrasto com o mouse. Enter inicia a corrida no menu.

O recorde é salvo no navegador usado para jogar. Limpar os dados do site pode
apagar o recorde.

## Arquivos de entrega

- Build WebGL: gere pelo menu `InfinityRunner → Build WebGL (itch.io)`.
- Pacote Unity: gere pelo menu `InfinityRunner → Exportar UnityPackage`.
- `GUIA.md`: explicação das mecânicas para a apresentação.

## Upload

O menu de build já gera `Builds/TetiCorre-WebGL.zip`, com `index.html` na raiz.
Crie um projeto do tipo **HTML Game** no itch.io, envie esse ZIP e marque
o arquivo como jogável no navegador. Para incorporar na página, use **960 × 640**
e mantenha **Click to Play** ligado; também pode usar lançamento em tela cheia.
A documentação oficial explica o formato e
as opções de incorporação: [Uploading HTML5 games](https://itch.io/docs/creators/html5).

A versão atual foi testada no navegador por HTTP, incluindo menu, corrida,
coleta, troca de faixa, salto, colisão, retorno ao menu, tela cheia e persistência
do recorde após recarregar. Confira a prévia do itch.io antes de publicar.
O `.unitypackage` é um arquivo separado do ZIP jogável.

## Créditos a conferir

O projeto inclui Teti, animações, moeda, SimplePoly City, Day-Night Skyboxes e
duas músicas fornecidas no repositório. Preserve os créditos e termos de cada
origem. A presença desses arquivos no projeto não comprova permissão de
redistribuição; não atribua licença CC0 a eles sem consultar a origem.
