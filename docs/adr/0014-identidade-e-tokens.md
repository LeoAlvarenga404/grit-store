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

- **IP do cliente atrás do BFF (emenda 2026-10-16, endurecimento):** a API usa `UseForwardedHeaders` só com `X-Forwarded-For` e confia apenas em loopback e nas redes de `ForwardedHeaders:KnownNetworks`; o cabeçalho de quem chama de fora é ignorado. O BFF repassa o último salto do `x-forwarded-for` no login. Em produção, o Next.js precisa de um proxy na frente que defina esse cabeçalho, porque o Next 16 só o preenche quando ele não existe.
- Dois logins errados simultâneos podem contar como uma falha só. O limite de tentativas restringe a perda.
- **Logout (emenda 2026-10-17):** `POST /api/v1/identity/logout { refreshToken }` revoga a família do token sob o mesmo lock de família do refresh e responde 204 também para token desconhecido ou já revogado, para não servir de oráculo. O BFF chama a rota antes de limpar os cookies, sem bloquear o logout se a API falhar.
- O OpenAPI declara o esquema `Bearer`, e só as operações que exigem autorização carregam `security` (emenda 2026-10-17).
