// ============================================================
// Madde 23 — Basic Load Test
// ============================================================
//
// Normal trafik senaryosu (steady state)
// - 100 concurrent users
// - 5 dakika süre
// - Gradual ramp-up (30s)
//
// Kullanım:
//   k6 run tests/load/basic-load-test.js
//   k6 run --vus 200 --duration 10m tests/load/basic-load-test.js
//
// Hedefler:
//   - P95 response time < 500ms
//   - Error rate < %1
//   - Min 500 RPS
// ============================================================

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

// ── Custom Metrics ──────────────────────────────────────────
const errorRate = new Rate('errors');
const responseTime = new Trend('response_time');
const requestCount = new Counter('request_count');
// ────────────────────────────────────────────────────────────

// ── Test Configuration ──────────────────────────────────────
export const options = {
  stages: [
    // Ramp-up: 0 → 100 users (30 saniye)
    { duration: '30s', target: 100 },
    
    // Steady state: 100 users (5 dakika)
    { duration: '5m', target: 100 },
    
    // Ramp-down: 100 → 0 users (30 saniye)
    { duration: '30s', target: 0 },
  ],
  
  thresholds: {
    // P95 response time < 500ms
    'http_req_duration': ['p(95)<500'],
    
    // P99 response time < 1000ms
    'http_req_duration{type:api}': ['p(99)<1000'],
    
    // %99 success rate (max %1 error)
    'http_req_failed': ['rate<0.01'],
    
    // Min 500 requests/sec
    'http_reqs': ['rate>500'],
    
    // Custom metrics
    'errors': ['rate<0.01'],
    'response_time': ['p(95)<500'],
  },
  
  // Global tags
  tags: {
    test_type: 'basic_load',
    environment: __ENV.ENVIRONMENT || 'local',
  },
};

// ── Base URL ────────────────────────────────────────────────
const BASE_URL = __ENV.BASE_URL || 'http://localhost:3000';
const API_URL = __ENV.API_URL || 'http://localhost:8080';
// ────────────────────────────────────────────────────────────

// ── Test Scenarios ──────────────────────────────────────────
const scenarios = {
  homepage: {
    weight: 30,
    url: `${BASE_URL}/`,
    name: 'Homepage',
  },
  products: {
    weight: 25,
    url: `${BASE_URL}/products`,
    name: 'Product List',
  },
  product_detail: {
    weight: 20,
    url: `${BASE_URL}/products/1`,
    name: 'Product Detail',
  },
  search: {
    weight: 10,
    url: `${BASE_URL}/search?q=test`,
    name: 'Search',
  },
  api_health: {
    weight: 5,
    url: `${API_URL}/health`,
    name: 'API Health Check',
  },
  api_products: {
    weight: 10,
    url: `${API_URL}/api/products?page=1&limit=20`,
    name: 'API Product List',
  },
};

// ── Helper Functions ────────────────────────────────────────
function selectScenario() {
  const rand = Math.random() * 100;
  let cumulative = 0;
  
  for (const [key, scenario] of Object.entries(scenarios)) {
    cumulative += scenario.weight;
    if (rand <= cumulative) {
      return scenario;
    }
  }
  
  return scenarios.homepage;
}

// ── Main Test Function ──────────────────────────────────────
export default function () {
  const scenario = selectScenario();
  
  const startTime = Date.now();
  const response = http.get(scenario.url, {
    tags: {
      name: scenario.name,
      type: scenario.url.includes('/api/') ? 'api' : 'page',
    },
  });
  const duration = Date.now() - startTime;
  
  // Metrics
  requestCount.add(1);
  responseTime.add(duration);
  
  // Validations
  const success = check(response, {
    'status is 200': (r) => r.status === 200,
    'response time < 1s': (r) => r.timings.duration < 1000,
    'has content': (r) => r.body.length > 0,
  });
  
  if (!success) {
    errorRate.add(1);
    console.error(`❌ ${scenario.name} failed — Status: ${response.status}`);
  } else {
    errorRate.add(0);
  }
  
  // Realistic user behavior — think time
  sleep(Math.random() * 3 + 1); // 1-4 saniye arası
}

// ── Setup (test başlamadan önce) ───────────────────────────
export function setup() {
  console.log('🚀 Basic Load Test başlıyor...');
  console.log(`   Base URL: ${BASE_URL}`);
  console.log(`   API URL: ${API_URL}`);
  console.log(`   Environment: ${__ENV.ENVIRONMENT || 'local'}`);
  console.log('');
  
  // Health check — site ayakta mı?
  const healthCheck = http.get(`${API_URL}/health`);
  if (healthCheck.status !== 200) {
    throw new Error(`❌ Health check failed — Status: ${healthCheck.status}`);
  }
  
  console.log('✅ Health check OK — Test başlıyor\n');
  
  return { startTime: new Date().toISOString() };
}

// ── Teardown (test bittikten sonra) ────────────────────────
export function teardown(data) {
  console.log('\n');
  console.log('========================================');
  console.log('📊 Basic Load Test Tamamlandı');
  console.log('========================================');
  console.log(`Start: ${data.startTime}`);
  console.log(`End:   ${new Date().toISOString()}`);
  console.log('');
  console.log('Sonraki adım:');
  console.log('  - Grafana dashboard kontrolü');
  console.log('  - Error log analizi');
  console.log('  - Response time trends');
  console.log('========================================');
}

// ── Test Scenarios for API Testing ─────────────────────────
export function apiLoadTest() {
  const endpoints = [
    { method: 'GET', url: '/api/products', weight: 40 },
    { method: 'GET', url: '/api/products/1', weight: 30 },
    { method: 'GET', url: '/api/categories', weight: 10 },
    { method: 'POST', url: '/api/products/1/vote', weight: 10 },
    { method: 'GET', url: '/api/products/trending', weight: 10 },
  ];
  
  // Weighted random endpoint selection
  const rand = Math.random() * 100;
  let cumulative = 0;
  let selectedEndpoint = endpoints[0];
  
  for (const endpoint of endpoints) {
    cumulative += endpoint.weight;
    if (rand <= cumulative) {
      selectedEndpoint = endpoint;
      break;
    }
  }
  
  // Make request
  const url = `${API_URL}${selectedEndpoint.url}`;
  let response;
  
  if (selectedEndpoint.method === 'POST') {
    response = http.post(url, JSON.stringify({ vote: 1 }), {
      headers: { 'Content-Type': 'application/json' },
    });
  } else {
    response = http.get(url);
  }
  
  // Validate
  check(response, {
    'status is 2xx': (r) => r.status >= 200 && r.status < 300,
    'response time OK': (r) => r.timings.duration < 500,
  });
  
  sleep(1);
}
