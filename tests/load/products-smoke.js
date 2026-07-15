import http from "k6/http";
import { check, sleep } from "k6";

const baseUrl = __ENV.BASE_URL || "http://host.docker.internal:5000";

export const options = {
  scenarios: {
    products_read_smoke: {
      executor: "constant-vus",
      vus: Number(__ENV.VUS || 5),
      duration: __ENV.DURATION || "15s",
      gracefulStop: "5s",
    },
  },
  thresholds: {
    http_req_failed: ["rate<0.01"],
    http_req_duration: ["p(95)<750"],
    checks: ["rate>0.99"],
  },
};

export default function () {
  const response = http.get(`${baseUrl}/api/products?limit=20`, {
    tags: { endpoint: "GET /api/products" },
  });

  check(response, {
    "status is 200": result => result.status === 200,
    "response is JSON": result => (result.headers["Content-Type"] || "").includes("application/json"),
    "response contains items": result => {
      try {
        return Array.isArray(result.json("items"));
      } catch {
        return false;
      }
    },
  });

  sleep(0.25);
}
