"use client";

import { ArrowLeft } from "lucide-react";

interface BackButtonProps {
  className?: string;
  label?: string;
}

export function BackButton({ className, label = "Önceki sayfaya dön" }: BackButtonProps) {
  return (
    <button
      type="button"
      onClick={() => window.history.back()}
      className={className}
    >
      <ArrowLeft className="h-4 w-4" />
      {label}
    </button>
  );
}
