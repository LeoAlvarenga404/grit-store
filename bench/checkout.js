// Checkout load: one customer per VU (a shared basket would change the subtotal under another VU's order).
// Needs ADMIN_PASSWORD (bench/run.sh reads it from the AppHost user secrets) to raise the stock first.
import http from 'k6/http';
import { check, fail, sleep } from 'k6';
import exec from 'k6/execution';

const api = `${__ENV.BASE_URL || 'http://host.docker.internal:5080'}/api/v1`;
const customers = Number(__ENV.CUSTOMERS || 5);
const password = 'Senha-de-carga-123!';

export const options = {
  setupTimeout: '2m',
  scenarios: {
    checkout: {
      executor: 'per-vu-iterations',
      vus: customers,
      iterations: Number(__ENV.ITERATIONS || 20),
      maxDuration: '5m',
    },
  },
  thresholds: {
    checks: ['rate>0.95'],
    'http_req_duration{name:add-line}': ['p(95)<300'],
    'http_req_duration{name:place-order}': ['p(95)<800'],
  },
};

function send(method, path, body, token, name, extraHeaders) {
  const headers = Object.assign({ 'Content-Type': 'application/json' }, extraHeaders);
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  return http.request(method, `${api}${path}`, body === null ? null : JSON.stringify(body), { headers, tags: { name } });
}

function logIn(email, secret) {
  const response = send('POST', '/identity/login', { email, password: secret }, null, 'login');
  if (response.status !== 200) {
    fail(`login ${email}: ${response.status}`);
  }

  return response.json('accessToken');
}

export function setup() {
  if (!__ENV.ADMIN_PASSWORD) {
    fail('ADMIN_PASSWORD is required: run bench/run.sh, which reads it from the AppHost user secrets.');
  }

  const admin = logIn('admin@grit.local', __ENV.ADMIN_PASSWORD);
  const slug = send('GET', '/catalog/products?limit=1', null, null, 'setup').json('items.0.slug');
  const skuId = send('GET', `/catalog/products/${slug}`, null, null, 'setup').json('skus.0.id');

  // Every order holds its unit until it ships, so the run starts with more stock than it can sell.
  const stock = send('PUT', `/admin/stock/${skuId}`, { onHand: 1000000 }, admin, 'setup');
  if (stock.status !== 204) {
    fail(`set stock: ${stock.status}`);
  }

  const tokens = [];
  for (let customer = 0; customer < customers; customer++) {
    const email = `k6-${Date.now()}-${customer}@grit.dev`;
    const registered = send('POST', '/identity/register', { email, password }, null, 'setup');
    if (registered.status !== 201) {
      fail(`register: ${registered.status}`);
    }

    tokens.push(logIn(email, password));
  }

  return { skuId, tokens };
}

export default function (data) {
  const token = data.tokens[exec.vu.idInTest - 1];
  const added = send('POST', '/basket/lines', { skuId: data.skuId, quantity: 1 }, token, 'add-line');
  check(added, { 'line added': (r) => r.status === 204 });

  const subtotal = send('GET', '/basket', null, token, 'basket').json('subtotalAmount');
  const placed = send(
    'POST',
    '/orders',
    { paymentToken: 'tok_visa', expectedSubtotalAmount: subtotal },
    token,
    'place-order',
    { 'Idempotency-Key': `k6-${exec.vu.idInTest}-${exec.vu.iterationInScenario}-${Date.now()}` },
  );
  if (!check(placed, { 'order placed': (r) => r.status === 201 })) {
    return;
  }

  const orderId = placed.json('orderId');
  let status = 'Placed';
  for (let attempt = 0; attempt < 20 && status === 'Placed'; attempt++) {
    sleep(0.25);
    status = send('GET', `/orders/${orderId}`, null, token, 'order').json('status');
  }

  check(status, { 'order confirmed': (value) => value === 'Confirmed' });
}
