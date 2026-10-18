import { CreditCard } from "lucide-react";

// Both gateways agree on these tokens: the Fake one declines only tok_decline, and Stripe declines a payment method
// it does not know as the customer's fault.
const testCards = [
  { token: "pm_card_visa", title: "Cartão aprovado", lastDigits: "4242", text: "O pagamento é aprovado e o pedido é confirmado." },
  { token: "tok_decline", title: "Cartão recusado", lastDigits: "0002", text: "O pagamento é recusado e o pedido é cancelado." },
];

export function TestCardOptions() {
  return (
    <fieldset>
      <legend className="text-display text-xl">Pagamento</legend>
      <p className="mt-2 text-sm text-muted-foreground">Loja de portfólio: escolha um cartão de teste. Nenhuma cobrança é real.</p>
      <div className="mt-5 grid gap-3 sm:grid-cols-2">
        {testCards.map((card, index) => (
          <label
            key={card.token}
            className="flex cursor-pointer gap-3 rounded-sm border p-4 hover:border-foreground has-[:checked]:border-foreground has-[:checked]:ring-1 has-[:checked]:ring-foreground has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-ring"
          >
            <input type="radio" name="paymentToken" value={card.token} defaultChecked={index === 0} required className="mt-1 size-4 accent-foreground" />
            <span>
              <span className="flex items-center gap-2 font-semibold">
                <CreditCard aria-hidden="true" className="size-4" />
                {card.title}
              </span>
              <span className="mt-1 block font-mono text-sm tabular-nums">•••• {card.lastDigits}</span>
              <span className="mt-2 block text-xs text-muted-foreground">{card.text}</span>
            </span>
          </label>
        ))}
      </div>
    </fieldset>
  );
}
