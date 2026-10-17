# 0007 — Concorrência otimista por agregado com `xmin`

- **Status:** aceita
- **Data:** 2026-10-15
- **Sub-plano:** 1a

## Contexto

Dois administradores podem editar o mesmo produto ao mesmo tempo. O produto é um agregado (produto + SKUs): uma mudança só num SKU também precisa ser protegida, senão uma escrita velha sobrescreve a outra sem aviso.

## Decisão

- **Uma versão por agregado:** só a tabela `products` mapeia a coluna de sistema `xmin` do PostgreSQL, como propriedade sombra `uint Version` (`IsRowVersion()`). Não há coluna de versão própria.
- **Filhos atualizam a raiz:** o `AuditInterceptor` carimba `updated_at` do produto quando um SKU é adicionado ou alterado. A linha do produto é atualizada, o `xmin` muda e o `UPDATE … WHERE xmin = @versão` protege o agregado inteiro.
- **A versão viaja no corpo JSON**, não em `ETag`/`If-Match`: o cliente envia a `version` que leu (na criação, no GET ou na resposta da escrita anterior). Assim ela fica tipada no OpenAPI e nos tipos do web.
- `CatalogDbContext.SaveProductAsync` usa essa versão como valor original do `xmin` e transforma `DbUpdateConcurrencyException` no erro `catalog.product_changed` (409, com `code`). Como o handler sempre carrega o produto atual no próprio request, uma versão velha marca a raiz como modificada mesmo que os valores enviados sejam iguais aos atuais, e a escrita é rejeitada.
- Uma `version` ausente (0) é erro de formato: 400 `validation.failed`. O `xmin` nunca vale 0.
- **Auditoria:** `created_at`/`updated_at` (`timestamptz`) só nas raízes (`products`, `categories`). "Criado/alterado por" (emenda 2026-10-17): `created_by`/`updated_by` (`uuid`, nulos) nas mesmas raízes. O interceptor lê o usuário com `GetUserId()` do `HttpContext` atual; escritas fora de uma requisição, como o seed, ficam com nulo.

## Consequências

- Nenhuma escrita num produto passa sem a versão lida; os testes cobrem a corrida real (A lê, B grava, A grava um SKU com a versão antiga) e a escrita velha com valores iguais aos atuais.
- Uma remoção de SKU (ainda inexistente) também terá de tocar a raiz; o interceptor só olha filhos adicionados ou modificados.
- Limite conhecido: o `xid` dá a volta depois de cerca de 4 bilhões de transações; uma versão guardada por tanto tempo poderia coincidir com a atual.
