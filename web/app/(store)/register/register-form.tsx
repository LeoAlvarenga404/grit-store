"use client";

import { Check, Eye, EyeOff } from "lucide-react";
import { useState } from "react";
import { SubmitButton } from "@/components/submit-button";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/cn";
import { isPasswordValid, passwordRules } from "@/lib/password-rules";
import { register } from "@/lib/session/actions";

const strengthLabels = ["Muito fraca", "Fraca", "Razoável", "Boa", "Forte", "Forte"];

function RuleItem({ label, isMet }: { label: string; isMet: boolean }) {
  return (
    <li className={cn("flex items-center gap-2 text-xs transition-colors", isMet ? "text-foreground" : "text-muted-foreground")}>
      <span
        aria-hidden="true"
        className={cn("flex size-4 shrink-0 items-center justify-center rounded-full border transition-colors", isMet ? "border-foreground bg-foreground text-background" : "border-input")}
      >
        {isMet && <Check className="size-3 stroke-[3]" />}
      </span>
      {label}
      <span className="sr-only">{isMet ? ": atendido" : ": pendente"}</span>
    </li>
  );
}

export function RegisterForm() {
  const [password, setPassword] = useState("");
  const [confirmation, setConfirmation] = useState("");
  const [isShown, setIsShown] = useState(false);

  const metCount = passwordRules.filter((rule) => rule.isMet(password)).length;
  const hasConfirmation = confirmation.length > 0;
  const isConfirmed = hasConfirmation && confirmation === password;
  const type = isShown ? "text" : "password";

  return (
    <form action={register} className="space-y-6">
      <div className="space-y-2">
        <Label htmlFor="email">E-mail</Label>
        <Input id="email" name="email" type="email" autoComplete="email" placeholder="voce@email.com" required className="h-12" />
      </div>

      <div className="space-y-3">
        <div className="space-y-2">
          <Label htmlFor="password">Senha</Label>
          <div className="relative">
            <Input
              id="password"
              name="password"
              type={type}
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              autoComplete="new-password"
              maxLength={128}
              required
              aria-describedby="password-rules"
              className="h-12 pr-12"
            />
            <Button
              type="button"
              variant="ghost"
              size="icon"
              onClick={() => setIsShown((current) => !current)}
              aria-label={isShown ? "Ocultar senha" : "Mostrar senha"}
              aria-pressed={isShown}
              className="absolute top-1/2 right-1.5 size-9 -translate-y-1/2 text-muted-foreground"
            >
              {isShown ? <EyeOff aria-hidden="true" /> : <Eye aria-hidden="true" />}
            </Button>
          </div>
        </div>

        <div className="rounded-md bg-muted/60 p-4">
          <div className="flex items-center gap-3">
            <div aria-hidden="true" className="flex flex-1 gap-1">
              {passwordRules.map((rule, index) => (
                <span key={rule.key} className={cn("h-1 flex-1 rounded-full bg-border transition-colors", index < metCount && "bg-foreground")} />
              ))}
            </div>
            <span className="w-20 text-right text-xs font-medium">{password ? strengthLabels[metCount] : "Força"}</span>
          </div>
          <ul id="password-rules" className="mt-4 grid grid-cols-2 gap-x-4 gap-y-2.5">
            {passwordRules.map((rule) => (
              <RuleItem key={rule.key} label={rule.label} isMet={rule.isMet(password)} />
            ))}
          </ul>
        </div>
      </div>

      <div className="space-y-2">
        <Label htmlFor="password-confirmation">Confirmar senha</Label>
        <Input
          id="password-confirmation"
          type={type}
          value={confirmation}
          onChange={(event) => setConfirmation(event.target.value)}
          autoComplete="new-password"
          maxLength={128}
          required
          aria-describedby="password-confirmation-status"
          aria-invalid={hasConfirmation && !isConfirmed}
          className="h-12"
        />
        <p
          id="password-confirmation-status"
          aria-live="polite"
          className={cn("flex min-h-5 items-center gap-1.5 text-xs", isConfirmed ? "text-foreground" : "text-destructive")}
        >
          {isConfirmed && <Check aria-hidden="true" className="size-3.5 stroke-[3]" />}
          {hasConfirmation && (isConfirmed ? "As senhas coincidem." : "As senhas ainda não coincidem.")}
        </p>
      </div>

      <SubmitButton size="lg" className="h-12 w-full" pendingLabel="Criando conta…" disabled={!isPasswordValid(password) || !isConfirmed}>
        Criar conta
      </SubmitButton>
    </form>
  );
}
