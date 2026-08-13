"use client";

import { Search, Sparkles, User as UserIcon, LogOut } from 'lucide-react'
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
  const searchRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  // Ana sayfada search'ü otomatik genişlet
  const isHomePage = pathname === '/';
  
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
      <div className="mx-auto flex h-16 max-w-7xl items-center gap-4 px-4 sm:px-6">
        {/* Logo */}
        <Link href="/" className="flex shrink-0 items-center gap-2">
          <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-primary text-primary-foreground shadow-sm shadow-primary/40 ring-1 ring-primary/30">
            <Sparkles className="h-5 w-5" aria-hidden="true" />
          </span>
          <span className="font-sans text-xl font-extrabold tracking-tight text-foreground">
            Vitrin
          </span>
        </Link>

        {/* Main Navigation - her zaman göster */}
        <MainNav />

        {/* Search - Always Visible but Expandable */}
        <div className="flex-[4] flex justify-center mx-1">
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

        {/* Actions */}
        <div className="flex shrink-0 items-center gap-2 ml-1">

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
                <span className="text-sm font-medium hidden sm:inline-block">
                  {session.user?.name || session.user?.email}
                </span>
              </Link>
              <Button variant="ghost" size="icon" onClick={() => signOut()}>
                <LogOut className="h-5 w-5" />
              </Button>
            </div>
          ) : (
            <div className="flex items-center space-x-3">
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
              
              {/* Shimmer effect */}
              <div className="absolute inset-0 -skew-x-12 bg-gradient-to-r from-transparent via-white/20 to-transparent transform -translate-x-full group-hover:translate-x-full transition-transform duration-700"></div>
            </Button>
          </Link>
        </div>
      </div>
    </header>
  )
}
