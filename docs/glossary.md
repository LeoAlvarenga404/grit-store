# Glossário

A linguagem ubíqua do Grit Store. O termo em português é o usado na conversa e nos docs; o identificador em inglês é o usado no código.

| Termo | No código | Significado |
|---|---|---|
| Categoria | `Category` | Agrupa produtos (ex.: Tênis, Acessórios). Tem slug único. |
| Produto | `Product` | O que o cliente vê na vitrine: nome, descrição, categoria, atributos. Só fica público quando tem ao menos um SKU. |
| SKU (variante) | `Sku` | A unidade vendável de um produto: uma combinação de atributos (cor × tamanho) com preço próprio. Estoque, carrinho e pedido falam em SKU. A cor do SKU escolhe as fotos que a vitrine e o carrinho mostram. |
| Cor | `cor` / `Sku.Color` | O valor do atributo `cor` de um SKU (`Sku.ColorAttribute`), comparado ao pé da letra (o seed grava em minúsculas). As cores de um produto são as dos seus SKUs; cada uma tem até 8 fotos (ADR 0020). |
| Imagem do produto | `ProductImage` | Uma foto do produto: a chave no armazenamento e a cor que ela mostra (nula só para SKUs sem cor). Ficam em ordem; a primeira é a capa (ADR 0020). |
| Linha própria GRIT | `seed/grit/catalog.json` | As quatro categorias de roupa da loja: 12 modelos escritos à mão, com fotos geradas com IA, ao lado do seed importado do Mercado Livre, que não tem roupas (ADR 0021). |
| Slug | `Slug` | Identificador legível e imutável gerado do nome, usado na URL (ADR 0010). |
| Atributos | `Attributes` | Pares chave–valor do produto ou do SKU (marca, gênero, tamanho), em `jsonb`. O atributo do SKU vence o do produto. |
| Preço | `Money` | Valor em BRL com no máximo duas casas, sem arredondamento (ADR 0001, 0012). |
| Carrinho | `CustomerBasket` | Linhas (SKU, quantidade, preço visto) de um cliente autenticado, no Redis. |
| Snapshot de preço | `UnitPrice` na linha | O preço que o cliente viu ao adicionar; o checkout confere com o preço atual. |
| Pedido | `Order` | A compra: linhas com preço congelado, subtotal, frete, total e histórico de status. |
| Número do pedido | `Order.Number` | Identificador legível `AAAA-NNNNNN`, de uma sequência do Postgres (pode ter buracos). |
| Status do pedido | `OrderStatus` | `Placed` (criado, aguardando a saga), `Confirmed` (estoque e pagamento ok), `Cancelled` (com motivo). |
| Motivo do cancelamento | `OrderCancellationReasons` | `out_of_stock`, `payment_declined` ou `customer_request`. |
| Chave de idempotência | `Idempotency-Key` | Header do checkout: repetir a chave devolve o mesmo pedido. |
| Item de estoque | `StockItem` | Quantidade em mão de um SKU e as reservas sobre ela. |
| Reserva | `StockReservation` | Unidades presas para um pedido por 15 minutos até a confirmação ou a liberação. |
| Disponível | `Available` | Em mão menos o que está reservado (confirmado ou ainda não expirado). |
| Confirmar / liberar estoque | `CommitStock` / `ReleaseStock` | Tornar a reserva definitiva (pagamento aprovado) ou desfazê-la (recusa, cancelamento). |
| Autorização de pagamento | `AuthorizePayment` / `AuthorizationId` | Pedido ao gateway para reservar o valor; o id permite o estorno. |
| Estorno | `RefundPayment` | Devolução do valor autorizado quando o cliente cancela um pedido confirmado. |
| Gateway de pagamento | `IPaymentGateway` | A porta para o provedor de pagamento: `FakePaymentGateway` (padrão; `tok_decline` recusa) ou `StripePaymentGateway` (`Payments:Provider=Stripe`; `pm_card_visa` aprova, `pm_card_chargeDeclined` recusa). |
| Saga de checkout | `CheckoutStateMachine` | Coordena reserva, pagamento e confirmação, e compensa as falhas. |
| Compensação | — | A ação que desfaz um passo já feito quando um passo seguinte falha (liberar a reserva, estornar). |
| Outbox | `OutboxMessage` / `OutboxState` | Tabela do módulo onde a mensagem é gravada na mesma transação da escrita e de onde é entregue depois. |
| Família de tokens | `FamilyId` | Os refresh tokens que nascem de um login; o reuso de um token antigo revoga a família inteira. |
| BFF | `web/lib/session` | O servidor Next.js que guarda os tokens em cookies httpOnly e chama a API pelo navegador. |
