// ============================================================
// Madde 23 — Spike Test
// ============================================================
//
// Ani trafik artışı senaryosu
// Simüle eder:
//   - Product Hunt launch
//   - Viral tweet
//   - Hacker News front page
//   - Reddit thread
//
// Senaryo:
//   - Normal: 100 users
//   - Spike: 1000 users (30 saniyede)
//   - Recovery: 100 users'a dön
//
// Kullanım:
//   k6 run tests/load/spike-test.js
//
// Hedef:
//   - Sistem spike'ı kaldırmalı
//   - Recovery hızlı olmalı
//   - Error rate < %5
// ============================================================

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// ── Custom Metrics ──────────────────────────────────────────
const errorRate = new Rate('errors');
const responseTime = new Trend('response_time');
// ────────────────────────────────────────────────────────────

// ── Test Configuration ──────────────────────────────────────
export const options = {
  stages: [
    // Stage 1: Normal trafik (1 dk)
    { duration: '1m', target: 100 },
    
    // Stage 2: SPIKE! (30 saniye)
    { duration: '30s', target: 1000 },
    
    // Stage 3: Spike devam ediyor (2 dk)
    { duration: '2m', target: 1000 },
    
    // Stage 4: Recovery (30 saniye)
    { duration: '30s', target: 100 },
    
    // Stage 5: Normal'e dön (1 dk)
    { duration: '1m', target: 100 },
    
    // Stage 6: Ramp-down
    { duration: '30s', target: 0 },
  ],
  
  thresholds: {
    // Spike sırasında response time artabilir ama max 2s
    'http_req_duration': ['p(95)<2000'],
    
    // Error rate max %5 (spike tolerance)
    'http_req_failed': ['rate<0.05'],
    
    // Recovery sonrası tekrar normal seviyede
    'http_req_duration{stage:recovery}': ['p(95)<500'],
    
    // Custom metrics
    'errors': ['rate<0.05'],
  },
  
  tags: {
    test_type: 'spike',
    environment: __ENV.ENVIRONMENT || 'local',
  },
};

// ── Base URLs ───────────────────────────────────────────────
const BASE_URL = __ENV.BASE_URL || 'http://localhost:3000';
const API_URL = __ENV.API_URL || 'http://localhost:8080';
// ────────────────────────────────────────────────────────────

// ── Critical Endpoints (spike sırasında en çok hit alanlar) ─
const criticalEndpoints = [
  { url: `${BASE_URL}/`, weight: 50, name: 'Homepage' },
  { url: `${BASE_URL}/products`, weight: 30, name: 'Products' },
  { url: `${API_URL}/api/products/trending`, weight: 20, name: 'Trending API' },
];

function selectEndpoint() {
  const rand = Math.random() * 100;
  let cumulative = 0;
  
  for (const endpoint of criticalEndpoints) {
    cumulative += endpoint.weight;
    if (rand <= cumulative) {
      return endpoint;
    }
  }
  
  return criticalEndpoints[0];
}

// ── Detect Current Stage ───────────────────────────────────
function getCurrentStage(vu) {
  // Approximate based on execution time
  const now = Date.now();
  const elapsed = (now - __ENV.TEST_START_TIME) / 1000;
  
  if (elapsed < 60) return 'normal';
  if (elapsed < 90) return 'spike_rampup';
  if (elapsed < 210) return 'spike_sustained';
  if (elapsed < 240) return 'recovery';
  if (elapsed < 300) return 'post_recovery';
  return 'rampdown';
}

// ── Main Test Function ──────────────────────────────────────
export default function () {
  const endpoint = selectEndpoint();
  const stage = getCurrentStage(__VU);
  
  const startTime = Date.now();
  const response = http.get(endpoint.url, {
    tags: {
      name: endpoint.name,
      stage: stage,
    },
  });
  const duration = Date.now() - startTime;
  
  // Record metrics
  responseTime.add(duration, { stage });
  
  // Validation
  const success = check(response, {
    'status is 200 or 503': (r) => r.status === 200 || r.status === 503,
    'response received': (r) => r.body.length > 0,
  });
  
  if (!success || response.status !== 200) {
    errorRate.add(1);
    
    if (response.status === 503) {
      // 503 Service Unavailable — expected during extreme spike
      console.log(`⚠️  503 Service Unavailable — ${endpoint.name} (stage: ${stage})`);
    } else {
      console.error(`❌ ${endpoint.name} failed — Status: ${response.status} (stage: ${stage})`);
    }
  } else {
    errorRate.add(0);
  }
  
  // Spike sırasında daha az think time (users agresif)
  const thinkTime = stage.includes('spike') 
    ? Math.random() * 0.5 + 0.5  // 0.5-1s
    : Math.random() * 2 + 1;      // 1-3s
  
  sleep(thinkTime);
}

// ── Setup ───────────────────────────────────────────────────
export function setup() {
  console.log('');
  console.log('========================================');
  console.log('⚡ Spike Test Başlıyor');
  console.log('========================================');
  console.log(`Base URL: ${BASE_URL}`);
  console.log(`API URL:  ${API_URL}`);
  console.log('');
  console.log('Senaryo:');
  console.log('  1. Normal trafik (100 users, 1 dk)');
  console.log('  2. 🔥 SPIKE! (1000 users, 30s ramp-up)');
  console.log('  3. Spike devam (1000 users, 2 dk)');
  console.log('  4. Recovery (100 users, 30s)');
  console.log('  5. Post-recovery check (1 dk)');
  console.log('========================================');
  console.log('');
  
  // Health check
  const health = http.get(`${API_URL}/health`);
  if (health.status !== 200) {
    throw new Error('❌ Health check failed — site down?');
  }
  
  console.log('✅ Health check OK\n');
  
  // Store test start time for stage detection
  __ENV.TEST_START_TIME = Date.now();
  
  return { 
    startTime: new Date().toISOString(),
    testStartEpoch: Date.now(),
  };
}

// ── Teardown ────────────────────────────────────────────────
export function teardown(data) {
  console.log('');
  console.log('========================================');
  console.log('📊 Spike Test Sonuçları');
  console.log('========================================');
  console.log(`Start:    ${data.startTime}`);
  console.log(`End:      ${new Date().toISOString()}`);
  console.log(`Duration: ${((Date.now() - data.testStartEpoch) / 1000 / 60).toFixed(1)} dakika`);
  console.log('');
  console.log('Kontrol edilecekler:');
  console.log('  ✓ Spike sırasında sistem ayakta kaldı mı?');
  console.log('  ✓ Recovery hızlı oldu mu?');
  console.log('  ✓ Error rate %5 altında mı?');
  console.log('  ✓ Auto-scaling tetiklendi mi?');
  console.log('  ✓ Circuit breakers devreye girdi mi?');
  console.log('  ✓ Cache hit rate nasıl?');
  console.log('');
  console.log('Grafana Dashboard:');
  console.log('  - Response time graph (spike görünmeli)');
  console.log('  - Error rate (spike sırasında artış beklenir)');
  console.log('  - Resource usage (CPU, memory spike)');
  console.log('  - Database connections (pool exhaustion?)');
  console.log('========================================');
}

// ── Spike Recovery Analysis ────────────────────────────────
export function handleSummary(data) {
  const spikeDuration = data.metrics.http_req_duration.values['p(95)'];
  const errorRate = data.metrics.errors.values.rate;
  
  let status = '✅ PASS';
  let recommendations = [];
  
  if (spikeDuration > 2000) {
    status = '⚠️  WARN';
    recommendations.push('Response time yüksek — cache stratejisi gözden geçir');
  }
  
  if (errorRate > 0.05) {
    status = '❌ FAIL';
    recommendations.push('Error rate çok yüksek — rate limiting veya auto-scaling gerekli');
  }
  
  const summary = {
    'stdout': JSON.stringify({
      test: 'spike-test',
      status: status,
      spike_p95_response_time: spikeDuration,
      error_rate: errorRate,
      recommendations: recommendations,
    }, null, 2),
  };
  
  return summary;
}
