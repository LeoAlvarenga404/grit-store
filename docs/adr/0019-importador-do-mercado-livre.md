# 0019 — Importador do Mercado Livre

- **Status:** aceita
- **Data:** 2026-10-17
- **Sub-plano:** 1c (o importador que tinha sido adiado no modo rápido); emenda 2026-10-17 no 13a

## Contexto

O seed versionado (`seed/catalog.json`) foi escrito à mão no modo rápido. O design (§12) previa gerá-lo a partir da API de catálogo do Mercado Livre, com imagens. O token OAuth é do dono do projeto, então a ferramenta precisa ser completa e testada sem ele: a execução real fica para quem tem o token.

**Emenda 2026-10-17 (13a).** A vitrine do M13 vende cada modelo em várias cores e tamanhos, com fotos por cor (ADR 0020). A primeira versão importava um SKU por produto de catálogo, sem variantes. Uma sondagem da API real (cerca de 110 chamadas, só leitura) mostrou:
- um resultado da busca é uma **folha**: uma cor e um tamanho; o `parent_id` dela lista todas as folhas cor × tamanho do modelo;
- os seletores (`pickers`) de uma folha apontam para as folhas irmãs de cor e de tamanho;
- o `buy_box_winner` veio nulo em todas as folhas amostradas, e `/products/{id}/items` responde 404 na maioria delas;
- as fotos têm só `url`;
- não há domínios de roupa (camisetas, shorts, leggings, calças, moletons e jaquetas devolvem zero resultados).

## Decisão

- **`tools/Grit.MlImporter`**, um console que lê a API de catálogo (`/products/search`, `/products/{id}`, `/products/{id}/items`). A categoria da loja vem da linha de comando (`--category "Tênis=tenis corrida@MLB-SNEAKERS"`), então os endpoints de destaques e de categorias não são usados.
- **O importador reaproveita os records e o `Validate` do seeder** (referência ao módulo Catalog com `InternalsVisibleTo`). O arquivo gerado é exatamente o que o seeder aceita, e um arquivo inválido nunca é gravado.
- **Fluxo por modelo (emenda 2026-10-17, 13a):**
  - busca por `q` + `domain_id`, 50 por página, avançando o `offset` até `paging.total` ou até o offset 500;
  - cada resultado tem a família percorrida em largura (pai, filhos e seletores), com um conjunto de ids já vistos que vale para a execução inteira e um teto de 150 produtos por família (o corte vai para o log);
  - um produto da loja por pai; folhas sem pai ficam com o modelo do resultado;
  - `--count` é o número de modelos por categoria da loja (1 a 20, padrão 10). Repetir a categoria na linha de comando dá várias fontes, tentadas na ordem até a categoria ter os modelos.
- **Preço (emenda 2026-10-17, 13a):**
  - um modelo que mostra menos de 2 cores é pulado antes de qualquer chamada de anúncios;
  - cada folha com cor é precificada sozinha: o `buy_box_winner` com preço positivo em BRL, senão o menor anúncio positivo em BRL de `/products/{id}/items`, arredondado a centavos (ADR 0001, 0012);
  - folhas sem preço são puladas, e o mínimo de 2 cores conta só as cores com preço;
  - a mesma cor × tamanho duas vezes fica com o preço menor.
- **Mapeamento (emenda 2026-10-17, 13a):**
  - nome = `family_name` do modelo; slug repetido é pulado;
  - SKUs só das combinações cor × tamanho que existem, com `cor` (em minúsculas) e `tamanho`; folhas sem cor ficam de fora;
  - atributos do produto = os que todas as folhas com preço compartilham, pela regra da ADR 0009; "Sem gênero" vira "Unissex";
  - até 3 fotos por cor, sem repetir URL, convertidas para WebP com o maior lado em até 800 px pelo SkiaSharp (MIT; o ImageSharp tem licença dividida), com o nome `{slug}-{número da cor}-{número da foto}.webp`.
- **Escopo (emenda 2026-10-17, 13a):** o importador cobre só **Tênis e Acessórios** (`MLB-SNEAKERS`, `MLB-HATS_AND_CAPS`, `MLB-TRAVEL_AND_TRAINING_BAGS`). As roupas são a linha própria GRIT (ADR 0021): uma `--category` que é categoria de `seed/grit/catalog.json` sai com código 1 antes de qualquer chamada; um modelo com o slug de um produto GRIT é pulado (log); e o catálogo importado é validado junto com a linha GRIT antes de ser gravado.
- **Resiliência (emenda 2026-10-17, 13a):**
  - 429, 500, 502, 503 e 504 são repetidos até 5 tentativas, com esperas de 5, 10, 15 e 20 s; qualquer outra falha HTTP encerra a execução;
  - uma foto que responde com status de erro, ou que não é imagem, é pulada;
  - o resultado é montado numa pasta temporária e o `SeedSwap` move para o lugar só `catalog.json` e `images/`, desfazendo os passos já feitos se um falhar; cada movimento é repetido até 5 vezes, 500 ms entre elas, em `IOException` ou `UnauthorizedAccessException`.
- **Token:** `MercadoLivre:AccessToken` nos user secrets da ferramenta, enviado só para `api.mercadolibre.com` (nunca para o CDN das fotos). A ferramenta sai com códigos claros: 1 para argumentos inválidos (inclusive uma categoria GRIT), 2 sem token e 3 para falha HTTP (o token dura 6 horas).
- **Testes sem rede:** respostas reais da API gravadas na sondagem (`real-product-MLB66991284.json`, `real-items-MLB66991284.json`) e famílias sintéticas, servidas por um `HttpMessageHandler` falso; as esperas das repetições são capturadas, não dormidas.

## Consequências

- Rodar o importador substitui só `seed/catalog.json` e `seed/images/`, e só quando a execução termina; `seed/grit/` nunca é tocado. Os testes de integração leem uma cópia congelada do seed escrito à mão, então a troca não quebra as contagens.
- O seed de Development grava direto em `products/` (sem staging): só roda na subida e só para produtos novos, então não há URL de upload para abusar (ADR 0016).
- Uma categoria pode terminar com menos modelos do que `--count`, porque poucas folhas têm anúncio; o log diz quantos vieram. A saída é aceitar menos ou passar mais fontes para a categoria.
- Custo: uma família de tênis com várias cores gasta de 60 a 100 chamadas (uma por folha, mais os anúncios de cada folha com cor). Pela estimativa da sondagem, chegar a 10 modelos leva de 10 a 25 minutos por categoria.
- Ficam para depois: respeitar o `Retry-After` (a espera crescente cobre o caso) e tratar timeouts.
