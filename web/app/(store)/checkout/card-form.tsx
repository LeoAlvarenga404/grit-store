"use client";

import { CreditCard, Wand2 } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

// Only the token travels with the order: the typed fields have no name, so the browser never sends the card. Both gateways
// (Fake and Stripe test mode) know pm_card_visa as an approved payment method, so this is the only card accepted until
// Stripe Elements can tokenize any other (P4).
const testCard = { number: "4242 4242 4242 4242", holder: "TESTE GRIT", expiry: "12/34", cvc: "123", token: "pm_card_visa" };

const digitsOf = (text: string) => text.replace(/\D/g, "");

const formatNumber = (text: string) => digitsOf(text).slice(0, 16).replace(/(\d{4})(?=\d)/g, "$1 ");

function formatExpiry(text: string) {
  const digits = digitsOf(text).slice(0, 4);
  return digits.length > 2 ? `${digits.slice(0, 2)}/${digits.slice(2)}` : digits;
}

function isExpired(expiry: string) {
  const [month, year] = expiry.split("/").map(Number);
  const now = new Date();
  return 2000 + year < now.getFullYear() || (2000 + year === now.getFullYear() && month < now.getMonth() + 1);
}

export function CardForm() {
  const [number, setNumber] = useState("");
  const [holder, setHolder] = useState("");
  const [expiry, setExpiry] = useState("");
  const [cvc, setCvc] = useState("");
  const numberRef = useRef<HTMLInputElement>(null);
  const expiryRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    const isOtherCard = number.length === testCard.number.length && number !== testCard.number;
    numberRef.current?.setCustomValidity(isOtherCard ? "Esta loja aceita só o cartão de teste. Use o botão “Usar cartão de teste”." : "");
  }, [number]);

  useEffect(() => {
    expiryRef.current?.setCustomValidity(expiry.length === 5 && isExpired(expiry) ? "Cartão vencido." : "");
  }, [expiry]);

  function fillTestCard() {
    setNumber(testCard.number);
    setHolder(testCard.holder);
    setExpiry(testCard.expiry);
    setCvc(testCard.cvc);
  }

  return (
    <fieldset>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <legend className="text-display text-xl">Pagamento</legend>
        <Button type="button" variant="outline" size="sm" onClick={fillTestCard}>
          <Wand2 aria-hidden="true" /> Usar cartão de teste
        </Button>
      </div>
      <p className="mt-2 text-sm text-muted-foreground">Loja de portfólio: nenhuma cobrança é real e só o cartão de teste é aceito.</p>
      <input type="hidden" name="paymentToken" value={number === testCard.number ? testCard.token : ""} />
      <div className="mt-5 grid gap-4 sm:grid-cols-2">
        <div className="grid gap-2 sm:col-span-2">
          <Label htmlFor="card-number">Número do cartão</Label>
          <div className="relative">
            <CreditCard aria-hidden="true" className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              id="card-number"
              ref={numberRef}
              value={number}
              onChange={(event) => setNumber(formatNumber(event.target.value))}
              inputMode="numeric"
              autoComplete="cc-number"
              placeholder="0000 0000 0000 0000"
              pattern="\d{4} \d{4} \d{4} \d{4}"
              title="16 dígitos"
              required
              className="h-11 pl-9 tabular-nums"
            />
          </div>
        </div>
        <div className="grid gap-2 sm:col-span-2">
          <Label htmlFor="card-holder">Nome impresso no cartão</Label>
          <Input
            id="card-holder"
            value={holder}
            onChange={(event) => setHolder(event.target.value.toUpperCase())}
            autoComplete="cc-name"
            placeholder="NOME COMO NO CARTÃO"
            required
            className="h-11"
          />
        </div>
        <div className="grid gap-2">
          <Label htmlFor="card-expiry">Validade</Label>
          <Input
            id="card-expiry"
            ref={expiryRef}
            value={expiry}
            onChange={(event) => setExpiry(formatExpiry(event.target.value))}
            inputMode="numeric"
            autoComplete="cc-exp"
            placeholder="MM/AA"
            pattern="(0[1-9]|1[0-2])/\d{2}"
            title="Mês e ano, como 12/34"
            required
            className="h-11 tabular-nums"
          />
        </div>
        <div className="grid gap-2">
          <Label htmlFor="card-cvc">Código de segurança</Label>
          <Input
            id="card-cvc"
            value={cvc}
            onChange={(event) => setCvc(digitsOf(event.target.value).slice(0, 3))}
            inputMode="numeric"
            autoComplete="cc-csc"
            placeholder="123"
            pattern="\d{3}"
            title="3 dígitos"
            required
            className="h-11 tabular-nums"
          />
        </div>
      </div>
    </fieldset>
  );
}
