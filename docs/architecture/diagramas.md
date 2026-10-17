# Diagramas

Visão C4 (contexto e contêineres) e a sequência da saga de checkout. Os diagramas descrevem o código da `main` em 2026-10-16.

## Contexto

```mermaid
flowchart LR
    cliente([Cliente]) -->|navega e compra| web[Grit Store Web<br/>Next.js + BFF]
    admin([Administrador]) -->|gerencia catálogo, estoque e pedidos| web
    web -->|HTTP + JWT| api[Grit Store API<br/>.NET 10]
    api -->|autoriza e estorna| gateway[[Gateway de pagamento<br/>Fake hoje, Stripe depois]]
```

## Contêineres

```mermaid
flowchart TB
    browser([Navegador]) -->|cookies httpOnly| web

    subgraph host[Aspire AppHost]
        web[web<br/>Next.js: páginas + BFF]
        subgraph apiBox[api: monólito modular]
            identity[Identity]
            catalog[Catalog]
            basket[Basket]
            ordering[Ordering + saga]
            inventory[Inventory]
            payments[Payments]
        end
        postgres[(Postgres<br/>um schema por módulo)]
        redis[(Redis<br/>carrinho)]
        storage[(SeaweedFS<br/>S3)]
    end

    web -->|HTTP + Bearer| apiBox
    identity --> postgres
    catalog --> postgres
    ordering --> postgres
    inventory --> postgres
    basket --> redis
    basket -.->|ICatalogReader| catalog
    ordering -.->|ICatalogReader| catalog
    ordering == "mensagens (MassTransit + outbox)" ==> inventory
    ordering == "mensagens" ==> payments
    ordering == "OrderPlaced" ==> basket
```

Linhas tracejadas são leituras síncronas pelo `.Contracts` do módulo dono. Linhas grossas são mensagens assíncronas, publicadas pelo outbox de cada módulo na mesma transação da escrita (ADR 0005).

## Saga de checkout

```mermaid
sequenceDiagram
    autonumber
    actor C as Cliente
    participant O as Ordering (API)
    participant S as Saga de checkout
    participant I as Inventory
    participant P as Payments
    participant B as Basket

    C->>O: POST /orders (Idempotency-Key, expectedSubtotalAmount)
    O->>O: grava Order (Placed) + OrderPlaced no outbox
    O-->>C: 201 (status Placed)
    O-)S: OrderPlaced
    O-)B: OrderPlaced (limpa o carrinho)
    S-)I: ReserveStock
    alt estoque suficiente
        I-)S: StockReserved
        S-)P: AuthorizePayment
        alt pagamento aprovado
            P-)S: PaymentAuthorized
            S-)I: CommitStock
            S->>O: Order → Confirmed
        else pagamento recusado (tok_decline)
            P-)S: PaymentDeclined
            S-)I: ReleaseStock
            S->>O: Order → Cancelled (payment_declined)
        end
    else sem estoque
        I-)S: StockReservationFailed
        S->>O: Order → Cancelled (out_of_stock)
    end

    opt cliente cancela um pedido Confirmed
        C->>O: POST /orders/{id}/cancel
        O->>O: Order → Cancelled (customer_request) + OrderCancelled no outbox
        O-)S: OrderCancelled
        S-)I: ReleaseStock
        S-)P: RefundPayment
    end
```

O cliente acompanha o pedido por `GET /orders/{id}` até `Confirmed` ou `Cancelled`. O estado da saga, a mudança do pedido e o outbox são gravados na mesma transação (ADR 0015).
