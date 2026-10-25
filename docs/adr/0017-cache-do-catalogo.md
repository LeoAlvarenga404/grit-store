# 0017 — Cache do catálogo com HybridCache

- **Status:** aceita
- **Data:** 2026-10-17
- **Sub-plano:** 5b

## Contexto

As leituras públicas do catálogo (listagem e busca, facetas, sugestões e página do produto) são as mais frequentes, e a listagem sem filtro e as facetas sem filtro varrem todos os produtos e SKUs a cada requisição (ADR 0013). As escritas são raras: só o admin muda o catálogo.

## Decisão

- **HybridCache** com L1 em memória (1 minuto) e L2 no Redis já existente (10 minutos). O módulo abre a própria conexão com o Redis, sem usar o registro do Basket. Cobre apenas leituras públicas:
  - página do produto (`catalog:product-v2:{slug}`, guardando também o produto inexistente);
  - busca (`catalog:search-page-v2:{query JSON}`, com a ordenação dentro da consulta serializada) e facetas (`catalog:facets:{query JSON}`);
  - sugestões (`catalog:suggestions:{q}`, com `q` normalizado: trim, minúsculas e sem acento).
  Estoque, carrinho, pedidos e leituras do admin nunca vão para o cache. Emenda 2026-10-17 (13a): a lista acima tem as chaves atuais. Antes do 13a elas eram `catalog:product:{slug}` e `catalog:search-page:{query JSON}`; o `catalog:search:{query}` do texto original já estava desatualizado.
- **Versão no tipo da chave (emenda 2026-10-17, 13a).** Quando o registro guardado muda de forma, o tipo da chave ganha uma versão nova (`product-v2`, `search-page-v2`), para que uma entrada deixada no Redis pela versão anterior da API nunca seja lida de volta. As facetas e as sugestões mantêm a forma e a chave.
- **Uma tag só, `catalog`.** Quase toda escrita do admin muda ao mesmo tempo a listagem, as facetas e as sugestões. Tags por produto ficam para quando as escritas forem frequentes.
- **A invalidação acontece depois do commit,** num filtro no grupo de rotas admin do Catalog. O `TransactionBehavior` faz o commit dentro do `sender.Send`, então o filtro roda depois dele. Se a invalidação ficasse no handler, ela rodaria antes do commit, e uma leitura concorrente gravaria de novo no cache a linha antiga. O filtro só invalida quando a resposta é 2xx, e não usa o token da requisição: um cliente que desconecta depois do commit não pode deixar uma entrada velha.
- **As URLs de imagem são assinadas depois do cache:** a entrada guarda só as chaves das imagens, então o prazo de 1 hora das URLs (ADR 0016) não limita o TTL. Emenda 2026-10-17 (13a): a entrada da busca guarda, por item, a chave da capa e a primeira chave de cada cor (ADR 0020), então cada item assina 1 + o número de cores URLs, contra 1 antes. O k6 precisa medir esse custo de novo contra os 43,6 ms de p95; se ele aparecer, a URL da capa pode ser reaproveitada quando a chave da capa for a primeira da primeira cor.
- **`Caching:Enabled`** (ligado por padrão) desliga o cache inteiro, para medir com o k6 com e sem ele. Os testes de integração rodam com o cache desligado, porque todos os hosts de teste compartilham o mesmo Redis. Só `CatalogReadCacheTests` liga o cache.

## Consequências

- **Emenda ao design §9.** A invalidação não vem de eventos de domínio: o Catalog não publica nenhum, e o espaço do outbox do barramento já está ocupado (ADR 0005). O output cache também ficou de fora: o HybridCache já elimina o trabalho no banco, um output cache exigiria um segundo caminho de invalidação e congelaria as URLs pré-assinadas. Isso pode ser revisto se o k6 mostrar que a serialização domina.
- Sem backplane: outra instância da API pode servir uma entrada de L1 desatualizada por até 1 minuto.
- Chaves com mais de 1024 caracteres não vão para o cache, que é o limite do HybridCache.
- Limite conhecido (já existia antes do 13a): uma leitura de produto ou de busca que perde o cache antes de uma escrita do admin e termina depois da invalidação da tag pode gravar o resultado anterior à escrita, servido por até 1 min (L1) / 10 min (L2); a correção seria uma geração de cache na chave, avançada a cada escrita confirmada.
- Se a requisição que está carregando uma entrada for abortada, as outras que esperam a mesma chave falham junto. Isso está marcado com `ponytail:` no código.

## Emenda de 2026-10-20: cache de leitura no front (Next.js)

O front passou a guardar as leituras públicas do catálogo (`lib/api/catalog-reads.ts`: busca, facetas, produto por slug e sugestões), com `unstable_cache` e a tag `catalog`. Antes, cada visita refazia as chamadas à API e recebia URLs de imagem novas, então o navegador nunca reaproveitava uma foto.

- **Janela de 10 minutos na chave.** A API assina as URLs por 1 hora (ADR 0016), e o `unstable_cache` serve uma entrada vencida enquanto recarrega em segundo plano, sem prazo máximo. Por isso a janela entra na chave: uma entrada de janela antiga nunca é lida, por mais tempo que o processo tenha ficado parado. A URL mais velha possível ainda vale cerca de 50 minutos.
- **Resposta sem dados não é guardada.** 404, 500 e filtro inválido saem do escopo do cache por exceção e voltam como `{ data: null, status }`; uma falha de rede continua subindo para a página de erro.
- **Invalidação:** as ações do admin que mudam preço ou fotos chamam `updateTag("catalog")` depois do sucesso, que expira a entrada na hora (leitura do que acabou de escrever). Estoque não entra: a loja não o exibe.
- **O que continua sem cache:** sacola, pedidos, sessão e o contador do cabeçalho. As páginas seguem renderizadas por requisição (`force-dynamic`): o `next build` não precisa da API.
- **Rejeitado:** Cache Components (`use cache`, `cacheLife` com `expire`), o modelo recomendado no Next 16. Exige tirar o `force-dynamic`, envolver em Suspense o que lê cookies e `searchParams`, e faria o build pré-renderizar a casca chamando a API, que o CI não tem. Pode ser reaberto num marco próprio. Um cache de página inteira (ISR) também ficou de fora, pelo mesmo motivo das URLs assinadas.
- **Limites conhecidos:** a invalidação do admin só vale no processo que a recebe (um deploy com várias instâncias do front precisaria de um cache compartilhado); `unstable_cache` está marcado como legado no Next 16; e a janela de 10 minutos pode mostrar um preço antigo se a mudança vier de fora do admin do front.
