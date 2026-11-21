# Plano de monetização

Como o Educa Game pode se sustentar sem deixar de ser um jogo grátis e seguro para crianças.
Os preços e prazos são **hipóteses para testar** com professores e famílias reais antes de fixar.

## Princípios

1. **O jogo das crianças é sempre grátis e sem anúncios.** Quem paga são os adultos: famílias, professores e escolas.
2. **Nada de anúncios, moedas, caixas surpresa ou "pague para passar de fase".**
   - No Brasil, publicidade dirigida a crianças é considerada abusiva (Resolução 163 do Conanda).
   - A LGPD exige cuidado extra com dados de crianças.
   - A política "Famílias" da Google Play restringe anúncios e compras em apps infantis.
3. **Confiança é o maior ativo de divulgação.** A landing promete "grátis, sem anúncios e sem compras"; isso continua valendo para quem joga.
4. **Pagar libera acompanhamento e conveniência para o adulto**, nunca conteúdo de aprendizagem para a criança.

## Planos

| | Grátis | **Professor Pro** | **Família** | Escola |
|---|---|---|---|---|
| Para quem | Todo mundo | Professor, com o próprio dinheiro | Pais e responsáveis | Escolas e redes |
| Preço (hipótese) | R$ 0 | R$ 19,90/mês | R$ 9,90/mês (ou R$ 79–99/ano) | Sob consulta (R$ 15–30 por aluno/ano) |
| Jogar (todo o conteúdo) | ✓ | ✓ | ✓ | ✓ |
| Perfis por conta | 3 | 3 | **8** | 8 |
| Turmas | 1 | **20** | 1 | 300 |
| Tarefas em aberto por turma | 2 | **50** | 2 | 50 |
| Relatório da turma por habilidade (BNCC) | ✓ | ✓ | ✓ | ✓ |
| Planilhas da turma (Excel/Google Planilhas) | — | **✓** | — | ✓ |
| Fichas para imprimir com gabarito | — | **✓** | **✓** | ✓ |
| Relatório da semana (com a semana anterior) | — | — | **✓** | ✓ |
| Treino de reforço escolhido pelo responsável | — | — | **✓** | ✓ |
| Como contrata | — | Escolhe no site | Escolhe no site | Contrato e nota fiscal |

- **Planos somados:** quem tem dois planos (um professor que também é pai, por exemplo) fica com o melhor de cada recurso.
- **Escola por enquanto:** é concedida à mão, com o comando `plano`. O painel do coordenador ainda não existe.

## Ordem de lançamento

1. **Agora: Professor Pro e Família, sem pagamento.**
   - A pessoa escolhe o plano na página **Planos** (`/jogar#/planos`) e ele já vale.
   - A página diz "grátis durante o lançamento" e avisa que a cobrança será anunciada antes.
   - Serve para medir o interesse real: quantos escolhem cada plano e quantos usam os recursos.
2. **Pagamento com Stripe (implementado; liga ao configurar as chaves).**
   - O Stripe Checkout cria as assinaturas e os webhooks mantêm tudo em dia.
   - O portal do Stripe cuida de cartão, recibos e cancelamento.
   - Ao ligar: avise quem escolheu plano na fase 1. Use `plano encerrar-escolhas 30` para dar 30 dias, e considere um cupom de fundador (o Checkout aceita códigos promocionais).
3. **Escola:** painel do coordenador (todas as turmas da escola), contrato e nota fiscal. Começar por escolas particulares pequenas e médias; redes públicas compram por licitação e demoram.
4. **Receitas complementares, a qualquer momento:**
   - botão "Apoie o projeto" (Pix, Apoia.se ou Catarse);
   - cadernos de exercícios em PDF por série;
   - patrocínio institucional (fundações e editais), sempre visível só para os adultos.

## Como está implementado

- **Planos e recursos:** `backend/src/EduGame.Domain/Billing/Plans.cs`.
- **Assinaturas:** tabela `subscriptions`. Cada assinatura tem plano, início, fim, `source` e `external_id`.
  - `source` diz de onde veio: `escolha` (site), `manual` (comando) ou `stripe`.
  - `external_id` vai guardar o id da assinatura no Stripe.
- **Limites desligados por padrão:**
  - Com `PLANS_ENFORCE=false` (padrão), ninguém é limitado: todos têm os recursos do Professor Pro e da Família. Escolher um plano fica só registrado.
  - Com `PLANS_ENFORCE=true` e Redeploy, cada conta tem só o que os seus planos liberam. Um recurso bloqueado mostra "faz parte do plano X" com o link para Planos.
  - **Recomendação:** ligar depois de algumas semanas, quando houver gente usando, e avisar antes.
- **Stripe:** `POST /api/billing/checkout`, `POST /api/billing/portal`, `POST /api/billing/webhook` e `GET /api/billing/status`.
  - O código fica em `backend/src/EduGame.Api/Features/Billing/`.
  - O cliente do Stripe fica em `users.stripe_customer_id`.
- **API:**
  - `GET /api/plans` (pública) lista os planos.
  - `GET /api/me/plan` mostra o plano da conta.
  - `PUT /api/me/plan` faz a escolha. Só para contas, convidado não pode.
- **Comando de terminal:** `dotnet EduGame.Api.dll plano listar|conceder|remover` (veja o README).
- **Painel admin:** a visão geral mostra quantas contas há em cada plano e de onde veio (escolha, manual ou pago); o detalhe do usuário mostra as assinaturas.

### Recursos do Professor Pro

- **Tarefas da turma:** com estrelas mínimas, prazo e recado. A criança joga a etapa mesmo bloqueada no mapa.
- **Relatório por habilidade com a BNCC** e **planilhas** de alunos e habilidades.
- **Fichas para imprimir:** `/jogar#/fichas`.

### Recursos do plano Família

- **Até 8 perfis.**
- **Relatório da semana** (`/jogar#/semana/{perfil}`):
  - dias com prática, problemas, acertos, minutos e XP, comparados com a semana anterior;
  - problemas por dia;
  - o que praticou e quanto evoluiu;
  - o que pede atenção, com a BNCC;
  - medalhas da semana e dicas.
- **Treino de reforço:**
  - o responsável escolhe uma habilidade no relatório da semana (até 3 em aberto por perfil);
  - ela aparece na tela inicial da criança, com 8 problemas daquela habilidade;
  - conta como concluído com 2 estrelas.
- **Fichas para imprimir:**
  - problemas novos a cada ficha, com dificuldade fácil, média ou difícil e gabarito em outra página;
  - a mesma semente gera a mesma ficha;
  - frações para pintar ficam de fora, porque só funcionam na tela.

## Antes de ligar a cobrança

- **Pagamento (código pronto):** criar a conta no Stripe, colocar a chave e rodar `dotnet EduGame.Api.dll stripe configurar`. Ele cria produtos, preços, webhook e portal (veja "Pagamentos (Stripe)" no README).
- **Empresa e documentos:**
  - CNPJ (MEI ou ME) e emissão de nota fiscal; um contador ajuda;
  - termos de uso e política de privacidade: prontos em `/termos` e `/privacidade` (LGPD, dados de crianças, cancelamento, arrependimento de 7 dias e reembolso). Falta preencher nome/CPF ou CNPJ e e-mail de contato, e uma revisão de advogado.
- **Comunicação:** o envio de e-mails já existe (SMTP; boas-vindas, recuperação de senha, senha alterada). Falta configurar o provedor em produção e criar os avisos de cobrança, de fim do teste grátis e, depois, o relatório semanal por e-mail.
- **Saída do lançamento:** avisar quem escolheu planos na fase de lançamento, com prazo e preço de fundador.

## O que acompanhar

- **Interesse:** contas que escolheram cada plano (painel admin, card de planos).
- **Uso dos recursos pagos:**
  - tarefas criadas;
  - planilhas baixadas;
  - fichas geradas;
  - relatórios da semana abertos;
  - treinos de reforço criados e concluídos.
- **Retenção:** pessoas que voltam depois de 7 e de 30 dias. Professores com turma ativa e alunos por turma.
- **Canais:** de onde vêm as pessoas (card "De onde vêm as pessoas").
- **Depois do Stripe:** conversão de quem escolheu para quem paga, receita por mês e cancelamentos.
