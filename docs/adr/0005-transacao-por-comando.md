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
- **Sem retry:** nunca `EnableRetryOnFailure` nem o `AddNpgsqlDbContext` do Aspire, que liga retry por padrão; retry lança exceção dentro de uma transação ambiente. Os módulos usam `AddDbContext` com o `NpgsqlDataSource` compartilhado do host.
- **Consumers e sagas** do MassTransit abrem a própria transação local. Dentro deles, não se envia um `ICommand` pelo MediatR; o consumer usa o domínio e o `DbContext` do módulo direto.
- **Outbox do barramento em um só `DbContext`.** O MassTransit permite um `DbContext` de bus outbox por barramento, e com dois o último registro vence. Copiar `UseBusOutbox()` para um segundo módulo é proibido até o sub-plano do segundo módulo que publica (4b, no máximo) provar a seleção por módulo com dois contextos.

## Consequências

- Os testes de integração contam as linhas da nota, da `OutboxMessage` e do `OutboxState` nos casos de sucesso, de falha e de exceção depois do `SaveChanges`, no Windows e no CI Linux.
- Os validators rodam um depois do outro, porque podem usar o mesmo `DbContext` do request.
- O `TransactionScope` confirma ou desfaz de forma síncrona no `Dispose`. Esse custo foi aceito.
