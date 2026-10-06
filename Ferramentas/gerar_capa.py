"""Cria a capa do itch.io a partir de uma captura real da corrida."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageOps

raiz = Path(__file__).resolve().parents[1]
captura = raiz / "InfinityRunner/Builds/Previews/Cenario.png"
saida = raiz / "Entrega/Capa.png"
saida.parent.mkdir(exist_ok=True)

# O HUD ocupa somente o topo; o recorte mantém a cidade e a Teti reais do jogo.
imagem = Image.open(captura).convert("RGB")
imagem = ImageOps.fit(imagem.crop((0, 110, imagem.width, imagem.height)), (1260, 1000), Image.Resampling.LANCZOS)
imagem = imagem.convert("RGBA")
degrade = Image.new("RGBA", imagem.size)
pincel = ImageDraw.Draw(degrade)
for y in range(350):
    pincel.line((0, y, 1260, y), fill=(12, 27, 52, int(235 * (1 - y / 350))))
imagem = Image.alpha_composite(imagem, degrade)
pincel = ImageDraw.Draw(imagem)
fonte = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 135)
subtitulo = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 38)
pincel.text((630, 45), "TETI CORRE!", font=fonte, anchor="mt", fill="#ffdc70", stroke_width=5, stroke_fill="#102a46")
pincel.text((630, 205), "TRÊS FAIXAS. UMA CORRIDA SEM FIM.", font=subtitulo, anchor="mt", fill="white", stroke_width=1, stroke_fill="#102a46")
imagem.convert("RGB").resize((630, 500), Image.Resampling.LANCZOS).save(saida, optimize=True)
print(saida)
