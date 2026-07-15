import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { confirmBeer, fetchBeer, fetchBeers, fetchMyProgress, login, register, saveBeer } from './api';

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

  it('register surfaces the API error message when the response is not ok', async () => {
    mockFetchOnce(false, { message: 'Passwords must be at least 8 characters.' });

    await expect(register('a@example.com', 'beer123')).rejects.toThrow(
      'Passwords must be at least 8 characters.'
    );
  });

  it('login surfaces the API error message when the response is not ok', async () => {
    mockFetchOnce(false, { message: 'Invalid credentials.' });

    await expect(login('a@example.com', 'wrong')).rejects.toThrow('Invalid credentials.');
  });

  it('confirmBeer POSTs the beer id and PIN with the Authorization header', async () => {
    localStorage.setItem('beer-token', 'abc123');
    mockFetchOnce(true, { confirmedCount: 1, goal: 200, mugEarned: false });

    const result = await confirmBeer(7, '123456');

    const [url, init] = global.fetch.mock.calls[0];
    expect(url).toContain('/api/confirmations');
    expect(init.method).toBe('POST');
    expect(init.headers.Authorization).toBe('Bearer abc123');
    expect(JSON.parse(init.body)).toEqual({ beerId: 7, pin: '123456' });
    expect(result.confirmedCount).toBe(1);
  });

  it('confirmBeer surfaces the API error message when the response is not ok', async () => {
    mockFetchOnce(false, { message: 'Invalid PIN.' });

    await expect(confirmBeer(7, '000000')).rejects.toThrow('Invalid PIN.');
  });

  it('fetchMyProgress GETs with the Authorization header and resolves the body', async () => {
    localStorage.setItem('beer-token', 'abc123');
    mockFetchOnce(true, { confirmedCount: 3, goal: 200, mugEarned: false, confirmations: [] });

    const result = await fetchMyProgress();

    const [url, init] = global.fetch.mock.calls[0];
    expect(url).toContain('/api/me/progress');
    expect(init.headers.Authorization).toBe('Bearer abc123');
    expect(result.confirmedCount).toBe(3);
  });

  it('fetchMyProgress throws when the response is not ok', async () => {
    mockFetchOnce(false, {});

    await expect(fetchMyProgress()).rejects.toThrow('Failed to load progress');
  });
});
