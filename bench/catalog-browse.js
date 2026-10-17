// Catalog load at a fixed arrival rate, so runs with and without the cache (Caching:Enabled) compare latency at the
// same load. Needs a catalog to browse: start the AppHost with --Seed:VolumeProducts=50000.
import http from 'k6/http';
import { check } from 'k6';

const api = `${__ENV.BASE_URL || 'http://host.docker.internal:5080'}/api/v1/catalog`;
const terms = ['tenis', 'jaqueta', 'mochila', 'barraca', 'capacete', 'camiseta', 'bota', 'luva'];
const prefixes = ['te', 'Tên', 'jaq', 'MOCH', 'barr', 'cap', 'cami', 'luv'];
const categories = ['calcados', 'vestuario', 'camping', 'ciclismo'];

export const options = {
  scenarios: {
    browse: {
      executor: 'constant-arrival-rate',
      rate: Number(__ENV.RATE || 20),
      timeUnit: '1s',
      duration: __ENV.DURATION || '1m',
      preAllocatedVUs: 20,
      maxVUs: 200,
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    'http_req_duration{name:listing}': ['p(95)<500'],
    'http_req_duration{name:facets}': ['p(95)<500'],
    'http_req_duration{name:search}': ['p(95)<500'],
    'http_req_duration{name:suggestions}': ['p(95)<200'],
    'http_req_duration{name:product}': ['p(95)<200'],
  },
};

const pick = (items) => items[Math.floor(Math.random() * items.length)];

function get(path, name) {
  const response = http.get(`${api}${path}`, { tags: { name } });
  check(response, { [`${name} 200`]: (r) => r.status === 200 });
  return response;
}

export function setup() {
  const slugs = get('/products?limit=50', 'setup').json('items.#.slug');
  if (!slugs || slugs.length === 0) {
    throw new Error('The catalog is empty: start the AppHost with --Seed:VolumeProducts=50000.');
  }

  return { slugs };
}

export default function (data) {
  get('/products', 'listing');
  get('/facets', 'facets');
  get(`/products?q=${pick(terms)}&category=${pick(categories)}`, 'search');
  get(`/suggestions?q=${encodeURIComponent(pick(prefixes))}`, 'suggestions');
  get(`/products/${pick(data.slugs)}`, 'product');
}
