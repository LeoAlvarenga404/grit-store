# 0014 — Identidade e tokens

- **Status:** aceita
- **Data:** 2026-10-16
- **Sub-plano:** 2a

## Contexto

O front (Next.js) precisa autenticar clientes, e os outros módulos precisam saber quem é o cliente sem conhecer o Identity. O design (§7) pede JWT curto, refresh com rotação e detecção de reuso, papéis e limite de tentativas no login.

## Decisão

- **ASP.NET Core Identity** com `IdentityUser<Guid>` no schema `identity`. As tabelas têm nome explícito: o Identity já nomeia as próprias tabelas (`AspNetUsers`), então a convenção snake_case não as alcança. E-mail único, que também é o nome de usuário. Senha de 8 a 128 caracteres, sem regras de composição (NIST SP 800-63B). O bloqueio entra após 5 falhas e dura 15 minutos.
- **Access token:** JWT HS256, 15 minutos, issuer `grit-api`, audience `grit-web`, com as claims `sub`, `email` e `role`. A chave de assinatura vem de `Identity:Jwt:SigningKey`: o AppHost gera e persiste a chave nos user secrets dele, e nada vai para o repositório.
- **Refresh token:** 32 bytes aleatórios. O banco guarda só o SHA-256. Vale 7 dias, e cada rotação emite um novo prazo de 7 dias. Cada login abre uma **família**, que se mantém nas rotações.
- **Rotação** é um `UPDATE` condicional (só troca o token ainda ativo). Se um token já substituído for apresentado, conta como **reuso** e a família inteira é revogada (401 `identity.refresh_token_reused`). Não há janela de tolerância: o BFF faz um refresh por vez por sessão.
- **Lock por família:** antes de trocar ou revogar tokens, o handler pega `pg_advisory_xact_lock` com a chave da família. Sem esse lock, a revogação de um ancestral reusado não enxerga o sucessor que uma rotação concorrente ainda não commitou, e esse sucessor sobrevive. O teste que segura o lock e insere o sucessor pega essa regressão.
- **Login e refresh não são `ICommand`:** o `TransactionBehavior` desfaz a transação quando o resultado é de falha (ADR 0005). Isso apagaria o contador de falhas do bloqueio e a revogação da família, dois efeitos de falhas que precisam ficar gravados. O login usa autocommit e o refresh abre a própria transação.
- **Limite no login:** janela fixa por IP do cliente, 10 tentativas por minuto (`Identity:LoginAttemptsPerMinute`).
- **Contrato para os outros módulos:** `ClaimsPrincipal.GetUserId()` em `Grit.BuildingBlocks` lê `sub` apenas de identidades autenticadas. Os papéis chegam como claims `role` (`customer`, `admin`).

## Consequências

- Atrás do BFF, todo login chega com o IP do servidor Next.js. A API ainda não usa `UseForwardedHeaders`, então todos os logins pelo front dividem a mesma janela de 10 por minuto. Corrigir isso exige confiar só no proxy conhecido.
- Dois logins errados simultâneos podem contar como uma falha só. O limite de tentativas restringe a perda.
- Logout por enquanto só limpa os cookies no BFF. Um endpoint que revogue a família fica para depois, junto com a política de admin (5a) e o botão de autenticação no OpenAPI.
