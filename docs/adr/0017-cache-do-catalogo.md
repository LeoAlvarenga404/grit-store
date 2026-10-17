# 0017 — Cache do catálogo com HybridCache

- **Status:** aceita
- **Data:** 2026-10-17
- **Sub-plano:** 5b

## Contexto

As leituras públicas do catálogo (listagem e busca, facetas, sugestões e página do produto) são as mais frequentes, e a listagem sem filtro e as facetas sem filtro varrem todos os produtos e SKUs a cada requisição (ADR 0013). As escritas são raras: só o admin muda o catálogo.

## Decisão

- **HybridCache** com L1 em memória (1 minuto) e L2 no Redis já existente (10 minutos). O módulo abre a própria conexão com o Redis, sem usar o registro do Basket. Cobre apenas leituras públicas:
  - página do produto (`catalog:product:{slug}`, guardando também o produto inexistente);
  - busca (`catalog:search:{query}`) e facetas (`catalog:facets:{query}`);
  - sugestões (`catalog:suggestions:{q}`, com `q` normalizado: trim, minúsculas e sem acento).
  Estoque, carrinho, pedidos e leituras do admin nunca vão para o cache.
- **Uma tag só, `catalog`.** Quase toda escrita do admin muda ao mesmo tempo a listagem, as facetas e as sugestões. Tags por produto ficam para quando as escritas forem frequentes.
- **A invalidação acontece depois do commit,** num filtro no grupo de rotas admin do Catalog. O `TransactionBehavior` faz o commit dentro do `sender.Send`, então o filtro roda depois dele. Se a invalidação ficasse no handler, ela rodaria antes do commit, e uma leitura concorrente gravaria de novo no cache a linha antiga. O filtro só invalida quando a resposta é 2xx, e não usa o token da requisição: um cliente que desconecta depois do commit não pode deixar uma entrada velha.
- **As URLs de imagem são assinadas depois do cache:** a entrada guarda só as chaves das imagens, então o prazo de 1 hora das URLs (ADR 0016) não limita o TTL.
- **`Caching:Enabled`** (ligado por padrão) desliga o cache inteiro, para medir com o k6 com e sem ele. Os testes de integração rodam com o cache desligado, porque todos os hosts de teste compartilham o mesmo Redis. Só `CatalogReadCacheTests` liga o cache.

## Consequências

- **Emenda ao design §9.** A invalidação não vem de eventos de domínio: o Catalog não publica nenhum, e o espaço do outbox do barramento já está ocupado (ADR 0005). O output cache também ficou de fora: o HybridCache já elimina o trabalho no banco, um output cache exigiria um segundo caminho de invalidação e congelaria as URLs pré-assinadas. Isso pode ser revisto se o k6 mostrar que a serialização domina.
- Sem backplane: outra instância da API pode servir uma entrada de L1 desatualizada por até 1 minuto.
- Chaves com mais de 1024 caracteres não vão para o cache, que é o limite do HybridCache.
- Se a requisição que está carregando uma entrada for abortada, as outras que esperam a mesma chave falham junto. Isso está marcado com `ponytail:` no código.
