"use client";

import { useScrollReveal } from "@/hooks/use-scroll-reveal";
import { cn } from "@/lib/utils";

type RevealVariant =
  | "fade-up"
  | "fade-down"
  | "fade-left"
  | "fade-right"
  | "fade-in"
  | "zoom-in"
  | "zoom-up";

interface ScrollRevealProps {
  children: React.ReactNode;
  variant?: RevealVariant;
  delay?: 0 | 1 | 2 | 3 | 4 | 5;
  duration?: "fast" | "normal" | "slow";
  className?: string;
  threshold?: number;
  rootMargin?: string;
}

const variantMap: Record<RevealVariant, { hidden: string; visible: string }> = {
  "fade-up":    { hidden: "opacity-0 translate-y-10",  visible: "opacity-100 translate-y-0" },
  "fade-down":  { hidden: "opacity-0 -translate-y-10", visible: "opacity-100 translate-y-0" },
  "fade-left":  { hidden: "opacity-0 -translate-x-10", visible: "opacity-100 translate-x-0" },
  "fade-right": { hidden: "opacity-0 translate-x-10",  visible: "opacity-100 translate-x-0" },
  "fade-in":    { hidden: "opacity-0",                 visible: "opacity-100" },
  "zoom-in":    { hidden: "opacity-0 scale-95",        visible: "opacity-100 scale-100" },
  "zoom-up":    { hidden: "opacity-0 scale-95 translate-y-6", visible: "opacity-100 scale-100 translate-y-0" },
};

const durationMap = { fast: "duration-500", normal: "duration-700", slow: "duration-1000" };
const delayMap: Record<0 | 1 | 2 | 3 | 4 | 5, string> = {
  0: "", 1: "delay-100", 2: "delay-200", 3: "delay-300", 4: "delay-[400ms]", 5: "delay-500",
};

export function ScrollReveal({
  children,
  variant = "fade-up",
  delay = 0,
  duration = "normal",
  className,
  threshold = 0.12,
  rootMargin = "0px 0px -50px 0px",
}: ScrollRevealProps) {
  const { ref, isVisible } = useScrollReveal({ threshold, rootMargin });
  const styles = variantMap[variant];

  return (
    <div
      ref={ref}
      className={cn(
        "transition-all ease-out will-change-transform",
        durationMap[duration],
        delayMap[delay],
        isVisible ? styles.visible : styles.hidden,
        className
      )}
    >
      {children}
    </div>
  );
}
