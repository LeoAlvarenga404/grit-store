# 0022 — Identidade visual e stack do front

- **Status:** aceita
- **Data:** 2026-10-17
- **Sub-plano:** 13b

## Contexto

A UI da v1 era mínima: classes simples e uma folha de estilos própria (`legacy.css`). O M13 transforma a loja numa loja séria de roupas e calçados esportivos (ruling UI-STORE). Uma direção anterior, street art com design system próprio (ruling UI-DESIGN), foi cancelada no piloto, sem chegar ao repositório. O admin precisa parecer uma ferramenta, não a vitrine.

## Decisão

- **Cor:** preto e branco, sem cor de destaque. Os tokens ficam em `web/app/globals.css`, com os nomes das variáveis do shadcn:

  | Token | Valor |
  |---|---|
  | `background` | `#ffffff` |
  | `foreground`, `primary` | `#111111` |
  | `muted`, `secondary`, `accent` | `#f4f4f4` |
  | `muted-foreground` | `#6b6b6b` |
  | `border` | `#e5e5e5` |
  | `input` | `#d4d4d4` |
  | `radius` | `0.125rem` |

  - Vermelho (`destructive`, `#c62828`) e verde (`success`, `#2e7d32`, token além dos do shadcn) só aparecem com significado: erro, pedido confirmado ou cancelado, item indisponível.
  - Os componentes usam só tokens, nunca hex nem `gray-*`/`neutral-*`. Os únicos hex fora do `globals.css` são o dicionário nome → cor das bolinhas (`lib/color-swatches.ts`), que divide a bolinha em duas para um nome "A/B".
- **Tipografia:** Archivo variável pelo `next/font`, com o eixo de largura (`wdth`).
  - `.text-display` (largura 125%, peso 700, caixa alta, entrelinha curta) é o único estilo de destaque da marca; `.text-eyebrow` serve aos rótulos pequenos.
  - Todo preço é formatado por `formatPrice` (BRL) e usa algarismos tabulares (`tabular-nums`).
- **Acessibilidade e movimento:** contorno global de `:focus-visible` na cor da tinta; `prefers-reduced-motion` desliga as animações.
  - O contrato da UI pede rótulo em todo campo, erros com `role="alert"`, situação com `aria-live`, `aria-label` em botão só de ícone, alvos de toque de 40 px na loja e ícones decorativos com `aria-hidden`.
- **Admin:** mesmos tokens e fonte, títulos em seminegrito sem caixa alta, cards brancos sobre `bg-muted/50`, sem banners e sem o estilo de destaque, exceto no wordmark.
- **Banners:** arquivos estáticos em `web/public/banners/`, descritos em `lib/banners.ts`. Um arquivo que ainda não existe vira um espaço reservado do mesmo tamanho, até as imagens geradas com IA chegarem.
- **Stack:**
  - Next.js 16 (App Router) + React 19, com Server Components e Server Functions;
  - Tailwind v4, configurado no CSS (`@theme inline`);
  - primitivos do shadcn/ui copiados em `web/components/ui/`: `badge`, `button`, `dialog`, `input`, `label`, `native-select`, `sheet`, `skeleton`, `sonner`, `table` e `tabs`. Foram gerados uma vez e ajustados: `cn` vem de `@/lib/cn`, "Fechar" em pt-BR e o `sonner` com tema claro fixo;
  - por baixo, `radix-ui`; ícones do `lucide-react`; avisos do `sonner` 2.0.8, o único pacote novo do 13b;
  - **sem biblioteca de estado no cliente:** dados do servidor, estado na URL (filtros do catálogo, `?cor=`), `useOptimistic` na contagem da sacola e `useTransition` em "Carregar mais";
  - `select` e radios nativos, para que os formulários funcionem sem JavaScript;
  - tipos da API gerados do OpenAPI (`openapi-typescript` + `openapi-fetch`): os componentes usam `components["schemas"][…]` (com `Pick<>`), nunca tipos copiados à mão;
  - testes com `node --test` nos helpers puros, sem framework de teste de UI.
- **Rejeitado:**
  - a identidade street art do UI-DESIGN (papel, laranja e lima, fontes de grafite, adesivos): trocada por uma loja mais limpa;
  - uma biblioteca de estado no cliente (Zustand, Redux) para a sacola: o servidor e o `useOptimistic` bastam;
  - framework de teste de UI ou Playwright agora: custo maior que o valor num portfólio, e a lógica pura já está em helpers testados;
  - rodar a CLI do shadcn no repositório: ela instala um pacote npm chamado `cn` e o `next-themes`;
  - modo escuro: fora de escopo.

## Consequências

- Atualizar um primitivo do shadcn é uma cópia manual.
- O dicionário de cores precisa de nomes novos conforme o catálogo cresce. Um nome desconhecido cai na foto da cor e, sem foto, num cinza.
- O frete fixo está espelhado no front (`lib/store-navigation.ts`) e muda junto com `Order.FlatShipping`.
- Limites conhecidos:
  - o admin é só para desktop (botões pequenos);
  - as páginas 404 e de erro mostram só a barra do wordmark, sem o cabeçalho e o rodapé da loja;
  - a página do pedido se recarrega inteira a cada 2 s enquanto o pedido está `Placed` (`<meta http-equiv="refresh">`). Como `placeOrder` redireciona sem revalidar o layout, e o carrinho é esvaziado de forma assíncrona (no `OrderPlaced`), o cabeçalho mostra a contagem antiga até o primeiro recarregamento; quem sai por um link antes dele leva a contagem antiga até o próximo carregamento completo ou a próxima ação na sacola. Para um leitor de tela, cada recarregamento é uma página nova, e o `aria-live` do painel de situação não anuncia a mudança;
  - as bolinhas de cor do card têm 24 px, abaixo dos 40 px: são um atalho visual (trocam a foto), não a escolha da cor, que fica na página do produto;
  - as telas não têm teste automatizado de UI: o aceite é uma verificação no navegador, tela a tela.
