import { formatPrice } from "@/lib/format";

// Same flat rate as Ordering's Order.FlatShipping: the API adds it to every order total.
export const flatShippingAmount = 19.9;

export const flatShippingLabel = formatPrice(flatShippingAmount);

// The six store categories are fixed by the catalogue ruling (UI-STORE); their banners are named after the slug.
export const storeCategories = [
  { slug: "tenis", name: "Tênis" },
  { slug: "camisetas-e-regatas", name: "Camisetas e regatas" },
  { slug: "shorts-e-bermudas", name: "Shorts e bermudas" },
  { slug: "calcas-e-leggings", name: "Calças e leggings" },
  { slug: "moletons-e-jaquetas", name: "Moletons e jaquetas" },
  { slug: "acessorios", name: "Acessórios" },
];

export const menuShortcuts = [
  { label: "Lançamentos", href: "/catalog" },
  { label: "Masculino", href: "/catalog?attribute=genero:Masculino" },
  { label: "Feminino", href: "/catalog?attribute=genero:Feminino" },
  { label: "Tênis", href: "/catalog?category=tenis" },
];
