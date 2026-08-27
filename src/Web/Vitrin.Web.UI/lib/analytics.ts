/**
 * Analytics & Tracking — Bölüm 4.3
 * 
 * Google Analytics 4 event tracking
 */

declare global {
  interface Window {
    gtag?: (
      command: 'config' | 'event' | 'set',
      targetId: string,
      config?: Record<string, unknown>
    ) => void
  }
}

export const GA_MEASUREMENT_ID = process.env.NEXT_PUBLIC_GA_MEASUREMENT_ID

/**
 * Madde 4.3: Google Analytics pageview tracking
 */
export function pageview(url: string) {
  if (!GA_MEASUREMENT_ID || !window.gtag) return
  
  window.gtag('config', GA_MEASUREMENT_ID, {
    page_path: url,
  })
}

/**
 * Madde 4.3: Custom event tracking
 */
export function trackEvent(
  action: string,
  params?: Record<string, unknown>
) {
  if (!GA_MEASUREMENT_ID || !window.gtag) return
  
  window.gtag('event', action, params)
}

/**
 * Conversion Events
 */

// Product interactions
export function trackProductView(productId: string, productName: string) {
  trackEvent('product_view', {
    product_id: productId,
    product_name: productName,
  })
}

export function trackUpvote(productId: string, productName: string) {
  trackEvent('upvote', {
    product_id: productId,
    product_name: productName,
  })
}

export function trackComment(productId: string) {
  trackEvent('comment', {
    product_id: productId,
  })
}

export function trackProductSubmit(productId: string, productName: string) {
  trackEvent('product_submit', {
    product_id: productId,
    product_name: productName,
    value: 1, // Conversion value
  })
}

// User actions
export function trackSignup(method: 'email' | 'google' | 'github') {
  trackEvent('sign_up', {
    method,
  })
}

export function trackLogin(method: 'email' | 'google' | 'github') {
  trackEvent('login', {
    method,
  })
}

// Newsletter
export function trackNewsletterSignup(preferences: string[]) {
  trackEvent('newsletter_signup', {
    preferences: preferences.join(','),
  })
}

// Search
export function trackSearch(searchTerm: string) {
  trackEvent('search', {
    search_term: searchTerm,
  })
}

// Social sharing
export function trackShare(platform: string, url: string) {
  trackEvent('share', {
    platform,
    url,
  })
}

// Outbound links
export function trackOutboundLink(url: string) {
  trackEvent('click', {
    event_category: 'outbound',
    event_label: url,
    transport_type: 'beacon',
  })
}

/**
 * Enhanced E-commerce (future)
 */
export function trackBeginCheckout(value: number) {
  trackEvent('begin_checkout', {
    value,
    currency: 'TRY',
  })
}

export function trackPurchase(
  transactionId: string,
  value: number,
  items: unknown[]
) {
  trackEvent('purchase', {
    transaction_id: transactionId,
    value,
    currency: 'TRY',
    items,
  })
}

/**
 * User engagement
 */
export function trackScrollDepth(percentage: number) {
  trackEvent('scroll', {
    event_category: 'engagement',
    event_label: `${percentage}%`,
  })
}

export function trackTimeOnPage(seconds: number) {
  trackEvent('timing_complete', {
    name: 'page_read_time',
    value: seconds * 1000, // milliseconds
  })
}

/**
 * UTM Tracking helper
 */
export function getUTMParams(): Record<string, string> {
  if (typeof window === 'undefined') return {}
  
  const searchParams = new URLSearchParams(window.location.search)
  const utmParams: Record<string, string> = {}
  
  const utmKeys = ['utm_source', 'utm_medium', 'utm_campaign', 'utm_term', 'utm_content']
  
  utmKeys.forEach((key) => {
    const value = searchParams.get(key)
    if (value) {
      utmParams[key] = value
    }
  })
  
  return utmParams
}

/**
 * Store UTM params in sessionStorage
 */
export function storeUTMParams() {
  if (typeof window === 'undefined') return
  
  const utmParams = getUTMParams()
  if (Object.keys(utmParams).length > 0) {
    sessionStorage.setItem('utm_params', JSON.stringify(utmParams))
  }
}

/**
 * Get stored UTM params
 */
export function getStoredUTMParams(): Record<string, string> {
  if (typeof window === 'undefined') return {}
  
  const stored = sessionStorage.getItem('utm_params')
  return stored ? JSON.parse(stored) : {}
}
