"use client";

import { useEffect, useRef, useState, useCallback } from "react";
import { Trophy, Zap, Star, TrendingUp, ArrowUp, Layers, Sparkles } from "lucide-react";

interface MousePos { x: number; y: number; }

const PRODUCT_CARDS = [
  { id: 1, name: "Metafor AI",      tagline: "Türkçe metin üretici",       votes: 312, color: "from-emerald-400/20 to-teal-400/20",   border: "border-emerald-500/30", icon: <Zap       className="w-4 h-4 text-emerald-500" />, delay: "0s",   x: "right-[-20px]", y: "top-[8%]",    z: 40 },
  { id: 2, name: "Kreatif Studio",  tagline: "No-code tasarım aracı",      votes: 189, color: "from-violet-400/20 to-purple-400/20",  border: "border-violet-500/30",  icon: <Star      className="w-4 h-4 text-violet-500" />,  delay: "0.4s", x: "right-[40px]",  y: "top-[42%]",   z: 30 },
  { id: 3, name: "Stok Radar",      tagline: "E-ticaret analiz platformu", votes: 97,  color: "from-sky-400/20 to-blue-400/20",       border: "border-sky-500/30",     icon: <TrendingUp className="w-4 h-4 text-sky-500" />,    delay: "0.8s", x: "right-[10px]",  y: "bottom-[10%]", z: 20 },
];

const FLOATING_BADGES = [
  { label: "Günün Ürünü", icon: <Trophy    className="w-3 h-3" />, color: "text-amber-600 bg-amber-50 dark:bg-amber-900/30 dark:text-amber-400 border-amber-200 dark:border-amber-800",     delay: "0s",   x: "left-[5%]",  y: "top-[18%]"    },
  { label: "+48 oy",      icon: <ArrowUp   className="w-3 h-3" />, color: "text-emerald-700 bg-emerald-50 dark:bg-emerald-900/30 dark:text-emerald-400 border-emerald-200 dark:border-emerald-800", delay: "1.2s", x: "left-[0%]",  y: "bottom-[30%]" },
  { label: "Trending",    icon: <Sparkles  className="w-3 h-3" />, color: "text-violet-700 bg-violet-50 dark:bg-violet-900/30 dark:text-violet-400 border-violet-200 dark:border-violet-800",    delay: "0.6s", x: "left-[20%]", y: "bottom-[8%]"  },
];

export function HeroShowcase() {
  const containerRef = useRef<HTMLDivElement>(null);
  const [mouse, setMouse] = useState<MousePos>({ x: 0.5, y: 0.5 });
  const [isHovered, setIsHovered] = useState(false);
  const targetMouse = useRef<MousePos>({ x: 0.5, y: 0.5 });
  const currentMouse = useRef<MousePos>({ x: 0.5, y: 0.5 });
  const rafRef = useRef<number | null>(null);

  const animate = useCallback(() => {
    const lerp = (a: number, b: number, t: number) => a + (b - a) * t;
    currentMouse.current.x = lerp(currentMouse.current.x, targetMouse.current.x, 0.08);
    currentMouse.current.y = lerp(currentMouse.current.y, targetMouse.current.y, 0.08);
    setMouse({ ...currentMouse.current });
    rafRef.current = requestAnimationFrame(animate);
  }, []);

  useEffect(() => {
    rafRef.current = requestAnimationFrame(animate);
    return () => { if (rafRef.current) cancelAnimationFrame(rafRef.current); };
  }, [animate]);

  const handleMouseMove = useCallback((e: React.MouseEvent<HTMLDivElement>) => {
    const rect = containerRef.current?.getBoundingClientRect();
    if (!rect) return;
    targetMouse.current = { x: (e.clientX - rect.left) / rect.width, y: (e.clientY - rect.top) / rect.height };
  }, []);

  const px = (depth: number) => `${(mouse.x - 0.5) * depth}px`;
  const py = (depth: number) => `${(mouse.y - 0.5) * depth}px`;

  return (
    <div ref={containerRef} className="relative w-full h-full min-h-[340px] select-none"
      onMouseMove={handleMouseMove} onMouseEnter={() => setIsHovered(true)} onMouseLeave={() => setIsHovered(false)}>

      {/* Spotlight */}
      <div className="absolute inset-0 pointer-events-none rounded-2xl overflow-hidden transition-opacity duration-500" style={{ opacity: isHovered ? 1 : 0 }}>
        <div className="absolute w-[400px] h-[400px] rounded-full pointer-events-none"
          style={{ background: "radial-gradient(circle, rgba(0,142,99,0.12) 0%, transparent 70%)", left: `${mouse.x * 100}%`, top: `${mouse.y * 100}%`, transform: "translate(-50%, -50%)" }} />
      </div>

      {/* Ambient glow */}
      <div className="absolute inset-0 pointer-events-none">
        <div className="absolute top-[20%] right-[20%] w-48 h-48 bg-primary/5 rounded-full blur-3xl animate-pulse" style={{ animationDuration: "4s" }} />
        <div className="absolute bottom-[15%] left-[25%] w-32 h-32 bg-violet-500/5 rounded-full blur-2xl animate-pulse" style={{ animationDuration: "6s", animationDelay: "2s" }} />
      </div>

      {/* Grid */}
      <div className="absolute inset-0 pointer-events-none opacity-[0.03] dark:opacity-[0.06]"
        style={{ backgroundImage: "linear-gradient(to right, currentColor 1px, transparent 1px), linear-gradient(to bottom, currentColor 1px, transparent 1px)", backgroundSize: "40px 40px" }} />

      {/* Central orb */}
      <div className="absolute top-[28%] left-[8%] w-32 h-32" style={{ transform: `translate(${px(8)}, ${py(8)})` }}>
        <div className="relative w-full h-full">
          <div className="absolute inset-0 rounded-full border border-primary/20 animate-spin" style={{ animationDuration: "20s" }}>
            <div className="absolute -top-1 left-1/2 -translate-x-1/2 w-2 h-2 bg-primary/40 rounded-full" />
          </div>
          <div className="absolute inset-3 rounded-full border border-primary/15 animate-spin" style={{ animationDuration: "15s", animationDirection: "reverse" }}>
            <div className="absolute -bottom-1 left-1/2 -translate-x-1/2 w-1.5 h-1.5 bg-violet-400/50 rounded-full" />
          </div>
          <div className="absolute inset-6 rounded-full bg-gradient-to-br from-primary/20 to-emerald-400/10 flex items-center justify-center shadow-inner">
            <div className="w-8 h-8 rounded-full bg-gradient-to-br from-primary/60 to-emerald-500/40 flex items-center justify-center shadow-lg shadow-primary/20">
              <Layers className="w-4 h-4 text-primary-foreground dark:text-background" />
            </div>
          </div>
        </div>
      </div>

      {/* SVG connection lines */}
      <svg className="absolute inset-0 w-full h-full pointer-events-none" style={{ opacity: 0.12 }} xmlns="http://www.w3.org/2000/svg">
        <line x1="22%" y1="43%" x2="72%" y2="22%" stroke="currentColor" strokeWidth="1" strokeDasharray="4 6" />
        <line x1="22%" y1="43%" x2="78%" y2="55%" stroke="currentColor" strokeWidth="1" strokeDasharray="4 6" />
        <line x1="22%" y1="43%" x2="74%" y2="80%" stroke="currentColor" strokeWidth="1" strokeDasharray="4 6" />
      </svg>

      {/* Product Cards */}
      {PRODUCT_CARDS.map((card) => (
        <div key={card.id} className={`absolute ${card.x} ${card.y} hero-product-card`}
          style={{ transform: `translate(${px(-12 * (card.id % 2 === 0 ? -1 : 1))}, ${py(-10 * (card.id % 2 === 0 ? -1 : 1))}) rotate(${card.id === 1 ? 4 : card.id === 2 ? -3 : 2}deg)`, zIndex: card.z, animationDelay: card.delay }}>
          <div className={`w-52 bg-card/80 dark:bg-card/70 backdrop-blur-md rounded-xl border ${card.border} shadow-xl shadow-black/5 dark:shadow-black/20 overflow-hidden transition-all duration-700 hover:scale-105 hover:shadow-2xl cursor-default`}>
            <div className={`h-1.5 w-full bg-gradient-to-r ${card.color.replace('/20', '')} opacity-60`} />
            <div className="p-3">
              <div className="flex items-center gap-2 mb-2">
                <div className={`w-7 h-7 rounded-lg bg-gradient-to-br ${card.color} flex items-center justify-center`}>{card.icon}</div>
                <div>
                  <div className="text-xs font-semibold text-foreground leading-tight">{card.name}</div>
                  <div className="text-[10px] text-muted-foreground leading-tight">{card.tagline}</div>
                </div>
              </div>
              <div className="flex items-center gap-2">
                <div className="flex-1 h-1 bg-border rounded-full overflow-hidden">
                  <div className="h-full bg-gradient-to-r from-primary to-emerald-400 rounded-full" style={{ width: `${Math.min((card.votes / 350) * 100, 100)}%` }} />
                </div>
                <div className="flex items-center gap-0.5 text-[10px] font-medium text-primary">
                  <ArrowUp className="w-2.5 h-2.5" /><span>{card.votes}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      ))}

      {/* Floating badges */}
      {FLOATING_BADGES.map((badge, i) => (
        <div key={i} className={`absolute ${badge.x} ${badge.y} hero-float-badge`}
          style={{ transform: `translate(${px(-6 * (i % 2 === 0 ? 1 : -1))}, ${py(-6 * (i % 2 === 0 ? 1 : -1))})`, animationDelay: badge.delay, zIndex: 50 }}>
          <div className={`flex items-center gap-1.5 px-2.5 py-1.5 rounded-full border text-xs font-medium backdrop-blur-sm shadow-sm ${badge.color}`}>
            {badge.icon}{badge.label}
          </div>
        </div>
      ))}

      {/* Particles */}
      {[
        { top: "12%", left: "35%", size: "w-1.5 h-1.5", color: "bg-primary/50",      delay: "0s",   dur: "3s"   },
        { top: "65%", left: "55%", size: "w-1 h-1",     color: "bg-violet-400/60",    delay: "1s",   dur: "4s"   },
        { top: "30%", left: "48%", size: "w-2 h-2",     color: "bg-amber-400/40",     delay: "0.5s", dur: "5s"   },
        { top: "80%", left: "38%", size: "w-1 h-1",     color: "bg-sky-400/50",       delay: "1.5s", dur: "3.5s" },
        { top: "48%", left: "30%", size: "w-1.5 h-1.5", color: "bg-emerald-400/40",   delay: "2s",   dur: "4.5s" },
      ].map((p, i) => (
        <div key={i} className={`absolute ${p.size} ${p.color} rounded-full animate-ping pointer-events-none`}
          style={{ top: p.top, left: p.left, animationDelay: p.delay, animationDuration: p.dur }} />
      ))}
    </div>
  );
}
