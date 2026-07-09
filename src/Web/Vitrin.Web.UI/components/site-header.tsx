"use client";

import { Search, Sparkles, User as UserIcon, LogOut } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { useSession, signIn, signOut } from "next-auth/react"
import Link from "next/link"
import { ThemeToggle } from "@/components/theme-toggle"

export function SiteHeader() {
  const { data: session } = useSession();

  return (
    <header className="sticky top-0 z-50 w-full border-b border-border bg-background/80 backdrop-blur-md">
      <div className="mx-auto flex h-16 max-w-6xl items-center gap-4 px-4 sm:px-6">
        {/* Logo */}
        <a href="/" className="flex shrink-0 items-center gap-2">
          <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-primary text-primary-foreground shadow-sm shadow-primary/40 ring-1 ring-primary/30">
            <Sparkles className="h-5 w-5" aria-hidden="true" />
          </span>
          <span className="font-sans text-xl font-extrabold tracking-tight text-foreground">
            Vitrin
          </span>
        </a>

        {/* Search */}
        <div className="relative mx-auto hidden w-full max-w-md md:block">
          <Search
            className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground"
            aria-hidden="true"
          />
          <Input
            type="search"
            placeholder="Ürün, kategori veya koleksiyon ara..."
            aria-label="Ara"
            className="h-10 rounded-full border-border bg-muted/60 pl-9 text-sm shadow-none focus-visible:bg-background"
          />
        </div>

        {/* Actions */}
        <div className="ml-auto flex shrink-0 items-center gap-2">
          <Button
            variant="ghost"
            size="icon"
            aria-label="Ara"
            className="md:hidden"
          >
            <Search className="h-5 w-5" />
          </Button>

          {session ? (
            <div className="flex items-center gap-2">
              <ThemeToggle />
              <span className="text-sm font-medium hidden sm:inline-block">
                {session.user?.name || session.user?.email}
              </span>
              <Button variant="ghost" size="icon" onClick={() => signOut()}>
                <LogOut className="h-5 w-5" />
              </Button>
            </div>
          ) : (
            <div className="flex items-center space-x-4">
              <ThemeToggle />
              <Link href="/login">
                <Button variant="ghost" className="hidden sm:flex">Giriş Yap</Button>
              </Link>
              <Link href="/register">
                <Button>Kayıt Ol</Button>
              </Link>
            </div>
          )}

          <Link href="/submit">
            <Button className="rounded-full font-semibold shadow-sm shadow-primary/30">
              Ürün Ekle
            </Button>
          </Link>
        </div>
      </div>
    </header>
  )
}
