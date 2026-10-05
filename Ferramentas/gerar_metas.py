"""Cria os arquivos .meta que faltam para pastas e scripts C# dentro de Assets/TetiCorre.

A Unity cria .meta sozinha ao abrir o projeto, mas cada computador geraria um GUID
diferente. Gerando aqui (e commitando), todo mundo da equipe usa os mesmos GUIDs
e as referências entre arquivos nunca quebram.

Uso:  python3 Ferramentas/gerar_metas.py
"""
import pathlib
import uuid

RAIZ = pathlib.Path(__file__).resolve().parent.parent / "InfinityRunner" / "Assets" / "TetiCorre"

META_PASTA = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

META_SCRIPT = """fileFormatVersion: 2
guid: {guid}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def criar_meta(caminho: pathlib.Path, modelo: str) -> None:
    meta = caminho.with_name(caminho.name + ".meta")
    if meta.exists():
        return
    meta.write_text(modelo.format(guid=uuid.uuid4().hex), encoding="utf-8")
    print("criado:", meta.relative_to(RAIZ.parent))


def main() -> None:
    criar_meta(RAIZ, META_PASTA)
    for caminho in sorted(RAIZ.rglob("*")):
        if caminho.suffix == ".meta":
            continue
        if caminho.is_dir():
            criar_meta(caminho, META_PASTA)
        elif caminho.suffix == ".cs":
            criar_meta(caminho, META_SCRIPT)


if __name__ == "__main__":
    main()
