#!/usr/bin/env python3
"""Gera o blog estático (frontend/blog/) a partir de content/blog/*.html.

Cada post é um arquivo HTML com um cabeçalho de comentário:

    <!--
    slug: como-ensinar-tabuada
    title: Título que aparece na página e no Google
    description: Resumo de 1 ou 2 frases (aparece no Google e no WhatsApp)
    emoji: ✖️
    date: 2026-11-21
    updated: 2026-11-21        (opcional)
    related: outro-slug, mais-um-slug
    order: 1                   (opcional: desempata posts do mesmo dia; menor aparece antes)
    -->
    <p class="lp-lead">Primeiro parágrafo…</p>
    <h2>…</h2>

Rodar:  python3 scripts/build_blog.py
Gera as páginas dos posts, a lista em /blog e atualiza o sitemap.xml e o llms.txt.
Só usa a biblioteca padrão do Python.
"""

from __future__ import annotations

import html
import json
import re
import sys
from dataclasses import dataclass
from datetime import date
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT / "content" / "blog"
OUT = ROOT / "frontend" / "blog"
SITEMAP = ROOT / "frontend" / "sitemap.xml"
LLMS = ROOT / "frontend" / "llms.txt"
SITE = "https://educa-game.georgel.ca"
AUTHOR = {"@type": "Person", "name": "Ricardo Georgel", "url": "https://georgel.ca"}
MONTHS = ["janeiro", "fevereiro", "março", "abril", "maio", "junho", "julho", "agosto", "setembro", "outubro", "novembro", "dezembro"]


@dataclass
class Post:
    slug: str
    title: str
    description: str
    emoji: str
    published: date
    updated: date
    related: list[str]
    order: int
    body: str

    @property
    def url(self) -> str:
        return f"{SITE}/blog/{self.slug}"

    @property
    def minutes(self) -> int:
        words = len(re.sub(r"<[^>]+>", " ", self.body).split())
        return max(1, round(words / 200))


def long_date(d: date) -> str:
    return f"{d.day} de {MONTHS[d.month - 1]} de {d.year}"


def parse(path: Path) -> Post:
    text = path.read_text(encoding="utf-8")
    match = re.match(r"\s*<!--(.*?)-->\s*(.*)", text, re.S)
    if not match:
        sys.exit(f"{path.name}: falta o cabeçalho <!-- slug: … -->")
    meta = {}
    for line in match.group(1).strip().splitlines():
        key, _, value = line.partition(":")
        meta[key.strip()] = value.strip()
    for key in ("slug", "title", "description", "date"):
        if not meta.get(key):
            sys.exit(f"{path.name}: falta \"{key}\" no cabeçalho")
    if not re.fullmatch(r"[a-z0-9]+(-[a-z0-9]+)*", meta["slug"]):
        sys.exit(f"{path.name}: slug inválido (use letras minúsculas, números e hífens)")
    published = date.fromisoformat(meta["date"])
    return Post(
        slug=meta["slug"],
        title=meta["title"],
        description=meta["description"],
        emoji=meta.get("emoji", "📘"),
        published=published,
        updated=date.fromisoformat(meta["updated"]) if meta.get("updated") else published,
        related=[s.strip() for s in meta.get("related", "").split(",") if s.strip()],
        order=int(meta.get("order", "100")),
        body=match.group(2).strip(),
    )


def ld_json(data: dict) -> str:
    # "</" quebraria o <script>; o JSON continua válido com a barra escapada.
    return json.dumps(data, ensure_ascii=False, indent=2).replace("</", "<\\/")


def head(title: str, description: str, canonical: str, og_type: str, structured: dict) -> str:
    t, d, c = html.escape(title), html.escape(description), html.escape(canonical)
    return f"""<!doctype html>
<html lang="pt-BR" data-theme="light">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>{t}</title>
    <meta name="description" content="{d}" />
    <meta name="robots" content="index, follow, max-image-preview:large, max-snippet:-1" />
    <meta name="color-scheme" content="light" />
    <meta name="theme-color" content="#2f6fde" />
    <link rel="canonical" href="{c}" />
    <meta property="og:type" content="{og_type}" />
    <meta property="og:site_name" content="Educa Game" />
    <meta property="og:locale" content="pt_BR" />
    <meta property="og:url" content="{c}" />
    <meta property="og:title" content="{t}" />
    <meta property="og:description" content="{d}" />
    <meta property="og:image" content="{SITE}/assets/og-image.png" />
    <meta property="og:image:width" content="1200" />
    <meta property="og:image:height" content="630" />
    <meta name="twitter:card" content="summary_large_image" />
    <meta name="twitter:title" content="{t}" />
    <meta name="twitter:description" content="{d}" />
    <meta name="twitter:image" content="{SITE}/assets/og-image.png" />
    <script type="application/ld+json">
{ld_json(structured)}
    </script>
    <link rel="icon" href="/assets/icon.svg" type="image/svg+xml" />
    <link rel="alternate" type="application/rss+xml" title="Blog do Educa Game" href="/blog/feed.xml" />
    <link rel="stylesheet" href="/css/tokens.css" />
    <link rel="stylesheet" href="/css/base.css" />
    <link rel="stylesheet" href="/css/landing.css" />
    <script type="module" src="/js/landing.js"></script>
  </head>
  <body class="landing">
    <a class="skip-link" href="#conteudo">Pular para o conteúdo</a>

    <header class="lp-header">
      <div class="lp-container lp-header-inner">
        <a href="/" class="lp-brand"><span aria-hidden="true">🧭</span> Educa Game</a>
        <nav class="lp-nav" aria-label="Principal">
          <a href="/" class="lp-nav-link">Início</a>
          <a href="/professores" class="lp-nav-link">Professores</a>
          <a href="/blog" class="lp-nav-link" aria-current="page">Blog</a>
          <a href="/jogar#/entrar" class="lp-btn lp-btn-primary lp-btn-small" data-cta="play">Jogar agora</a>
        </nav>
      </div>
    </header>
"""


FOOTER = """
    <footer class="lp-footer">
      <div class="lp-container lp-footer-inner">
        <span><span aria-hidden="true">🧭</span> Educa Game</span>
        <nav aria-label="Rodapé">
          <a href="/">Início</a>
          <a href="/jogar#/entrar">Jogar</a>
          <a href="/professores">Professores</a>
          <a href="/blog">Blog</a>
          <a href="/termos">Termos de uso</a>
          <a href="/privacidade">Privacidade</a>
        </nav>
      </div>
      <p class="lp-container lp-footer-legal">Feito por <a href="https://georgel.ca" target="_blank" rel="noopener">Ricardo Georgel</a></p>
    </footer>
  </body>
</html>
"""


def card(post: Post, heading: str = "h2") -> str:
    return f"""          <li class="lp-post-card">
            <span class="lp-post-emoji" aria-hidden="true">{post.emoji}</span>
            <div>
              <{heading}><a href="/blog/{post.slug}">{html.escape(post.title)}</a></{heading}>
              <p>{html.escape(post.description)}</p>
              <p class="lp-post-meta"><time datetime="{post.published.isoformat()}">{long_date(post.published)}</time> · {post.minutes} min de leitura</p>
            </div>
          </li>"""


def article_page(post: Post, by_slug: dict[str, Post]) -> str:
    for slug in post.related:
        if slug not in by_slug:
            sys.exit(f"{post.slug}: post relacionado \"{slug}\" não existe")
    related = [by_slug[s] for s in post.related] or [p for p in by_slug.values() if p.slug != post.slug][:3]
    structured = {
        "@context": "https://schema.org",
        "@graph": [
            {
                "@type": "BlogPosting",
                "@id": f"{post.url}#post",
                "headline": post.title,
                "description": post.description,
                "url": post.url,
                "mainEntityOfPage": post.url,
                "datePublished": post.published.isoformat(),
                "dateModified": post.updated.isoformat(),
                "inLanguage": "pt-BR",
                "image": f"{SITE}/assets/og-image.png",
                "author": AUTHOR,
                "publisher": {"@type": "Organization", "name": "Educa Game", "url": f"{SITE}/", "logo": {"@type": "ImageObject", "url": f"{SITE}/assets/icon-512.png"}},
                "isPartOf": {"@type": "Blog", "@id": f"{SITE}/blog#blog", "name": "Blog do Educa Game"},
            },
            {
                "@type": "BreadcrumbList",
                "itemListElement": [
                    {"@type": "ListItem", "position": 1, "name": "Início", "item": f"{SITE}/"},
                    {"@type": "ListItem", "position": 2, "name": "Blog", "item": f"{SITE}/blog"},
                    {"@type": "ListItem", "position": 3, "name": post.title, "item": post.url},
                ],
            },
        ],
    }
    updated = (
        f' · atualizado em <time datetime="{post.updated.isoformat()}">{long_date(post.updated)}</time>' if post.updated != post.published else ""
    )
    related_html = "\n".join(card(p, "h3") for p in related)
    body = "\n".join("        " + line if line.strip() else "" for line in post.body.splitlines())
    return (
        head(f"{post.title} · Blog do Educa Game", post.description, post.url, "article", structured)
        + f"""
    <main id="conteudo" class="lp-legal lp-article">
      <div class="lp-container">
        <nav class="lp-breadcrumb" aria-label="Você está em"><a href="/">Início</a> › <a href="/blog">Blog</a></nav>
        <article>
        <h1>{html.escape(post.title)}</h1>
        <p class="lp-legal-meta">
          Por <a href="https://georgel.ca" target="_blank" rel="noopener">Ricardo Georgel</a> ·
          <time datetime="{post.published.isoformat()}">{long_date(post.published)}</time>{updated} · {post.minutes} min de leitura
        </p>
{body}
        </article>

        <aside class="lp-article-cta">
          <h2>Experimente o Educa Game</h2>
          <p>Jogo de matemática e lógica que se adapta ao nível de cada criança. Grátis, sem anúncios e sem cadastro para começar.</p>
          <a href="/jogar#/entrar" class="lp-btn lp-btn-primary" data-cta="play">Jogar agora</a>
        </aside>

        <section class="lp-related" aria-labelledby="leia-tambem">
          <h2 id="leia-tambem">Leia também</h2>
          <ul class="lp-post-list">
{related_html}
          </ul>
        </section>
      </div>
    </main>
"""
        + FOOTER
    )


def index_page(posts: list[Post]) -> str:
    structured = {
        "@context": "https://schema.org",
        "@type": "Blog",
        "@id": f"{SITE}/blog#blog",
        "name": "Blog do Educa Game",
        "url": f"{SITE}/blog",
        "inLanguage": "pt-BR",
        "description": "Ideias para pais e professores ensinarem matemática e raciocínio lógico com jogos.",
        "blogPost": [{"@type": "BlogPosting", "headline": p.title, "url": p.url, "datePublished": p.published.isoformat()} for p in posts],
    }
    cards = "\n".join(card(p) for p in posts)
    return (
        head(
            "Blog do Educa Game · matemática e lógica com jogos, para pais e professores",
            "Ideias práticas para ensinar matemática e raciocínio lógico com jogos: tabuada, sudoku, padrões, BNCC e como escolher um bom jogo educativo.",
            f"{SITE}/blog",
            "website",
            structured,
        )
        + f"""
    <main id="conteudo" class="lp-legal lp-article">
      <div class="lp-container">
        <h1>Blog do Educa Game</h1>
        <p class="lp-lead">Ideias práticas para pais e professores ensinarem matemática e raciocínio lógico com jogos, em casa e na sala de aula.</p>
        <ul class="lp-post-list">
{cards}
        </ul>
      </div>
    </main>
"""
        + FOOTER
    )


def feed(posts: list[Post]) -> str:
    items = "\n".join(
        f"""    <item>
      <title>{html.escape(p.title)}</title>
      <link>{p.url}</link>
      <guid>{p.url}</guid>
      <pubDate>{p.published.strftime("%a, %d %b %Y")} 12:00:00 +0000</pubDate>
      <description>{html.escape(p.description)}</description>
    </item>"""
        for p in posts
    )
    return f"""<?xml version="1.0" encoding="UTF-8"?>
<rss version="2.0">
  <channel>
    <title>Blog do Educa Game</title>
    <link>{SITE}/blog</link>
    <description>Ideias para pais e professores ensinarem matemática e raciocínio lógico com jogos.</description>
    <language>pt-BR</language>
{items}
  </channel>
</rss>
"""


def replace_block(path: Path, start: str, end: str, content: str) -> None:
    text = path.read_text(encoding="utf-8")
    pattern = re.compile(re.escape(start) + r".*?" + re.escape(end), re.S)
    if not pattern.search(text):
        sys.exit(f"{path.name}: marcadores {start} … {end} não encontrados")
    path.write_text(pattern.sub(lambda _: f"{start}\n{content}{end}", text), encoding="utf-8")


def main() -> None:
    posts = sorted((parse(p) for p in SOURCE.glob("*.html")), key=lambda p: (-p.published.toordinal(), p.order, p.slug))
    if not posts:
        sys.exit("Nenhum post em content/blog/")
    by_slug = {p.slug: p for p in posts}
    if len(by_slug) != len(posts):
        sys.exit("Há dois posts com o mesmo slug")

    OUT.mkdir(exist_ok=True)
    for old in OUT.glob("*.html"):
        old.unlink()
    for post in posts:
        (OUT / f"{post.slug}.html").write_text(article_page(post, by_slug), encoding="utf-8")
    (OUT / "index.html").write_text(index_page(posts), encoding="utf-8")
    (OUT / "feed.xml").write_text(feed(posts), encoding="utf-8")

    newest = max(p.updated for p in posts)
    urls = [(f"{SITE}/blog", newest, "weekly", "0.7")] + [(p.url, p.updated, "monthly", "0.6") for p in posts]
    replace_block(
        SITEMAP,
        "  <!-- blog:inicio (gerado por scripts/build_blog.py) -->",
        "  <!-- blog:fim -->",
        "".join(
            f"  <url>\n    <loc>{u}</loc>\n    <lastmod>{d.isoformat()}</lastmod>\n    <changefreq>{f}</changefreq>\n    <priority>{pr}</priority>\n  </url>\n"
            for u, d, f, pr in urls
        ),
    )
    replace_block(
        LLMS,
        "<!-- blog:inicio (gerado por scripts/build_blog.py) -->",
        "<!-- blog:fim -->",
        "".join(f"- [{p.title}]({p.url}): {p.description}\n" for p in posts),
    )
    print(f"{len(posts)} posts gerados em frontend/blog/")


if __name__ == "__main__":
    main()
