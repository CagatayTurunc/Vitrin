// Server component wrapper — metadata export edebilmek için.
// Etkileşimli form ContactForm client component'inde.
import type { Metadata } from "next";
import { ContactForm } from "./contact-form";

export const metadata: Metadata = {
  title: "İletişim — Vitrin",
  description:
    "Vitrin ekibiyle iletişime geçin. Soru, öneri, iş birliği ve destek için buradayız.",
  openGraph: {
    title: "İletişim — Vitrin",
    description: "Vitrin ekibiyle iletişime geçin.",
    url: "https://vitrin.it.com/contact",
  },
};

export default function ContactPage() {
  return <ContactForm />;
}
