"use client";

import { Search, Sparkles, User as UserIcon, LogOut, Menu, X, Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { useSession, signOut } from "next-auth/react"
import Link from "next/link"
import Image from "next/image"
import { useRouter, usePathname } from "next/navigation"
import { useState, useRef, useEffect } from "react"
import { ThemeToggle } from "@/components/theme-toggle"
import { MainNav } from "@/components/main-nav"
import { NotificationDropdown } from "@/components/notification-dropdown"
import { SearchSuggestions } from "@/components/search-suggestions"

export function SiteHeader() {
  const { data: session, status } = useSession();
  const router = useRouter();
  const pathname = usePathname();
  const [searchQuery, setSearchQuery] = useState("");
  const [showSuggestions, setShowSuggestions] = useState(false);
  const [isSearchExpanded, setIsSearchExpanded] = useState(false);
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [isMobileSearchOpen, setIsMobileSearchOpen] = useState(false);
  const searchRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  // Ana sayfada search'ü otomatik genişlet
  const isHomePage = pathname === '/';

  // Sayfa değiştiğinde mobil menüyü kapat
  useEffect(() => {
    setIsMobileMenuOpen(false);
    setIsMobileSearchOpen(false);
  }, [pathname]);
  
  useEffect(() => {
    if (isHomePage) {
      setIsSearchExpanded(true);
    }
  }, [isHomePage]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (searchQuery.trim()) {
      router.push(`/search?q=${encodeURIComponent(searchQuery.trim())}`);
      setShowSuggestions(false);
      if (!isHomePage) {
        setIsSearchExpanded(false);
      }
    }
  };

  const expandSearch = () => {
    setIsSearchExpanded(true);
    setTimeout(() => {
      inputRef.current?.focus();
    }, 100);
  };

  // Handle click outside to close suggestions and search
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (searchRef.current && !searchRef.current.contains(event.target as Node)) {
        setShowSuggestions(false);
        if (!searchQuery.trim() && !isHomePage) {
          setIsSearchExpanded(false);
        }
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [searchQuery, isHomePage]);

  return (
    <header className="sticky top-0 z-[60] w-full border-b border-border bg-background/80 backdrop-blur-md">
      <div className="mx-auto flex h-16 max-w-7xl items-center gap-2 px-4 sm:px-6">
        {/* Logo */}
        <Link href="/" className="flex shrink-0 items-center gap-2">
          <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-primary text-primary-foreground shadow-sm shadow-primary/40 ring-1 ring-primary/30">
            <Sparkles className="h-5 w-5" aria-hidden="true" />
          </span>
          <span className="font-sans text-xl font-extrabold tracking-tight text-foreground">
            Vitrin
          </span>
        </Link>

        {/* Main Navigation - sadece lg ve üzeri */}
        <MainNav />

        {/* Search - Desktop: Always Visible but Expandable */}
        <div className="hidden md:flex flex-[4] justify-center mx-1">
          <div 
            ref={searchRef} 
            className="relative z-50 w-full"
          >
            <form onSubmit={handleSearch} className="relative">
              <div 
                className={`relative bg-muted/60 backdrop-blur-sm border border-border/50 rounded-full transition-all duration-500 ease-[cubic-bezier(0.16,1,0.3,1)] overflow-hidden ${
                  isSearchExpanded || isHomePage
                    ? 'shadow-2xl shadow-primary/20 ring-2 ring-primary/20 bg-background/95' 
                    : 'hover:bg-muted/80 hover:shadow-lg'
                }`}
                onClick={!isSearchExpanded && !isHomePage ? expandSearch : undefined}
              >
                <Search
                  className={`absolute left-4 top-1/2 h-5 w-5 -translate-y-1/2 transition-all duration-300 ${
                    isSearchExpanded || isHomePage
                      ? 'text-primary pointer-events-none' 
                      : 'text-muted-foreground'
                  }`}
                  aria-hidden="true"
                />
                <Input
                  ref={inputRef}
                  type="search"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  onFocus={() => {
                    setIsSearchExpanded(true);
                    setShowSuggestions(true);
                  }}
                  onBlur={() => {
                    setTimeout(() => {
                      if (!showSuggestions && !searchQuery.trim() && !isHomePage) {
                        setIsSearchExpanded(false);
                      }
                    }, 200);
                  }}
                  placeholder={isSearchExpanded || isHomePage ? "Ürün, kategori veya koleksiyon ara..." : "Ara..."}
                  aria-label="Ara"
                  className={`w-full rounded-full border-0 bg-transparent text-sm transition-all duration-500 placeholder:transition-all placeholder:duration-300 focus-visible:ring-0 focus-visible:ring-offset-0 ${
                    isSearchExpanded || isHomePage
                      ? 'h-12 pl-12 pr-16 text-base placeholder:text-muted-foreground/70 font-medium' 
                      : 'h-10 pl-12 pr-12 placeholder:text-muted-foreground/50 cursor-pointer'
                  }`}
                  readOnly={!isSearchExpanded && !isHomePage}
                />
                
                {/* Close/Clear button */}
                {(isSearchExpanded || isHomePage) && (
                  <button
                    type="button"
                    onClick={() => {
                      if (searchQuery) {
                        setSearchQuery("");
                      } else if (!isHomePage) {
                        setShowSuggestions(false);
                        setIsSearchExpanded(false);
                      }
                    }}
                    className="absolute right-3 top-1/2 -translate-y-1/2 w-8 h-8 rounded-full bg-muted/80 hover:bg-destructive/20 hover:text-destructive flex items-center justify-center transition-all duration-200 group/close backdrop-blur-sm border border-border/30 animate-fade-in-up"
                    aria-label={searchQuery ? "Temizle" : "Aramayı kapat"}
                  >
                    <span className="text-lg font-light text-muted-foreground group-hover/close:text-destructive transition-colors duration-200">×</span>
                  </button>
                )}
              </div>
            </form>
            
            {showSuggestions && (isSearchExpanded || isHomePage) && (
              <div className="animate-search-suggestions-in">
                <SearchSuggestions onClose={() => {
                  setShowSuggestions(false);
                  if (!searchQuery.trim() && !isHomePage) {
                    setIsSearchExpanded(false);
                  }
                }} />
              </div>
            )}
          </div>
        </div>

        {/* Desktop Actions */}
        <div className="hidden md:flex shrink-0 items-center gap-2 ml-1">
          {status === 'loading' ? (
            <div className="flex items-center space-x-3">
              <ThemeToggle />
              <div className="w-16 h-8 bg-muted animate-pulse rounded"></div>
              <div className="w-16 h-8 bg-muted animate-pulse rounded"></div>
            </div>
          ) : session ? (
            <div className="flex items-center gap-2">
              <ThemeToggle />
              <NotificationDropdown />
              <Link href="/profile" className="flex items-center gap-2 hover:opacity-80 transition-opacity">
                <div className="w-8 h-8 rounded-full overflow-hidden border border-border flex items-center justify-center bg-muted/50 shrink-0">
                  {session.user.image ? (
                    <Image
                      src={session.user.image}
                      alt={session.user.name || "Avatar"}
                      width={32}
                      height={32}
                      className="w-full h-full object-cover"
                    />
                  ) : (
                    <UserIcon className="w-4 h-4 text-muted-foreground" />
                  )}
                </div>
                <span className="text-sm font-medium hidden lg:inline-block">
                  {session.user?.name || session.user?.email}
                </span>
              </Link>
              <Button variant="ghost" size="icon" onClick={() => signOut()}>
                <LogOut className="h-5 w-5" />
              </Button>
            </div>
          ) : (
            <div className="flex items-center space-x-2">
              <ThemeToggle />
              <Link href="/login">
                <Button 
                  variant="ghost" 
                  className="relative overflow-hidden group hover:bg-transparent border-2 border-transparent hover:border-border transition-all duration-300"
                >
                  <span className="relative z-10 group-hover:text-primary transition-colors duration-300">
                    Giriş Yap
                  </span>
                  <div className="absolute inset-0 bg-gradient-to-r from-primary/10 to-secondary/10 transform scale-x-0 group-hover:scale-x-100 transition-transform duration-300 origin-left"></div>
                </Button>
              </Link>
              <Link href="/register">
                <Button 
                  className="relative overflow-hidden group bg-gradient-to-r from-primary to-primary/80 hover:from-primary/90 hover:to-primary/70 border-0 shadow-lg shadow-primary/25 hover:shadow-xl hover:shadow-primary/30 transition-all duration-300"
                >
                  <span className="relative z-10 font-semibold group-hover:scale-105 transition-transform duration-200">
                    Kayıt Ol
                  </span>
                  <div className="absolute inset-0 bg-gradient-to-r from-white/20 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300"></div>
                </Button>
              </Link>
            </div>
          )}

          <Link href="/submit">
            <Button className="relative overflow-hidden group bg-gradient-to-r from-emerald-500 to-green-500 hover:from-emerald-600 hover:to-green-600 text-white border-0 rounded-2xl px-6 py-2 font-bold shadow-lg shadow-emerald-500/25 hover:shadow-xl hover:shadow-emerald-500/30 transform hover:scale-105 transition-all duration-300">
              <span className="relative z-10 flex items-center gap-2 group-hover:gap-3 transition-all duration-200">
                <svg className="w-4 h-4 group-hover:rotate-12 transition-transform duration-300" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clipRule="evenodd" />
                </svg>
                Ekle
              </span>
              <div className="absolute inset-0 bg-gradient-to-r from-lime-400/20 to-emerald-300/20 opacity-0 group-hover:opacity-100 transition-opacity duration-300"></div>
              <div className="absolute inset-0 -skew-x-12 bg-gradient-to-r from-transparent via-white/20 to-transparent transform -translate-x-full group-hover:translate-x-full transition-transform duration-700"></div>
            </Button>
          </Link>
        </div>

        {/* Mobile Right Actions */}
        <div className="flex md:hidden items-center gap-1 ml-auto">
          <ThemeToggle />
          {/* Mobile Search Toggle */}
          <Button
            variant="ghost"
            size="icon"
            onClick={() => {
              setIsMobileSearchOpen(!isMobileSearchOpen);
              setIsMobileMenuOpen(false);
            }}
            aria-label="Aramayı aç"
          >
            <Search className="h-5 w-5" />
          </Button>
          {/* Mobile Submit shortcut */}
          <Link href="/submit">
            <Button
              size="icon"
              className="bg-emerald-500 hover:bg-emerald-600 text-white border-0 rounded-xl"
              aria-label="Ürün ekle"
            >
              <Plus className="h-5 w-5" />
            </Button>
          </Link>
          {/* Hamburger */}
          <Button
            variant="ghost"
            size="icon"
            onClick={() => {
              setIsMobileMenuOpen(!isMobileMenuOpen);
              setIsMobileSearchOpen(false);
            }}
            aria-label={isMobileMenuOpen ? "Menüyü kapat" : "Menüyü aç"}
          >
            {isMobileMenuOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
          </Button>
        </div>
      </div>

      {/* Mobile Search Bar */}
      {isMobileSearchOpen && (
        <div className="md:hidden border-t border-border bg-background/95 backdrop-blur-md px-4 py-3">
          <div ref={searchRef} className="relative">
            <form onSubmit={(e) => { handleSearch(e); setIsMobileSearchOpen(false); }}>
              <div className="relative bg-muted/60 border border-border/50 rounded-full overflow-hidden shadow-sm">
                <Search className="absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-primary pointer-events-none" aria-hidden="true" />
                <Input
                  type="search"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  onFocus={() => setShowSuggestions(true)}
                  placeholder="Ürün, kategori veya koleksiyon ara..."
                  aria-label="Ara"
                  autoFocus
                  className="h-11 w-full rounded-full border-0 bg-transparent pl-11 pr-12 text-sm focus-visible:ring-0 focus-visible:ring-offset-0"
                />
                {searchQuery && (
                  <button
                    type="button"
                    onClick={() => setSearchQuery("")}
                    className="absolute right-3 top-1/2 -translate-y-1/2 w-7 h-7 rounded-full bg-muted/80 flex items-center justify-center"
                    aria-label="Temizle"
                  >
                    <X className="h-3.5 w-3.5 text-muted-foreground" />
                  </button>
                )}
              </div>
            </form>
            {showSuggestions && searchQuery && (
              <SearchSuggestions onClose={() => {
                setShowSuggestions(false);
                setIsMobileSearchOpen(false);
              }} />
            )}
          </div>
        </div>
      )}

      {/* Mobile Menu Drawer */}
      {isMobileMenuOpen && (
        <div className="md:hidden border-t border-border bg-background/95 backdrop-blur-md">
          <nav className="px-4 py-4 space-y-1">
            <Link href="/launches" onClick={() => setIsMobileMenuOpen(false)} className="flex items-center gap-2 rounded-xl px-3 py-2.5 text-sm font-medium text-foreground hover:bg-muted transition-colors">
              En İyiler
            </Link>
            <Link href="/launches/upcoming" onClick={() => setIsMobileMenuOpen(false)} className="flex items-center gap-2 rounded-xl px-3 py-2.5 text-sm font-medium text-foreground hover:bg-muted transition-colors">
              Lansmanlar
            </Link>
            <Link href="/blog" onClick={() => setIsMobileMenuOpen(false)} className="flex items-center gap-2 rounded-xl px-3 py-2.5 text-sm font-medium text-foreground hover:bg-muted transition-colors">
              Haberler
            </Link>
            <Link href="/activity" onClick={() => setIsMobileMenuOpen(false)} className="flex items-center gap-2 rounded-xl px-3 py-2.5 text-sm font-medium text-foreground hover:bg-muted transition-colors">
              Forumlar
            </Link>
            <Link href="/advertise" onClick={() => setIsMobileMenuOpen(false)} className="flex items-center gap-2 rounded-xl px-3 py-2.5 text-sm font-medium text-foreground hover:bg-muted transition-colors">
              Reklamver
            </Link>

            <div className="pt-2 border-t border-border">
              {status === 'loading' ? null : session ? (
                <div className="space-y-1">
                  <Link href="/profile" onClick={() => setIsMobileMenuOpen(false)} className="flex items-center gap-3 rounded-xl px-3 py-2.5 hover:bg-muted transition-colors">
                    <div className="w-8 h-8 rounded-full overflow-hidden border border-border flex items-center justify-center bg-muted/50 shrink-0">
                      {session.user.image ? (
                        <Image src={session.user.image} alt={session.user.name || "Avatar"} width={32} height={32} className="w-full h-full object-cover" />
                      ) : (
                        <UserIcon className="w-4 h-4 text-muted-foreground" />
                      )}
                    </div>
                    <span className="text-sm font-medium">{session.user?.name || session.user?.email}</span>
                  </Link>
                  <NotificationDropdown />
                  <button
                    type="button"
                    onClick={() => { signOut(); setIsMobileMenuOpen(false); }}
                    className="flex w-full items-center gap-2 rounded-xl px-3 py-2.5 text-sm font-medium text-destructive hover:bg-destructive/10 transition-colors"
                  >
                    <LogOut className="h-4 w-4" />
                    Çıkış Yap
                  </button>
                </div>
              ) : (
                <div className="flex flex-col gap-2 pt-1">
                  <Link href="/login" onClick={() => setIsMobileMenuOpen(false)}>
                    <Button variant="outline" className="w-full">Giriş Yap</Button>
                  </Link>
                  <Link href="/register" onClick={() => setIsMobileMenuOpen(false)}>
                    <Button className="w-full">Kayıt Ol</Button>
                  </Link>
                </div>
              )}
            </div>
          </nav>
        </div>
      )}
    </header>
  )
}
