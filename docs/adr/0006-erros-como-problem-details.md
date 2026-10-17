# 0006 — Erros como ProblemDetails

- **Status:** aceita
- **Data:** 2026-10-15
- **Sub-plano:** 0b

## Contexto

O design (§5) pede que o endpoint converta `Error` em ProblemDetails (RFC 9457) com o status certo, e que um `IExceptionHandler` global trate o resto.

## Decisão

- **Status por `ErrorType`:**

  | `ErrorType` | Status HTTP |
  |---|---|
  | `Validation` | 400 |
  | `Unauthorized` | 401 |
  | `Forbidden` | 403 |
  | `NotFound` | 404 |
  | `Conflict` | 409 |
  | `Failure` | 500 |

  `Failure` é o tipo genérico: o servidor não conseguiu atender um pedido válido. Responder 400 mandaria o cliente corrigir um pedido que está certo. Uma falha causada pelo cliente usa um tipo específico.
- **Formato:** `detail` traz a mensagem do erro, a extensão `code` traz o código (`money.amount_too_large`) e a extensão `errors` traz os erros por campo, quando existem. `traceId` vem do framework.
- **Validação:** erros de formato do FluentValidation viram um `Result` com falha (`validation.failed`), não uma exceção. As chaves de `errors` seguem o camelCase do JSON em cada segmento (`text`, `address.zipCode`).
- **Exceções não tratadas** usam o `UseExceptionHandler` nativo com `AddProblemDetails`. JSON malformado vira 400 e o resto vira 500, sem detalhes da exceção. `UseStatusCodePages` faz 404 e 405 também saírem como ProblemDetails.
- **Desvio do design §5:** não há uma classe `IExceptionHandler` própria. O middleware nativo já registra o log e escreve o RFC 9457, então a classe seria código sem comportamento próprio. Se ela fizer falta, é um arquivo de umas 30 linhas.

## Consequências

- O front lê sempre o formato RFC 9457. A extensão `code` só existe nos erros de negócio e de validação (os que vêm de um `Error`). Exceções não tratadas, JSON malformado e 404/405 de rota saem sem `code`, então o front trata `code` como opcional.
- O `/health` responde 200 ou 503 com `{ status }` e aparece no OpenAPI, então os tipos gerados do front incluem esse endpoint.
- **Schema no OpenAPI (emenda 2026-10-17):** um transformador do documento declara `code` e `errors` (opcionais) no schema `ProblemDetails`, então os tipos gerados do front leem o código do erro sem cast.
