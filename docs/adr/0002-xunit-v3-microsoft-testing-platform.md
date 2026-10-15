# 0002 — xUnit v3 sobre o Microsoft.Testing.Platform

- **Status:** aceita
- **Data:** 2026-10-15
- **Sub-plano:** 0a

## Contexto

O SDK do .NET 10 executa testes de dois jeitos: pelo VSTest, o caminho antigo, ou pelo Microsoft.Testing.Platform (MTP). O xUnit v3 tem um pacote próprio para cada caminho.

## Decisão

- Os testes usam `xunit.v3.mtp-v2` + Shouldly.
- `global.json` fixa o runner: `"test": { "runner": "Microsoft.Testing.Platform" }`.
- Os projetos de teste não referenciam `Microsoft.NET.Test.Sdk` nem `xunit.runner.visualstudio`.
- `global.json` fixa o SDK 10.0.402 com `rollForward: latestPatch`, para o CI no Ubuntu usar os mesmos analisadores e o mesmo formatador da máquina local.

## Consequências

- Os testes rodam com `dotnet test` (a solution inteira) ou `dotnet test --project <projeto>`.
- No MTP, um projeto de teste sem nenhum teste termina com código 8. Todo projeto de teste precisa ter ao menos um teste antes de entrar no CI.
- A máquina de desenvolvimento não tinha fonte NuGet configurada, então o repositório traz um `nuget.config` que limpa as fontes e aponta só para nuget.org. O restore fica igual em qualquer máquina.
