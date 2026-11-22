#!/usr/bin/env python3
"""Avisa Bing, Yandex, Seznam, Naver e outros (via IndexNow) que páginas do site mudaram.

A chave fica em frontend/<chave>.txt (o arquivo contém só a própria chave) e precisa estar
no ar em https://educa-game.georgel.ca/<chave>.txt antes do envio; por isso rode depois do deploy.

Rodar:
    python3 scripts/indexnow.py                         # todas as URLs do sitemap.xml
    python3 scripts/indexnow.py --desde 2026-11-21      # só as com <lastmod> a partir dessa data
    python3 scripts/indexnow.py /blog/como-ensinar-tabuada /professores   # URLs escolhidas
    python3 scripts/indexnow.py --simular               # mostra o que seria enviado, sem enviar

Só usa a biblioteca padrão do Python.
"""

from __future__ import annotations

import argparse
import json
import sys
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
from datetime import date
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
FRONTEND = ROOT / "frontend"
SITEMAP = FRONTEND / "sitemap.xml"
SITE = "https://educa-game.georgel.ca"
HOST = "educa-game.georgel.ca"
ENDPOINT = "https://api.indexnow.org/indexnow"
NS = {"s": "http://www.sitemaps.org/schemas/sitemap/0.9"}
# O Cloudflare bloqueia (403) o User-Agent padrão do Python ("Python-urllib/3.x").
USER_AGENT = "educa-game-indexnow/1.0"

RESPOSTAS = {
    200: "OK: URLs recebidas.",
    202: "Aceito: a chave ainda vai ser validada.",
    400: "Formato inválido.",
    403: "Chave inválida (arquivo da chave não encontrado no site ou com outro conteúdo).",
    422: "URLs não pertencem ao host ou a chave não confere.",
    429: "Muitos envios (possível spam). Tente mais tarde.",
}


def ler_chave() -> str:
    arquivos = [p for p in FRONTEND.glob("*.txt") if len(p.stem) == 32 and p.read_text().strip() == p.stem]
    if len(arquivos) != 1:
        sys.exit(f"Esperava exatamente 1 arquivo de chave IndexNow em frontend/, achei {len(arquivos)}.")
    return arquivos[0].stem


def urls_do_sitemap(desde: date | None) -> list[str]:
    urls = []
    for url in ET.parse(SITEMAP).getroot().findall("s:url", NS):
        loc = url.findtext("s:loc", namespaces=NS).strip()
        lastmod = url.findtext("s:lastmod", namespaces=NS)
        if desde and lastmod and date.fromisoformat(lastmod.strip()[:10]) < desde:
            continue
        urls.append(loc)
    return urls


def normalizar(url: str) -> str:
    if url.startswith("/"):
        url = SITE + url
    if not url.startswith(SITE):
        sys.exit(f"URL fora do site: {url}")
    return url


def chave_no_ar(chave: str) -> str | None:
    """Devolve None se a chave está no ar; senão, o motivo."""
    req = urllib.request.Request(f"{SITE}/{chave}.txt", headers={"User-Agent": USER_AGENT})
    try:
        with urllib.request.urlopen(req, timeout=15) as r:
            if r.read().decode().strip() == chave:
                return None
            return "o endereço respondeu, mas com outro conteúdo (deploy ainda não terminou?)"
    except urllib.error.HTTPError as e:
        return f"HTTP {e.code}"
    except urllib.error.URLError as e:
        return str(e.reason)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("urls", nargs="*", help="URLs (ou caminhos como /blog/x) para enviar; sem nada, usa o sitemap")
    parser.add_argument("--desde", type=date.fromisoformat, help="só URLs do sitemap com lastmod >= AAAA-MM-DD")
    parser.add_argument("--simular", action="store_true", help="mostra o que seria enviado, sem enviar")
    args = parser.parse_args()

    chave = ler_chave()
    urls = [normalizar(u) for u in args.urls] if args.urls else urls_do_sitemap(args.desde)
    if not urls:
        sys.exit("Nenhuma URL para enviar.")

    corpo = {"host": HOST, "key": chave, "keyLocation": f"{SITE}/{chave}.txt", "urlList": urls}
    print(f"{len(urls)} URL(s):")
    for u in urls:
        print(f"  {u}")
    if args.simular:
        return

    motivo = chave_no_ar(chave)
    if motivo:
        sys.exit(f"A chave não está no ar em {SITE}/{chave}.txt ({motivo}). Faça o deploy antes de enviar.")

    req = urllib.request.Request(
        ENDPOINT,
        data=json.dumps(corpo).encode(),
        headers={"Content-Type": "application/json; charset=utf-8", "User-Agent": USER_AGENT},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=30) as r:
            status = r.status
    except urllib.error.HTTPError as e:
        status = e.code
    print(f"IndexNow respondeu {status}: {RESPOSTAS.get(status, 'resposta inesperada.')}")
    if status not in (200, 202):
        sys.exit(1)


if __name__ == "__main__":
    main()
