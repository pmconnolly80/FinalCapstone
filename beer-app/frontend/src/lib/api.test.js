import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  AUTH_CHANGED_EVENT,
  assignRole,
  confirmBeer,
  deactivateAccount,
  deactivateStaffPin,
  deleteBeer,
  fetchAdminConfirmations,
  fetchBeer,
  fetchConfirmationAudits,
  fetchMyProgress,
  getAdminUsers,
  getRolesFromToken,
  issueOrResetStaffPin,
  login,
  logout,
  reactivateAccount,
  register,
  saveBeer,
  searchBeers,
  searchBreweries,
  searchCatalogBeer,
  setMyPin,
  updateBeerAvailability,
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

  it('updateBeerAvailability PATCHes the availability, and surfaces API errors', async () => {
    localStorage.setItem('beer-token', 'abc123');
    mockFetchOnce(true, null);

    await updateBeerAvailability(5, 'OutOfStock');

    const [url, init] = global.fetch.mock.calls[0];
    expect(url).toContain('/api/beers/5/availability');
    expect(init.method).toBe('PATCH');
    expect(init.headers.Authorization).toBe('Bearer abc123');
    expect(JSON.parse(init.body)).toEqual({ availability: 'OutOfStock' });

    mockFetchOnce(false, { message: 'Beer not found.' });
    await expect(updateBeerAvailability(5, 'OutOfStock')).rejects.toThrow('Beer not found.');
  });

  it('deleteBeer DELETEs with the reason in the query string, and surfaces API errors', async () => {
    localStorage.setItem('beer-token', 'abc123');
    mockFetchOnce(true, null);

    await deleteBeer(5, 'discontinued by brewery');

    const [url, init] = global.fetch.mock.calls[0];
    expect(url).toContain('/api/beers/5?reason=discontinued%20by%20brewery');
    expect(init.method).toBe('DELETE');
    expect(init.headers.Authorization).toBe('Bearer abc123');

    mockFetchOnce(false, { message: 'A reason is required to delete a beer.' });
    await expect(deleteBeer(5, '')).rejects.toThrow('A reason is required to delete a beer.');
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

  it('getAdminUsers GETs with the Authorization header', async () => {
    localStorage.setItem('beer-token', 'abc123');
    mockFetchOnce(true, [{ id: 'u1', email: 'a@example.com', role: 'Customer' }]);

    const users = await getAdminUsers();

    const [url, init] = global.fetch.mock.calls[0];
    expect(url).toContain('/api/admin/users');
    expect(init.headers.Authorization).toBe('Bearer abc123');
    expect(users[0].email).toBe('a@example.com');
  });

  it('assignRole PUTs the role and reason, and surfaces API errors', async () => {
    localStorage.setItem('beer-token', 'abc123');
    mockFetchOnce(true, null);

    await assignRole('u1', 'Bartender', 'promoted to staff');

    const [url, init] = global.fetch.mock.calls[0];
    expect(url).toContain('/api/admin/users/u1/role');
    expect(init.method).toBe('PUT');
    expect(JSON.parse(init.body)).toEqual({ role: 'Bartender', reason: 'promoted to staff' });

    mockFetchOnce(false, { message: 'Invalid role.' });
    await expect(assignRole('u1', 'SuperAdmin', 'x')).rejects.toThrow('Invalid role.');
  });

  it('deactivateAccount POSTs the reason, and surfaces API errors', async () => {
    localStorage.setItem('beer-token', 'abc123');
    mockFetchOnce(true, null);

    await deactivateAccount('u1', 'policy violation');

    const [url, init] = global.fetch.mock.calls[0];
    expect(url).toContain('/api/admin/users/u1/deactivate');
    expect(init.method).toBe('POST');
    expect(JSON.parse(init.body)).toEqual({ reason: 'policy violation' });

    mockFetchOnce(false, { message: 'A reason is required to deactivate an account.' });
    await expect(deactivateAccount('u1', '')).rejects.toThrow(
      'A reason is required to deactivate an account.'
    );
  });

  it('reactivateAccount POSTs the reason, and surfaces API errors', async () => {
    localStorage.setItem('beer-token', 'abc123');
    mockFetchOnce(true, null);

    await reactivateAccount('u1', 'appeal approved');

    const [url, init] = global.fetch.mock.calls[0];
    expect(url).toContain('/api/admin/users/u1/reactivate');
    expect(init.method).toBe('POST');
    expect(JSON.parse(init.body)).toEqual({ reason: 'appeal approved' });

    mockFetchOnce(false, { message: 'User not found.' });
    await expect(reactivateAccount('u1', 'x')).rejects.toThrow('User not found.');
  });

  it('issueOrResetStaffPin PUTs the pin, and surfaces API errors', async () => {
    localStorage.setItem('beer-token', 'abc123');
    mockFetchOnce(true, null);

    await issueOrResetStaffPin('u1', '135790');

    const [url, init] = global.fetch.mock.calls[0];
    expect(url).toContain('/api/staff-pins/u1');
    expect(init.method).toBe('PUT');
    expect(JSON.parse(init.body)).toEqual({ pin: '135790' });

    mockFetchOnce(false, { message: 'That PIN is already in use by another staff member.' });
    await expect(issueOrResetStaffPin('u1', '135790')).rejects.toThrow(
      'That PIN is already in use by another staff member.'
    );
  });

  it('deactivateStaffPin DELETEs with the Authorization header, and surfaces API errors', async () => {
    localStorage.setItem('beer-token', 'abc123');
    mockFetchOnce(true, null);

    await deactivateStaffPin('u1');

    const [url, init] = global.fetch.mock.calls[0];
    expect(url).toContain('/api/staff-pins/u1');
    expect(init.method).toBe('DELETE');
    expect(init.headers.Authorization).toBe('Bearer abc123');

    mockFetchOnce(false, { message: 'No PIN exists for that user.' });
    await expect(deactivateStaffPin('u1')).rejects.toThrow('No PIN exists for that user.');
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

  it('searchCatalogBeer GETs with the query string and Authorization header', async () => {
    localStorage.setItem('beer-token', 'abc123');
    mockFetchOnce(true, [{ id: 'cb-1', name: 'Duvel' }]);

    const results = await searchCatalogBeer('duvel');

    const [url, init] = global.fetch.mock.calls[0];
    expect(url).toContain('/api/catalog-beer/search?query=duvel');
    expect(init.headers.Authorization).toBe('Bearer abc123');
    expect(results).toEqual([{ id: 'cb-1', name: 'Duvel' }]);
  });

  it('searchCatalogBeer throws when the response is not ok', async () => {
    mockFetchOnce(false, {});

    await expect(searchCatalogBeer('duvel')).rejects.toThrow('Failed to search Catalog.beer');
  });

  it('logout removes the token and dispatches the auth-changed event', () => {
    localStorage.setItem('beer-token', 'abc123');
    const handler = vi.fn();
    window.addEventListener(AUTH_CHANGED_EVENT, handler);

    logout();

    expect(localStorage.getItem('beer-token')).toBeNull();
    expect(handler).toHaveBeenCalledTimes(1);
    window.removeEventListener(AUTH_CHANGED_EVENT, handler);
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
