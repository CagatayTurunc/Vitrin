"use client";

import { useEffect, useState } from "react";
import Link from "next/link";

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
        <div className={`mb-8 space-y-2 text-center ${mounted ? "animate-fade-in-up" : "opacity-0"}`}>
          <h2 className="text-sm font-semibold tracking-wider uppercase text-primary">
            İLGİ ALANLARI
          </h2>
          <h3 className="text-3xl font-bold text-foreground">
            Bir sonraki favorini seçkilerden bul.
          </h3>
        </div>

        {/* Topic Pills */}
        <div className={`flex flex-wrap justify-center gap-3 ${mounted ? "animate-fade-in-up animate-stagger-2" : "opacity-0"}`}>
          {topics.map((topic, index) => (
            <Link
              key={topic.id}
              href={`/topics/${topic.slug}`}
              className={`px-6 py-3 rounded-full font-medium transition-all duration-300 card-hover ${
                topic.isActive
                  ? "bg-primary text-primary-foreground shadow-sm"
                  : "bg-card border border-border text-foreground hover:bg-muted hover:border-border/80"
              } ${mounted ? `animate-scale-in animate-stagger-${index + 3}` : "opacity-0"}`}
            >
              {topic.name}
            </Link>
          ))}
        </div>
      </div>
    </section>
  );
}