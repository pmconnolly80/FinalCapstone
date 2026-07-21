import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  confirmBeer,
  fetchAdminConfirmations,
  fetchBeer,
  fetchConfirmationAudits,
  fetchMyProgress,
  getRolesFromToken,
  login,
  register,
  saveBeer,
  searchBeers,
  searchBreweries,
  setMyPin,
  voidConfirmation,
} from './api';

function fakeJwt(payload) {
  return `header.${btoa(JSON.stringify(payload))}.signature`;
}

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

  it('searchBeers GETs /api/beers with no query string when called with no params', async () => {
    mockFetchOnce(true, { items: [], page: 1, pageSize: 200, totalCount: 0 });

    await searchBeers();

    const [url] = global.fetch.mock.calls[0];
    expect(url).toMatch(/\/api\/beers$/);
  });

  it('searchBeers builds a query string from provided params, omitting blank ones', async () => {
    mockFetchOnce(true, { items: [], page: 1, pageSize: 200, totalCount: 0 });

    await searchBeers({ search: 'ipa', availability: '', hadStatus: undefined, page: 2 });

    const [url] = global.fetch.mock.calls[0];
    expect(url).toContain('search=ipa');
    expect(url).toContain('page=2');
    expect(url).not.toContain('availability');
    expect(url).not.toContain('hadStatus');
  });

  it('searchBeers includes the Authorization header when a token is stored', async () => {
    localStorage.setItem('beer-token', 'abc123');
    mockFetchOnce(true, { items: [], page: 1, pageSize: 200, totalCount: 0 });

    await searchBeers({ hadStatus: 'had' });

    const [, init] = global.fetch.mock.calls[0];
    expect(init.headers.Authorization).toBe('Bearer abc123');
  });

  it('searchBeers resolves with the full paginated response', async () => {
    const body = { items: [{ id: 1, name: 'Pale Ale' }], page: 1, pageSize: 200, totalCount: 1 };
    mockFetchOnce(true, body);

    const result = await searchBeers();

    expect(result).toEqual(body);
  });

  it('searchBeers throws when the response is not ok', async () => {
    mockFetchOnce(false, {});

    await expect(searchBeers()).rejects.toThrow('Failed to load beers');
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

  it('setMyPin PUTs the pin with the Authorization header', async () => {
    localStorage.setItem('beer-token', 'abc123');
    mockFetchOnce(true, null);

    await setMyPin('654321');

    const [url, init] = global.fetch.mock.calls[0];
    expect(url).toContain('/api/staff-pins/me');
    expect(init.method).toBe('PUT');
    expect(init.headers.Authorization).toBe('Bearer abc123');
    expect(JSON.parse(init.body)).toEqual({ pin: '654321' });
  });

  it('setMyPin surfaces the API error message when the response is not ok', async () => {
    mockFetchOnce(false, { message: 'That PIN is already in use by another staff member.' });

    await expect(setMyPin('654321')).rejects.toThrow(
      'That PIN is already in use by another staff member.'
    );
  });

  it('fetchAdminConfirmations GETs with the Authorization header', async () => {
    localStorage.setItem('beer-token', 'abc123');
    mockFetchOnce(true, [{ id: 1, beerName: 'Duvel' }]);

    const rows = await fetchAdminConfirmations();

    const [url, init] = global.fetch.mock.calls[0];
    expect(url).toContain('/api/admin/confirmations');
    expect(init.headers.Authorization).toBe('Bearer abc123');
    expect(rows[0].beerName).toBe('Duvel');
  });

  it('fetchConfirmationAudits GETs the audits list', async () => {
    localStorage.setItem('beer-token', 'abc123');
    mockFetchOnce(true, [{ id: 1, reason: 'wrong beer' }]);

    const audits = await fetchConfirmationAudits();

    const [url] = global.fetch.mock.calls[0];
    expect(url).toContain('/api/admin/confirmations/audits');
    expect(audits[0].reason).toBe('wrong beer');
  });

  it('voidConfirmation POSTs the reason and surfaces API errors', async () => {
    localStorage.setItem('beer-token', 'abc123');
    mockFetchOnce(true, null);

    await voidConfirmation(7, 'wrong customer');

    const [url, init] = global.fetch.mock.calls[0];
    expect(url).toContain('/api/admin/confirmations/7/void');
    expect(init.method).toBe('POST');
    expect(JSON.parse(init.body)).toEqual({ reason: 'wrong customer' });

    mockFetchOnce(false, { message: 'A reason is required to void a confirmation.' });
    await expect(voidConfirmation(7, '')).rejects.toThrow(
      'A reason is required to void a confirmation.'
    );
  });

  it('searchBreweries GETs with the query string and Authorization header', async () => {
    localStorage.setItem('beer-token', 'abc123');
    mockFetchOnce(true, [{ id: 'obdb-1', name: 'Sierra Nevada Brewing Co' }]);

    const results = await searchBreweries('sierra');

    const [url, init] = global.fetch.mock.calls[0];
    expect(url).toContain('/api/breweries/search?query=sierra');
    expect(init.headers.Authorization).toBe('Bearer abc123');
    expect(results).toEqual([{ id: 'obdb-1', name: 'Sierra Nevada Brewing Co' }]);
  });

  it('searchBreweries throws when the response is not ok', async () => {
    mockFetchOnce(false, {});

    await expect(searchBreweries('sierra')).rejects.toThrow('Failed to search breweries');
  });

  it('getRolesFromToken reads the role claim, tolerating strings, arrays, and junk', () => {
    const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

    localStorage.setItem('beer-token', fakeJwt({ [roleClaim]: 'Admin' }));
    expect(getRolesFromToken()).toEqual(['Admin']);

    localStorage.setItem('beer-token', fakeJwt({ [roleClaim]: ['Bartender', 'Admin'] }));
    expect(getRolesFromToken()).toEqual(['Bartender', 'Admin']);

    localStorage.setItem('beer-token', fakeJwt({}));
    expect(getRolesFromToken()).toEqual([]);

    localStorage.setItem('beer-token', 'not-a-jwt');
    expect(getRolesFromToken()).toEqual([]);

    localStorage.removeItem('beer-token');
    expect(getRolesFromToken()).toEqual([]);
  });
});
