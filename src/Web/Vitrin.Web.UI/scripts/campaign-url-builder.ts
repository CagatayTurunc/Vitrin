#!/usr/bin/env tsx
/**
 * Campaign URL Builder — Bölüm 4.2
 * 
 * UTM parametreli campaign URL'leri oluşturur
 * 
 * Kullanım:
 *   pnpm tsx scripts/campaign-url-builder.ts
 */

import { buildCampaignURL } from '../lib/marketing'

interface Campaign {
  name: string
  source: string
  medium: string
  campaign: string
  term?: string
  content?: string
}

const campaigns: Campaign[] = [
  // Launch Campaigns
  {
    name: 'Product Hunt Launch',
    source: 'producthunt',
    medium: 'referral',
    campaign: 'launch_2026',
  },
  {
    name: 'Hacker News Show HN',
    source: 'hackernews',
    medium: 'social',
    campaign: 'launch_2026',
  },
  {
    name: 'Twitter Launch Thread',
    source: 'twitter',
    medium: 'social',
    campaign: 'launch_2026',
    content: 'launch_thread',
  },
  {
    name: 'LinkedIn Company Page',
    source: 'linkedin',
    medium: 'social',
    campaign: 'launch_2026',
  },
  
  // Email Campaigns
  {
    name: 'Welcome Email',
    source: 'email',
    medium: 'email',
    campaign: 'welcome_series',
  },
  {
    name: 'Daily Digest',
    source: 'email',
    medium: 'email',
    campaign: 'daily_digest',
  },
  {
    name: 'Weekly Roundup',
    source: 'email',
    medium: 'email',
    campaign: 'weekly_roundup',
  },
  
  // Paid Ads
  {
    name: 'Google Search - Ürün Keşfi',
    source: 'google',
    medium: 'cpc',
    campaign: 'search_discovery',
    term: 'ürün keşfi',
  },
  {
    name: 'Google Search - Product Hunt Türkiye',
    source: 'google',
    medium: 'cpc',
    campaign: 'search_brand',
    term: 'product hunt türkiye',
  },
  {
    name: 'Facebook Ads - Maker Audience',
    source: 'facebook',
    medium: 'cpc',
    campaign: 'maker_acquisition',
    content: 'carousel_v1',
  },
  
  // Influencer & PR
  {
    name: 'Tech Blog Guest Post',
    source: 'tech_blog',
    medium: 'referral',
    campaign: 'guest_post',
  },
  {
    name: 'Podcast Interview',
    source: 'podcast',
    medium: 'audio',
    campaign: 'interview_series',
  },
]

function main() {
  console.log('🔗 Vitrin Campaign URL Builder')
  console.log('=' .repeat(80))
  console.log()
  
  const baseURL = 'https://vitrin.com'
  
  campaigns.forEach((campaign) => {
    const url = buildCampaignURL(baseURL, {
      source: campaign.source,
      medium: campaign.medium,
      campaign: campaign.campaign,
      term: campaign.term,
      content: campaign.content,
    })
    
    console.log(`📌 ${campaign.name}`)
    console.log(`   ${url}`)
    console.log()
  })
  
  console.log('=' .repeat(80))
  console.log('✅ URL'ler oluşturuldu!')
  console.log()
  console.log('💡 İpucu: Bu URL'leri kampanyalarınızda kullanarak')
  console.log('   GA4\'te traffic source'ları takip edebilirsiniz.')
  console.log()
  console.log('📊 Google Analytics → Reports → Acquisition → Traffic acquisition')
}

main()
