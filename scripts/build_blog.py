#!/usr/bin/env python3
"""Gera o blog estático em português, inglês e espanhol a partir de content/blog/.

- Português: content/blog/*.html     → frontend/blog/      (/blog)
- Inglês:    content/blog/en/*.html  → frontend/en/blog/   (/en/blog)
- Espanhol:  content/blog/es/*.html  → frontend/es/blog/   (/es/blog)

Cada post é um arquivo HTML com um cabeçalho de comentário:

    <!--
    slug: como-ensinar-tabuada
    translation-of: como-ensinar-tabuada   (só nas traduções: o slug do post original em português)
    title: Título que aparece na página e no Google
    description: Resumo de 1 ou 2 frases (aparece no Google e no WhatsApp)
    emoji: ✖️
    date: 2026-11-21
    updated: 2026-11-21        (opcional)
    related: outro-slug, mais-um-slug   (slugs do mesmo idioma)
    order: 1                   (opcional: desempata posts do mesmo dia; menor aparece antes)
    -->
    <p class="lp-lead">Primeiro parágrafo…</p>
    <h2>…</h2>

As traduções de um mesmo post ficam ligadas pelo "translation-of" (vira o hreflang de cada página).
Links "/jogar#/…" no texto ganham o idioma do post (/jogar?lang=en#/…).

Rodar:  python3 scripts/build_blog.py
Gera as páginas dos posts, a lista de cada idioma e o feed, e atualiza o sitemap.xml e o llms.txt.
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
CONTENT = ROOT / "content" / "blog"
FRONTEND = ROOT / "frontend"
SITEMAP = FRONTEND / "sitemap.xml"
LLMS = FRONTEND / "llms.txt"
SITE = "https://educa-game.georgel.ca"
AUTHOR = {"@type": "Person", "name": "Ricardo Georgel", "url": "https://georgel.ca"}


@dataclass(frozen=True)
class Lang:
    code: str
    html_lang: str
    og_locale: str
    source: Path
    out: Path
    prefix: str  # "" (português) ou "/en", "/es"
    months: list[str]
    t: dict[str, str]
    pages: dict[str, str]  # início, professores, termos, privacidade

    @property
    def blog_path(self) -> str:
        return f"{self.prefix}/blog"

    def long_date(self, d: date) -> str:
        if self.code == "en":
            return f"{self.months[d.month - 1]} {d.day}, {d.year}"
        return f"{d.day} de {self.months[d.month - 1]} de {d.year}"

    def game(self, hash_: str) -> str:
        return f"/jogar?lang={self.code}#{hash_}"


LANGS = [
    Lang(
        code="pt",
        html_lang="pt-BR",
        og_locale="pt_BR",
        source=CONTENT,
        out=FRONTEND / "blog",
        prefix="",
        months=["janeiro", "fevereiro", "março", "abril", "maio", "junho", "julho", "agosto", "setembro", "outubro", "novembro", "dezembro"],
        pages={"home": "/", "teachers": "/professores", "terms": "/termos", "privacy": "/privacidade"},
        t={
            "skip": "Pular para o conteúdo",
            "main": "Principal",
            "footer": "Rodapé",
            "home": "Início",
            "teachers": "Professores",
            "play": "Jogar",
            "play_now": "Jogar agora",
            "terms": "Termos de uso",
            "privacy": "Privacidade",
            "made_by": "Feito por",
            "language": "Idioma",
            "blog_name": "Blog do Educa Game",
            "blog_title": "Blog do Educa Game · matemática e lógica com jogos, para pais e professores",
            "blog_description": "Ideias práticas para ensinar matemática e raciocínio lógico com jogos: tabuada, sudoku, padrões, BNCC e como escolher um bom jogo educativo.",
            "blog_lead": "Ideias práticas para pais e professores ensinarem matemática e raciocínio lógico com jogos, em casa e na sala de aula.",
            "feed_description": "Ideias para pais e professores ensinarem matemática e raciocínio lógico com jogos.",
            "minutes": "min de leitura",
            "by": "Por",
            "updated": "atualizado em",
            "breadcrumb": "Você está em",
            "cta_title": "Experimente o Educa Game",
            "cta_text": "Jogo de matemática e lógica que se adapta ao nível de cada criança. Grátis, sem anúncios e sem cadastro para começar.",
            "read_also": "Leia também",
        },
    ),
    Lang(
        code="en",
        html_lang="en",
        og_locale="en_US",
        source=CONTENT / "en",
        out=FRONTEND / "en" / "blog",
        prefix="/en",
        months=["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"],
        pages={"home": "/en/", "teachers": "/en/teachers", "terms": "/en/terms", "privacy": "/en/privacy"},
        t={
            "skip": "Skip to content",
            "main": "Main",
            "footer": "Footer",
            "home": "Home",
            "teachers": "Teachers",
            "play": "Play",
            "play_now": "Play now",
            "terms": "Terms of use",
            "privacy": "Privacy",
            "made_by": "Made by",
            "language": "Language",
            "blog_name": "Educa Game Blog",
            "blog_title": "Educa Game Blog · math and logic through games, for parents and teachers",
            "blog_description": "Practical ideas for teaching math and logical reasoning with games: times tables, sudoku, patterns, curriculum and how to choose a good educational game.",
            "blog_lead": "Practical ideas for parents and teachers to teach math and logical reasoning with games, at home and in the classroom.",
            "feed_description": "Ideas for parents and teachers to teach math and logical reasoning with games.",
            "minutes": "min read",
            "by": "By",
            "updated": "updated on",
            "breadcrumb": "You are here",
            "cta_title": "Try Educa Game",
            "cta_text": "A math and logic game that adapts to each child's level. Free, no ads and no sign-up to get started.",
            "read_also": "Read also",
        },
    ),
    Lang(
        code="es",
        html_lang="es",
        og_locale="es_ES",
        source=CONTENT / "es",
        out=FRONTEND / "es" / "blog",
        prefix="/es",
        months=["enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"],
        pages={"home": "/es/", "teachers": "/es/profesores", "terms": "/es/terminos", "privacy": "/es/privacidad"},
        t={
            "skip": "Saltar al contenido",
            "main": "Principal",
            "footer": "Pie de página",
            "home": "Inicio",
            "teachers": "Profesores",
            "play": "Jugar",
            "play_now": "Jugar ahora",
            "terms": "Términos de uso",
            "privacy": "Privacidad",
            "made_by": "Hecho por",
            "language": "Idioma",
            "blog_name": "Blog de Educa Game",
            "blog_title": "Blog de Educa Game · matemáticas y lógica con juegos, para familias y profesores",
            "blog_description": "Ideas prácticas para enseñar matemáticas y razonamiento lógico con juegos: tablas de multiplicar, sudoku, patrones, currículo y cómo elegir un buen juego educativo.",
            "blog_lead": "Ideas prácticas para que familias y profesores enseñen matemáticas y razonamiento lógico con juegos, en casa y en el aula.",
            "feed_description": "Ideas para que familias y profesores enseñen matemáticas y razonamiento lógico con juegos.",
            "minutes": "min de lectura",
            "by": "Por",
            "updated": "actualizado el",
            "breadcrumb": "Estás en",
            "cta_title": "Prueba Educa Game",
            "cta_text": "Un juego de matemáticas y lógica que se adapta al nivel de cada niño. Gratis, sin anuncios y sin registro para empezar.",
            "read_also": "Lee también",
        },
    ),
]
BY_CODE = {lang.code: lang for lang in LANGS}
LANGUAGE_NAMES = {"pt": "Português", "en": "English", "es": "Español"}


@dataclass
class Post:
    lang: Lang
    slug: str
    original: str  # slug do post em português (liga as traduções)
    title: str
    description: str
    emoji: str
    published: date
    updated: date
    related: list[str]
    order: int
    body: str

    @property
    def path(self) -> str:
        return f"{self.lang.blog_path}/{self.slug}"

    @property
    def url(self) -> str:
        return f"{SITE}{self.path}"

    @property
    def minutes(self) -> int:
        words = len(re.sub(r"<[^>]+>", " ", self.body).split())
        return max(1, round(words / 200))


def parse(path: Path, lang: Lang) -> Post:
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
    if lang.code != "pt" and not meta.get("translation-of"):
        sys.exit(f"{lang.code}/{path.name}: falta \"translation-of\" (slug do post em português)")
    published = date.fromisoformat(meta["date"])
    # Links para o jogo abrem no idioma do post.
    body = match.group(2).strip().replace('href="/jogar#/', f'href="{lang.game("/")}')
    return Post(
        lang=lang,
        slug=meta["slug"],
        original=meta.get("translation-of") or meta["slug"],
        title=meta["title"],
        description=meta["description"],
        emoji=meta.get("emoji", "📘"),
        published=published,
        updated=date.fromisoformat(meta["updated"]) if meta.get("updated") else published,
        related=[s.strip() for s in meta.get("related", "").split(",") if s.strip()],
        order=int(meta.get("order", "100")),
        body=body,
    )


def ld_json(data: dict) -> str:
    # "</" quebraria o <script>; o JSON continua válido com a barra escapada.
    return json.dumps(data, ensure_ascii=False, indent=2).replace("</", "<\\/")


def alternate_links(alternates: dict[str, str]) -> str:
    """hreflang de cada idioma; o português é o x-default."""
    lines = []
    for code in ("pt", "en", "es"):
        if code in alternates:
            lines.append(f'    <link rel="alternate" hreflang="{BY_CODE[code].html_lang}" href="{SITE}{alternates[code]}" />')
    if "pt" in alternates:
        lines.append(f'    <link rel="alternate" hreflang="x-default" href="{SITE}{alternates["pt"]}" />')
    return "\n".join(lines)


def head(lang: Lang, title: str, description: str, canonical: str, og_type: str, structured: dict, alternates: dict[str, str]) -> str:
    t, d, c = html.escape(title), html.escape(description), html.escape(canonical)
    tx = lang.t
    return f"""<!doctype html>
<html lang="{lang.html_lang}" data-theme="light">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>{t}</title>
    <meta name="description" content="{d}" />
    <meta name="robots" content="index, follow, max-image-preview:large, max-snippet:-1" />
    <meta name="color-scheme" content="light" />
    <meta name="theme-color" content="#2f6fde" />
    <link rel="canonical" href="{c}" />
{alternate_links(alternates)}
    <meta property="og:type" content="{og_type}" />
    <meta property="og:site_name" content="Educa Game" />
    <meta property="og:locale" content="{lang.og_locale}" />
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
    <link rel="alternate" type="application/rss+xml" title="{html.escape(tx["blog_name"])}" href="{lang.blog_path}/feed.xml" />
    <link rel="stylesheet" href="/css/tokens.css" />
    <link rel="stylesheet" href="/css/base.css" />
    <link rel="stylesheet" href="/css/landing.css" />
    <script type="module" src="/js/landing.js"></script>
  </head>
  <body class="landing">
    <a class="skip-link" href="#conteudo">{tx["skip"]}</a>

    <header class="lp-header">
      <div class="lp-container lp-header-inner">
        <a href="{lang.pages["home"]}" class="lp-brand"><span aria-hidden="true">🧭</span> Educa Game</a>
        <nav class="lp-nav" aria-label="{tx["main"]}">
          <a href="{lang.pages["home"]}" class="lp-nav-link">{tx["home"]}</a>
          <a href="{lang.pages["teachers"]}" class="lp-nav-link">{tx["teachers"]}</a>
          <a href="{lang.blog_path}" class="lp-nav-link" aria-current="page">Blog</a>
          <a href="#idiomas" class="lp-nav-link" title="Idioma · Language · Idioma" aria-label="Idioma · Language · Idioma">🌐</a>
          <a href="{lang.game("/entrar")}" class="lp-btn lp-btn-primary lp-btn-small" data-cta="play">{tx["play_now"]}</a>
        </nav>
      </div>
    </header>
"""


def footer(lang: Lang, alternates: dict[str, str]) -> str:
    tx = lang.t
    languages = "\n".join(
        f'        <a href="{alternates.get(code, BY_CODE[code].blog_path)}" lang="{BY_CODE[code].html_lang}" hreflang="{BY_CODE[code].html_lang}"'
        f'{" aria-current=\"true\"" if code == lang.code else ""}>{LANGUAGE_NAMES[code]}</a>'
        for code in ("pt", "en", "es")
    )
    return f"""
    <footer class="lp-footer">
      <div class="lp-container lp-footer-inner">
        <span><span aria-hidden="true">🧭</span> Educa Game</span>
        <nav aria-label="{tx["footer"]}">
          <a href="{lang.pages["home"]}">{tx["home"]}</a>
          <a href="{lang.game("/entrar")}">{tx["play"]}</a>
          <a href="{lang.pages["teachers"]}">{tx["teachers"]}</a>
          <a href="{lang.blog_path}">Blog</a>
          <a href="{lang.pages["terms"]}">{tx["terms"]}</a>
          <a href="{lang.pages["privacy"]}">{tx["privacy"]}</a>
        </nav>
      </div>
      <p class="lp-container lp-langs" id="idiomas" aria-label="{tx["language"]}">
        <span aria-hidden="true">🌐</span>
{languages}
      </p>
      <p class="lp-container lp-footer-legal">{tx["made_by"]} <a href="https://georgel.ca" target="_blank" rel="noopener">Ricardo Georgel</a></p>
    </footer>
  </body>
</html>
"""


def card(post: Post, heading: str = "h2") -> str:
    return f"""          <li class="lp-post-card">
            <span class="lp-post-emoji" aria-hidden="true">{post.emoji}</span>
            <div>
              <{heading}><a href="{post.path}">{html.escape(post.title)}</a></{heading}>
              <p>{html.escape(post.description)}</p>
              <p class="lp-post-meta"><time datetime="{post.published.isoformat()}">{post.lang.long_date(post.published)}</time> · {post.minutes} {post.lang.t["minutes"]}</p>
            </div>
          </li>"""


def article_page(post: Post, by_slug: dict[str, Post], alternates: dict[str, str]) -> str:
    lang, tx = post.lang, post.lang.t
    for slug in post.related:
        if slug not in by_slug:
            sys.exit(f"{lang.code}/{post.slug}: post relacionado \"{slug}\" não existe")
    related = [by_slug[s] for s in post.related] or [p for p in by_slug.values() if p.slug != post.slug][:3]
    blog_url = f"{SITE}{lang.blog_path}"
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
                "inLanguage": lang.html_lang,
                "image": f"{SITE}/assets/og-image.png",
                "author": AUTHOR,
                "publisher": {"@type": "Organization", "name": "Educa Game", "url": f"{SITE}/", "logo": {"@type": "ImageObject", "url": f"{SITE}/assets/icon-512.png"}},
                "isPartOf": {"@type": "Blog", "@id": f"{blog_url}#blog", "name": tx["blog_name"]},
            },
            {
                "@type": "BreadcrumbList",
                "itemListElement": [
                    {"@type": "ListItem", "position": 1, "name": tx["home"], "item": f"{SITE}{lang.pages['home']}"},
                    {"@type": "ListItem", "position": 2, "name": "Blog", "item": blog_url},
                    {"@type": "ListItem", "position": 3, "name": post.title, "item": post.url},
                ],
            },
        ],
    }
    updated = (
        f' · {tx["updated"]} <time datetime="{post.updated.isoformat()}">{lang.long_date(post.updated)}</time>' if post.updated != post.published else ""
    )
    related_html = "\n".join(card(p, "h3") for p in related)
    body = "\n".join("        " + line if line.strip() else "" for line in post.body.splitlines())
    return (
        head(lang, f"{post.title} · {tx['blog_name']}", post.description, post.url, "article", structured, alternates)
        + f"""
    <main id="conteudo" class="lp-legal lp-article">
      <div class="lp-container">
        <nav class="lp-breadcrumb" aria-label="{tx["breadcrumb"]}"><a href="{lang.pages["home"]}">{tx["home"]}</a> › <a href="{lang.blog_path}">Blog</a></nav>
        <article>
        <h1>{html.escape(post.title)}</h1>
        <p class="lp-legal-meta">
          {tx["by"]} <a href="https://georgel.ca" target="_blank" rel="noopener">Ricardo Georgel</a> ·
          <time datetime="{post.published.isoformat()}">{lang.long_date(post.published)}</time>{updated} · {post.minutes} {tx["minutes"]}
        </p>
{body}
        </article>

        <aside class="lp-article-cta">
          <h2>{tx["cta_title"]}</h2>
          <p>{tx["cta_text"]}</p>
          <a href="{lang.game("/entrar")}" class="lp-btn lp-btn-primary" data-cta="play">{tx["play_now"]}</a>
        </aside>

        <section class="lp-related" aria-labelledby="leia-tambem">
          <h2 id="leia-tambem">{tx["read_also"]}</h2>
          <ul class="lp-post-list">
{related_html}
          </ul>
        </section>
      </div>
    </main>
"""
        + footer(lang, alternates)
    )


def index_page(lang: Lang, posts: list[Post]) -> str:
    tx = lang.t
    blog_url = f"{SITE}{lang.blog_path}"
    structured = {
        "@context": "https://schema.org",
        "@type": "Blog",
        "@id": f"{blog_url}#blog",
        "name": tx["blog_name"],
        "url": blog_url,
        "inLanguage": lang.html_lang,
        "description": tx["feed_description"],
        "blogPost": [{"@type": "BlogPosting", "headline": p.title, "url": p.url, "datePublished": p.published.isoformat()} for p in posts],
    }
    alternates = {code: BY_CODE[code].blog_path for code in ("pt", "en", "es")}
    cards = "\n".join(card(p) for p in posts)
    return (
        head(lang, tx["blog_title"], tx["blog_description"], blog_url, "website", structured, alternates)
        + f"""
    <main id="conteudo" class="lp-legal lp-article">
      <div class="lp-container">
        <h1>{tx["blog_name"]}</h1>
        <p class="lp-lead">{tx["blog_lead"]}</p>
        <ul class="lp-post-list">
{cards}
        </ul>
      </div>
    </main>
"""
        + footer(lang, alternates)
    )


def feed(lang: Lang, posts: list[Post]) -> str:
    tx = lang.t
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
    <title>{html.escape(tx["blog_name"])}</title>
    <link>{SITE}{lang.blog_path}</link>
    <description>{html.escape(tx["feed_description"])}</description>
    <language>{lang.html_lang}</language>
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


def load(lang: Lang) -> list[Post]:
    posts = sorted((parse(p, lang) for p in lang.source.glob("*.html")), key=lambda p: (-p.published.toordinal(), p.order, p.slug))
    if len({p.slug for p in posts}) != len(posts):
        sys.exit(f"{lang.code}: há dois posts com o mesmo slug")
    return posts


def main() -> None:
    by_lang = {lang.code: load(lang) for lang in LANGS}
    if not by_lang["pt"]:
        sys.exit("Nenhum post em content/blog/")
    originals = {p.slug for p in by_lang["pt"]}
    # Traduções de cada post original: slug em português → {idioma: caminho}.
    translations: dict[str, dict[str, str]] = {slug: {} for slug in originals}
    for code, posts in by_lang.items():
        for post in posts:
            if post.original not in originals:
                sys.exit(f"{code}/{post.slug}: translation-of \"{post.original}\" não existe em português")
            if code in translations[post.original]:
                sys.exit(f"{code}: duas traduções de \"{post.original}\"")
            translations[post.original][code] = post.path

    for lang in LANGS:
        posts = by_lang[lang.code]
        by_slug = {p.slug: p for p in posts}
        lang.out.mkdir(parents=True, exist_ok=True)
        for old in lang.out.glob("*.html"):
            old.unlink()
        for post in posts:
            (lang.out / f"{post.slug}.html").write_text(article_page(post, by_slug, translations[post.original]), encoding="utf-8")
        (lang.out / "index.html").write_text(index_page(lang, posts), encoding="utf-8")
        (lang.out / "feed.xml").write_text(feed(lang, posts), encoding="utf-8")

    urls = []
    for lang in LANGS:
        posts = by_lang[lang.code]
        if not posts:
            continue
        newest = max(p.updated for p in posts)
        urls += [(f"{SITE}{lang.blog_path}", newest, "weekly", "0.7")] + [(p.url, p.updated, "monthly", "0.6") for p in posts]
    replace_block(
        SITEMAP,
        "  <!-- blog:inicio (gerado por scripts/build_blog.py) -->",
        "  <!-- blog:fim -->",
        "".join(
            f"  <url>\n    <loc>{u}</loc>\n    <lastmod>{d.isoformat()}</lastmod>\n    <changefreq>{f}</changefreq>\n    <priority>{pr}</priority>\n  </url>\n"
            for u, d, f, pr in urls
        ),
    )
    llms = "".join(f"- [{p.title}]({p.url}): {p.description}\n" for p in by_lang["pt"])
    for code, label in (("en", "Em inglês"), ("es", "Em espanhol")):
        if by_lang[code]:
            llms += f"\n{label} ({SITE}{BY_CODE[code].blog_path}):\n\n" + "".join(f"- [{p.title}]({p.url}): {p.description}\n" for p in by_lang[code])
    replace_block(LLMS, "<!-- blog:inicio (gerado por scripts/build_blog.py) -->", "<!-- blog:fim -->", llms)
    print(", ".join(f"{len(by_lang[lang.code])} posts em {lang.out.relative_to(ROOT)}/" for lang in LANGS))


if __name__ == "__main__":
    main()
