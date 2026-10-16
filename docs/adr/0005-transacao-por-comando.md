# 0005 — Uma transação por comando com `TransactionScope`

- **Status:** aceita
- **Data:** 2026-10-15
- **Sub-plano:** 0b

## Contexto

O design (§4) pede um pipeline behavior de transação. Cada módulo tem o próprio `DbContext`, e o behavior fica em `Grit.BuildingBlocks`, que não conhece nenhum deles. Os eventos de integração vão para o outbox do EF Core na mesma transação do agregado.

## Decisão

- `TransactionBehavior` abre um `System.Transactions.TransactionScope` (ReadCommitted, com fluxo assíncrono) só para requests marcados com `ICommand`. Queries não abrem transação.
- O escopo só é confirmado se a resposta não for um `Result` com falha. Uma exceção ou um `Result` com falha desfazem tudo: o agregado, a `OutboxMessage` e o `OutboxState`.
- Qualquer `DbContext` usado pelo handler entra na transação ambiente sem que o behavior saiba o tipo dele.

**Regras que valem para todos os sub-planos seguintes:**

- **Uma conexão por comando.** Duas conexões abertas ao mesmo tempo promovem a transação a distribuída, o que o .NET não suporta no Linux. Isso proíbe:
  - `Task.WhenAll` sobre o mesmo `DbContext`;
  - Dapper ao lado do EF dentro de um comando;
  - um leitor de outro módulo que abra a própria conexão dentro de um comando.

  Por isso o Dapper fica só em handlers de query (sub-plano 1b, ADR 0013); o teste `CommandHandlers_DoNotUseDapper` falha se um handler de `ICommand` referenciar o Dapper diretamente.
- **Sem retry:** nunca `EnableRetryOnFailure` nem o `AddNpgsqlDbContext` do Aspire, que liga retry por padrão; retry lança exceção dentro de uma transação ambiente. Os módulos usam `AddDbContext` com o `NpgsqlDataSource` compartilhado do host.
- **Consumers e sagas** do MassTransit abrem a própria transação local. Dentro deles, não se envia um `ICommand` pelo MediatR; o consumer usa o domínio e o `DbContext` do módulo direto.
- **Outbox do barramento por módulo (emenda 2026-10-16, 4b).** O `UseBusOutbox()` vincula o `IPublishEndpoint` genérico a um só `DbContext` por barramento, e o último registro vence. Um segundo módulo que publica (Ordering) não usa esse endpoint genérico: publica pelo próprio `EntityFrameworkScopedBusContextProvider<IBus, OrderingDbContext>`, que grava no outbox do contexto dele, na mesma transação do comando.
- **Sem cache de schema nas instruções de lock do outbox.** O `SqlLockStatementProvider` do MassTransit 8.5 guarda schema, tabela e colunas num dicionário estático indexado pelo tipo da entidade (`OutboxState`, `InboxState`), não pelo `DbContext`. Com dois módulos, o primeiro a resolver o tipo fixa a tabela de todos, e o outro passa a travar as linhas erradas. Por isso todo `AddEntityFrameworkOutbox<T>` usa `UsePostgres(enableSchemaCaching: false)`, e o repositório EF de saga usa `new PostgresLockStatementProvider(false)`.

## Consequências

- Os testes de integração contam as linhas da nota, da `OutboxMessage` e do `OutboxState` nos casos de sucesso, de falha e de exceção depois do `SaveChanges`, no Windows e no CI Linux.
- Os validators rodam um depois do outro, porque podem usar o mesmo `DbContext` do request.
- O `TransactionScope` confirma ou desfaz de forma síncrona no `Dispose`. Esse custo foi aceito.
