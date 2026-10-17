import Link from "next/link";
import { register } from "@/lib/session/actions";

const errorMessages: Record<string, string> = {
  "identity.email_taken": "Este e-mail já tem uma conta.",
  "validation.failed": "Confira o e-mail e use uma senha de 8 a 128 caracteres.",
  http_429: "Muitas tentativas. Aguarde um minuto e entre pela tela de login.",
};

export default async function RegisterPage({ searchParams }: { searchParams: Promise<{ error?: string }> }) {
  const { error } = await searchParams;

  return (
    <main className="legacy narrow">
      <h1>Criar conta</h1>
      {error && (
        <p role="alert" className="error">
          {errorMessages[error] ?? "Não foi possível criar a conta. Tente de novo."}
        </p>
      )}
      <form action={register} className="stack">
        <label>
          E-mail
          <input name="email" type="email" autoComplete="email" required />
        </label>
        <label>
          Senha
          <input
            name="password"
            type="password"
            autoComplete="new-password"
            minLength={8}
            maxLength={128}
            required
            aria-describedby="password-hint"
          />
        </label>
        <p id="password-hint" className="hint">
          De 8 a 128 caracteres.
        </p>
        <button type="submit">Criar conta</button>
      </form>
      <p>
        Já tem conta? <Link href="/login">Entre</Link>.
      </p>
    </main>
  );
}
