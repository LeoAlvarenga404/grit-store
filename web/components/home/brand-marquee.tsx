const phrases = ["Feito pra quem não para", "Treino", "Corrida", "Força", "Ritmo"];

function Line({ isHidden = false }: { isHidden?: boolean }) {
  return (
    <ul aria-hidden={isHidden || undefined} className="flex shrink-0 items-center gap-10 pr-10">
      {phrases.map((phrase, index) => (
        <li key={phrase} className="flex items-center gap-10">
          <span
            className={
              index % 2 === 0
                ? "text-display text-3xl whitespace-nowrap md:text-5xl"
                : "text-display text-3xl whitespace-nowrap text-transparent [-webkit-text-stroke:1px_var(--background)] md:text-5xl"
            }
          >
            {phrase}
          </span>
          <span aria-hidden="true" className="size-2 shrink-0 rounded-full bg-background" />
        </li>
      ))}
    </ul>
  );
}

// Two identical lines side by side: the track moves one line to the left and loops with no jump.
export function BrandMarquee() {
  return (
    <section aria-label="Lema da Grit" className="overflow-hidden bg-foreground py-6 text-background">
      <div className="marquee flex w-max">
        <Line />
        <Line isHidden />
      </div>
    </section>
  );
}
