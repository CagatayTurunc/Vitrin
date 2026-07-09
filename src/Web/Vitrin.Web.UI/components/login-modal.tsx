import { X, Mail } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useRouter } from "next/navigation";
import Image from "next/image";

interface LoginModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export function LoginModal({ isOpen, onClose }: LoginModalProps) {
  const router = useRouter();

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 backdrop-blur-sm">
      <div className="relative w-full max-w-[440px] rounded-2xl border border-border bg-card p-8 shadow-2xl animate-in fade-in zoom-in-95 duration-200">
        <button
          onClick={onClose}
          className="absolute right-4 top-4 rounded-full p-2 text-muted-foreground hover:bg-muted hover:text-foreground transition-colors"
          aria-label="Kapat"
        >
          <X className="h-5 w-5" />
        </button>
        
        <div className="mb-8 mt-2 flex flex-col items-center text-center">
          <div className="relative mb-6 h-24 w-24 overflow-hidden rounded-full border-4 border-muted/50 bg-muted/20">
            <Image
              src="/vitrin_mascot.png"
              alt="Vitrin Maskotu"
              fill
              className="object-cover"
            />
          </div>
          
          <h2 className="text-2xl font-bold tracking-tight text-foreground">
            Oy vermek için giriş yapın
          </h2>
          <p className="mt-3 text-[15px] leading-relaxed text-muted-foreground px-4">
            Teknoloji dünyasındaki en yeni ürünleri keşfeden ve paylaşan harika topluluğumuza katılın.
          </p>
        </div>

        <div className="flex flex-col gap-3">
          <Button 
            variant="outline" 
            className="h-12 w-full justify-center gap-3 border-border bg-background text-[15px] font-semibold hover:bg-muted"
            onClick={() => router.push("/login")}
          >
            <svg viewBox="0 0 24 24" className="h-5 w-5" aria-hidden="true">
              <path d="M12.0003 4.75C13.7703 4.75 15.3553 5.36002 16.6053 6.54998L20.0303 3.125C17.9502 1.19 15.2353 0 12.0003 0C7.31028 0 3.25527 2.69 1.28027 6.60998L5.27028 9.70498C6.21525 6.86002 8.87028 4.75 12.0003 4.75Z" fill="#EA4335"/>
              <path d="M23.49 12.275C23.49 11.49 23.415 10.73 23.3 10H12V14.51H18.47C18.18 15.99 17.34 17.25 16.08 18.1L19.945 21.1C22.2 19.01 23.49 15.92 23.49 12.275Z" fill="#4285F4"/>
              <path d="M5.26498 14.2949C5.02498 13.5699 4.88501 12.7999 4.88501 11.9999C4.88501 11.1999 5.01998 10.4299 5.26498 9.7049L1.275 6.60986C0.46 8.22986 0 10.0599 0 11.9999C0 13.9399 0.46 15.7699 1.28 17.3899L5.26498 14.2949Z" fill="#FBBC05"/>
              <path d="M12.0004 24.0001C15.2404 24.0001 17.9654 22.935 19.9454 21.095L16.0804 18.095C15.0054 18.82 13.6204 19.245 12.0004 19.245C8.8704 19.245 6.21537 17.135 5.26537 14.29L1.27539 17.385C3.25539 21.31 7.3104 24.0001 12.0004 24.0001Z" fill="#34A853"/>
            </svg>
            Google ile Giriş Yap
          </Button>

          <Button 
            variant="outline" 
            className="h-12 w-full justify-center gap-3 border-border bg-background text-[15px] font-semibold hover:bg-muted"
            onClick={() => router.push("/login")}
          >
            <svg viewBox="0 0 24 24" className="h-5 w-5" fill="currentColor" aria-hidden="true">
              <path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/>
            </svg>
            Github ile Giriş Yap
          </Button>

          <Button 
            variant="outline" 
            className="h-12 w-full justify-center gap-3 border-border bg-background text-[15px] font-semibold hover:bg-muted"
            onClick={() => router.push("/login")}
          >
            <Mail className="h-5 w-5 text-muted-foreground" />
            E-posta ile Giriş Yap
          </Button>
        </div>

        <p className="mt-8 text-center text-[13px] text-muted-foreground/80">
          İzniniz olmadan hesaplarınızda asla paylaşım yapmayız.
        </p>
      </div>
    </div>
  );
}
