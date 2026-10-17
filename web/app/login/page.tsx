import Link from "next/link";
import { logIn } from "@/lib/session/actions";

const errorMessages: Record<string, string> = {
  "identity.invalid_credentials": "E-mail ou senha incorretos.",
  "identity.locked_out": "Muitas senhas erradas. Tente de novo em 15 minutos.",
  "validation.failed": "Confira o e-mail e a senha.",
  http_429: "Muitas tentativas. Aguarde um minuto e tente de novo.",
};

export default async function LoginPage({ searchParams }: { searchParams: Promise<{ error?: string }> }) {
  const { error } = await searchParams;

  return (
    <main className="legacy narrow">
      <h1>Entrar</h1>
      {error && (
        <p role="alert" className="error">
          {errorMessages[error] ?? "Não foi possível entrar. Tente de novo."}
        </p>
      )}
      <form action={logIn} className="stack">
        <label>
          E-mail
          <input name="email" type="email" autoComplete="email" required />
        </label>
        <label>
          Senha
          <input name="password" type="password" autoComplete="current-password" required />
        </label>
        <button type="submit">Entrar</button>
      </form>
      <p>
        Não tem conta? <Link href="/register">Crie uma</Link>.
      </p>
    </main>
  );
}
