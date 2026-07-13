const http = require('http');

async function test() {
  const loginRes = await fetch("http://localhost:5000/api/auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email: "admin@vitrin.app", password: "Admin1234!" })
  });
  const token = await loginRes.json();

  const getRes = await fetch("http://localhost:5000/api/auth/admin/maker-applications", {
    method: "GET",
    headers: { "Authorization": `Bearer ${token}` }
  });
  console.log("Status: ", getRes.status);
  console.log("X-Debug-Claims: ", getRes.headers.get("X-Debug-Claims"));
  const data = await getRes.text();
  console.log("Data: ", data);
}

test();
