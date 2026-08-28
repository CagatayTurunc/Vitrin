'use client'

/**
 * Social Share Component — Bölüm 3.7
 * 
 * Sosyal medya paylaşım butonları
 */

import { useState } from 'react'
import { Share2, Link as LinkIcon, Check } from 'lucide-react'
import { generateShareUrls, type SocialShareData } from '@/lib/social-media'

interface SocialShareProps {
  data: SocialShareData
  className?: string
  showLabels?: boolean
}

export function SocialShare({ data, className = '', showLabels = false }: SocialShareProps) {
  const [copied, setCopied] = useState(false)
  const shareUrls = generateShareUrls(data)

  const handleCopyLink = async () => {
    try {
      await navigator.clipboard.writeText(data.url)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch (error) {
      console.error('Failed to copy:', error)
    }
  }

  const handleShare = (url: string) => {
    window.open(url, '_blank', 'width=600,height=400')
  }

  const buttons = [
    {
      name: 'X (Twitter)',
      icon: Share2,
      url: shareUrls.twitter,
      color: 'hover:bg-black hover:text-white',
    },
    {
      name: 'Facebook',
      icon: Share2,
      url: shareUrls.facebook,
      color: 'hover:bg-[#1877F2] hover:text-white',
    },
    {
      name: 'LinkedIn',
      icon: Share2,
      url: shareUrls.linkedin,
      color: 'hover:bg-[#0A66C2] hover:text-white',
    },
  ]

  return (
    <div className={`flex items-center gap-2 ${className}`}>
      <div className="flex items-center gap-1 text-sm text-muted-foreground">
        <Share2 className="w-4 h-4" />
        {showLabels && <span>Paylaş:</span>}
      </div>

      {buttons.map((button) => (
        <button
          key={button.name}
          onClick={() => handleShare(button.url)}
          className={`flex items-center gap-2 px-3 py-1.5 rounded-lg border border-border transition-colors ${button.color}`}
          aria-label={`${button.name}'da paylaş`}
        >
          <button.icon className="w-4 h-4" />
          {showLabels && <span className="text-sm font-medium">{button.name}</span>}
        </button>
      ))}

      <button
        onClick={handleCopyLink}
        className="flex items-center gap-2 px-3 py-1.5 rounded-lg border border-border hover:bg-muted transition-colors"
        aria-label="Linki kopyala"
      >
        {copied ? (
          <>
            <Check className="w-4 h-4 text-emerald-500" />
            {showLabels && <span className="text-sm font-medium text-emerald-500">Kopyalandı!</span>}
          </>
        ) : (
          <>
            <LinkIcon className="w-4 h-4" />
            {showLabels && <span className="text-sm font-medium">Link</span>}
          </>
        )}
      </button>
    </div>
  )
}

/**
 * Native Web Share API destekli paylaşım butonu
 */
export function NativeShareButton({
  data,
  className = '',
}: {
  data: SocialShareData
  className?: string
}) {
  const [canShare, setCanShare] = useState(false)

  // Check if Web Share API is available
  useState(() => {
    if (typeof navigator !== 'undefined' && navigator.share) {
      setCanShare(true)
    }
  })

  const handleNativeShare = async () => {
    if (!navigator.share) return

    try {
      await navigator.share({
        title: data.title,
        text: data.description,
        url: data.url,
      })
    } catch (error) {
      // User cancelled or error occurred
      console.error('Share failed:', error)
    }
  }

  if (!canShare) return null

  return (
    <button
      onClick={handleNativeShare}
      className={`flex items-center gap-2 px-4 py-2 rounded-lg bg-emerald-500 text-white hover:bg-emerald-600 transition-colors ${className}`}
    >
      <Share2 className="w-4 h-4" />
      <span className="text-sm font-medium">Paylaş</span>
    </button>
  )
}
