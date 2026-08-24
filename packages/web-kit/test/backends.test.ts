import { afterEach, describe, expect, it } from 'vitest';
import { candidates } from '../src/backends';

const ENV_KEYS = ['OFFERS_API_URL', 'services__offers__https__0', 'services__offers__http__0'];

afterEach(() => {
  for (const key of ENV_KEYS) {
    delete process.env[key];
  }
});

describe('the candidate ladder', () => {
  it('prefers the explicit env var over discovery and localhost', () => {
    process.env.OFFERS_API_URL = 'https://offers.example';
    process.env.services__offers__http__0 = 'http://discovered:8080';

    expect(candidates('offers')).toEqual([
      'https://offers.example',
      'http://discovered:8080',
      'http://localhost:8082',
    ]);
  });

  it('falls back to localhost with the service default port when nothing is configured', () => {
    expect(candidates('offers')).toEqual(['http://localhost:8082']);
  });

  it('keeps the aspire https rung ahead of the http rung', () => {
    process.env.services__offers__https__0 = 'https://discovered:8443';
    process.env.services__offers__http__0 = 'http://discovered:8080';

    expect(candidates('offers')[0]).toBe('https://discovered:8443');
  });
});
