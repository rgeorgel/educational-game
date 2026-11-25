#!/usr/bin/env python3
"""Confere as traduções do jogo (frontend/js/i18n/en.js e es.js).

Cada texto da interface fica em português dentro de t("…") ou tn(n, "…", "…"); o português é a chave
dos dicionários. Este script lista:
- frases usadas no código que faltam em en.js ou es.js (aparecem em português para quem joga em outro idioma);
- traduções que sobraram (a frase mudou ou saiu do código).

Rodar:  python3 scripts/check_i18n.py          (sai com erro se faltar alguma tradução)
        python3 scripts/check_i18n.py --sobras  (também falha se houver traduções sem uso)
Só usa a biblioteca padrão do Python.
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

JS = Path(__file__).resolve().parent.parent / "frontend" / "js"
LITERAL = r'"((?:[^"\\\n]|\\.)*)"'


def used_keys() -> set[str]:
    keys: set[str] = set()
    for path in JS.rglob("*.js"):
        if path.parent.name == "i18n" or path.name == "i18n.js":
            continue
        src = path.read_text(encoding="utf-8")
        for m in re.finditer(r"\bt\(\s*" + LITERAL, src):
            keys.add(json.loads(f'"{m.group(1)}"'))
        for m in re.finditer(r"\btn\([^,]+,\s*" + LITERAL + r"\s*,\s*" + LITERAL, src):
            keys.update(json.loads(f'"{g}"') for g in m.groups())
        if path.name == "main.js":  # títulos das rotas, traduzidos pelo roteador
            for m in re.finditer(r"\{ title: " + LITERAL, src):
                keys.add(json.loads(f'"{m.group(1)}"'))
    return keys


def dictionary(code: str) -> dict[str, str]:
    entries: dict[str, str] = {}
    for line in (JS / "i18n" / f"{code}.js").read_text(encoding="utf-8").splitlines():
        m = re.fullmatch(r"\s*" + LITERAL + r":\s*" + LITERAL + r",?", line)
        if m:
            entries[json.loads(f'"{m.group(1)}"')] = json.loads(f'"{m.group(2)}"')
    return entries


def main() -> None:
    used = used_keys()
    problems = 0
    for code in ("en", "es"):
        entries = dictionary(code)
        missing = sorted(used - entries.keys())
        extra = sorted(entries.keys() - used)
        for key in missing:
            print(f"{code}.js: falta tradução de {json.dumps(key, ensure_ascii=False)}")
        if "--sobras" in sys.argv:
            for key in extra:
                print(f"{code}.js: tradução sem uso {json.dumps(key, ensure_ascii=False)}")
            problems += len(extra)
        problems += len(missing)
        print(f"{code}.js: {len(entries)} traduções, {len(missing)} faltando, {len(extra)} sem uso")
    sys.exit(1 if problems else 0)


if __name__ == "__main__":
    main()
