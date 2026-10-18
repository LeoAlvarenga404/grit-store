// Banner slots of the store. Images live in web/public/banners/ under these exact names (briefing:
// .claude/plans/2026-10-17-banner-briefing.md); a missing file renders a sized placeholder.
export type Banner = {
  title: string;
  subtitle?: string;
  eyebrow?: string;
  href: string;
  cta: string;
  alt: string;
  desktopImage: string;
  mobileImage?: string;
  widthInPixels: number;
  heightInPixels: number;
};

export const heroBanners: Banner[] = [
  {
    eyebrow: "Nova coleção",
    title: "Feito pra quem não para",
    subtitle: "Roupa de treino e tênis de corrida para o dia inteiro em movimento.",
    href: "/catalog",
    cta: "Ver lançamentos",
    alt: "Dupla correndo pela cidade ao amanhecer",
    desktopImage: "home-hero-1-desktop.webp",
    mobileImage: "home-hero-1-mobile.webp",
    widthInPixels: 1920,
    heightInPixels: 800,
  },
  {
    eyebrow: "Tênis",
    title: "Cada passada conta",
    subtitle: "Amortecimento, leveza e cores para todo tipo de corrida.",
    href: "/catalog?category=tenis",
    cta: "Comprar tênis",
    alt: "Tênis de corrida em movimento no asfalto",
    desktopImage: "home-hero-2-desktop.webp",
    mobileImage: "home-hero-2-mobile.webp",
    widthInPixels: 1920,
    heightInPixels: 800,
  },
  {
    eyebrow: "Feminino",
    title: "Treino sem limite",
    subtitle: "Leggings, tops e regatas que acompanham o seu ritmo.",
    href: "/catalog?attribute=genero:Feminino",
    cta: "Ver feminino",
    alt: "Mulher em treino funcional de legging e top",
    desktopImage: "home-hero-3-desktop.webp",
    mobileImage: "home-hero-3-mobile.webp",
    widthInPixels: 1920,
    heightInPixels: 800,
  },
];

export const promoBanners: Banner[] = [
  {
    eyebrow: "Masculino",
    title: "Do treino à rua",
    href: "/catalog?attribute=genero:Masculino",
    cta: "Comprar",
    alt: "Homem de moletom e calça de treino em fundo de concreto",
    desktopImage: "promo-masculino.webp",
    widthInPixels: 1200,
    heightInPixels: 600,
  },
  {
    eyebrow: "Feminino",
    title: "Leve como o vento",
    href: "/catalog?attribute=genero:Feminino",
    cta: "Comprar",
    alt: "Mulher de jaqueta corta-vento em estúdio claro",
    desktopImage: "promo-feminino.webp",
    widthInPixels: 1200,
    heightInPixels: 600,
  },
];

export const categoryCardImage = (slug: string) => `category-${slug}.webp`;

// Top banner of a store category in the catalog (briefing: 1920×400 desktop, 1080×540 mobile).
export const catalogBanner = (slug: string) => ({
  desktopImage: `catalog-${slug}-desktop.webp`,
  mobileImage: `catalog-${slug}-mobile.webp`,
});

export const authBanner = {
  file: "auth.webp",
  alt: "Atleta em pausa durante o treino",
  widthInPixels: 960,
  heightInPixels: 1200,
};
