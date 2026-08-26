"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { ScrollReveal } from "@/components/scroll-reveal";

interface Topic {
  id: string;
  name: string;
  slug: string;
  isActive?: boolean;
}

// Sample data - replace with real API data
const topics: Topic[] = [
  { id: "1", name: "Yapay Zekâ", slug: "ai", isActive: true },
  { id: "2", name: "Tasarım", slug: "design" },
  { id: "3", name: "Geliştirici Araçları", slug: "developer-tools" },
  { id: "4", name: "SaaS", slug: "saas" },
];

export function TopicsSection() {
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  return (
    <section className="py-16 bg-background">
      <div className="mx-auto max-w-6xl px-4">
        {/* Header */}
        <ScrollReveal variant="fade-up" className="mb-8 space-y-2 text-center">
          <h2 className="text-sm font-semibold tracking-wider uppercase text-primary">
            İLGİ ALANLARI
          </h2>
          <h3 className="text-3xl font-bold text-foreground">
            Bir sonraki favorini seçkilerden bul.
          </h3>
        </ScrollReveal>

        {/* Topic Pills */}
        <div className="flex flex-wrap justify-center gap-3">
          {topics.map((topic, index) => (
            <ScrollReveal
              key={topic.id}
              variant="zoom-in"
              delay={(index % 4) as 0 | 1 | 2 | 3}
              duration="normal"
              threshold={0.1}
            >
              <Link
                href={`/topics/${topic.slug}`}
                className={`inline-block px-6 py-3 rounded-full font-medium transition-all duration-300 card-hover ${
                  topic.isActive
                    ? "bg-primary text-primary-foreground shadow-sm"
                    : "bg-card border border-border text-foreground hover:bg-muted hover:border-border/80"
                }`}
              >
                {topic.name}
              </Link>
            </ScrollReveal>
          ))}
        </div>
      </div>
    </section>
  );
}