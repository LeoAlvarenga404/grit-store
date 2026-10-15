# 0004 — Composição explícita dos módulos com `IModule`

- **Status:** aceita
- **Data:** 2026-10-15
- **Sub-plano:** 0b

## Contexto

O design (§6) diz que cada módulo expõe um `Map<Modulo>Endpoints()` e que o host só compõe. O 0b precisava provar o padrão de módulo de ponta a ponta (endpoint, MediatR, behaviors, handler, `DbContext` do módulo, outbox) sem enviar código de mentira para produção. Por isso existe um módulo **probe** que vive só em `tests/`.

Duas formas de o host achar os módulos foram consideradas:

1. Varrer `Grit.Modules.*.dll` na pasta de saída.
2. Uma lista explícita.

A varredura depende de DLLs que sobram de builds antigos e de ordem de arquivos, que muda entre Windows e Linux. Com isso, o documento OpenAPI gerado poderia mudar entre o commit e o CI.

## Decisão

- Cada módulo tem uma classe pública `<Nome>Module : IModule`, com `AddServices`, `AddMessaging` e `MapEndpoints`. O resto do módulo é `internal`.
- `Grit.Api.ModuleCatalog` guarda a lista de produção, explícita e ordenada (vazia até o Catalog chegar no 1a).
- `ModuleCatalog` acrescenta os tipos listados na configuração `Modules:Additional`. Só o host de testes usa isso, com `UseSetting`, para incluir o probe. Assim os testes usam o `Program` real.
- O host registra handlers do MediatR e validators a partir dos assemblies dos módulos.
- **Desvio da redação do design §6:** o módulo expõe as rotas por `IModule.MapEndpoints`, que chama as extensões `Map<CasoDeUso>` de cada feature, e não por uma extensão `Map<Modulo>Endpoints()`. O host percorre a lista de `IModule`.
- `IModule` terá seis implementações de produção, então não é uma interface com uma única implementação.

## Consequências

- A ordem dos módulos é determinística, e o OpenAPI gerado sai igual em qualquer máquina.
- Adicionar um módulo é uma linha em `ModuleCatalog` mais a referência de projeto. Um teste de arquitetura falha se um módulo não estiver referenciado.
- O registro de um módulo não pode lançar exceção por configuração ausente: a geração do OpenAPI no build roda `AddServices` sem configuração nem banco.
