import http from 'k6/http';
import { check, sleep } from 'k6';

// Charge progressive : montée à 10 VUs, plateau 1 min, descente
export const options = {
  stages: [
    { duration: '30s', target: 10 },
    { duration: '1m', target: 10 },
    { duration: '30s', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'], // 95 % des requêtes < 2 s
    http_req_failed: ['rate<0.01'],     // < 1 % d'échecs
  },
};

const API = __ENV.API_URL || 'http://api:8080';

// Fichier 100 Ko généré en mémoire
const payload = 'A'.repeat(100 * 1024);

export default function () {
  const form = {
    file: http.file(payload, `load-${__VU}-${__ITER}.txt`, 'text/plain'),
    expiresInDays: '1',
  };

  const res = http.post(`${API}/api/files`, form);

  check(res, {
    'status 201': (r) => r.status === 201,
    'body has downloadToken': (r) => {
      try { return JSON.parse(r.body).downloadToken?.length > 0; }
      catch { return false; }
    },
  });

  sleep(0.5);
}
