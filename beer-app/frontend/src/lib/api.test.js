import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { fetchBeer, fetchBeers, login, register, saveBeer } from './api';

function mockFetchOnce(ok, body) {
  global.fetch = vi.fn().mockResolvedValue({
    ok,
    json: () => Promise.resolve(body),
  });
}

describe('api', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('fetchBeers resolves with the response body on success', async () => {
    mockFetchOnce(true, [{ id: 1, name: 'Pale Ale' }]);

    const beers = await fetchBeers();

    expect(beers).toEqual([{ id: 1, name: 'Pale Ale' }]);
  });

  it('fetchBeers throws when the response is not ok', async () => {
    mockFetchOnce(false, {});

    await expect(fetchBeers()).rejects.toThrow('Failed to load beers');
  });

  it('fetchBeer throws when the response is not ok', async () => {
    mockFetchOnce(false, {});

    await expect(fetchBeer(1)).rejects.toThrow('Failed to load beer');
  });

  it('saveBeer POSTs without an id and omits the Authorization header when no token is stored', async () => {
    mockFetchOnce(true, { id: 1 });

    await saveBeer({ name: 'New Beer' });

    const [, init] = global.fetch.mock.calls[0];
    expect(init.method).toBe('POST');
    expect(init.headers.Authorization).toBeUndefined();
  });

  it('saveBeer PUTs with an id and includes the Authorization header when a token is stored', async () => {
    localStorage.setItem('beer-token', 'abc123');
    mockFetchOnce(true, { id: 5 });

    await saveBeer({ name: 'Updated Beer' }, 5);

    const [url, init] = global.fetch.mock.calls[0];
    expect(url).toContain('/api/beers/5');
    expect(init.method).toBe('PUT');
    expect(init.headers.Authorization).toBe('Bearer abc123');
  });

  it('login resolves with the response body on success', async () => {
    mockFetchOnce(true, { token: 'abc', email: 'a@example.com' });

    const result = await login('a@example.com', 'password');

    expect(result).toEqual({ token: 'abc', email: 'a@example.com' });
  });

  it('login throws when the response is not ok', async () => {
    mockFetchOnce(false, {});

    await expect(login('a@example.com', 'wrong')).rejects.toThrow('Login failed');
  });

  it('register throws when the response is not ok', async () => {
    mockFetchOnce(false, {});

    await expect(register('a@example.com', 'password')).rejects.toThrow('Registration failed');
  });
});
