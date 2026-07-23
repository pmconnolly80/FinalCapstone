import { act, render, screen, waitFor, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import App from './App';
import { AUTH_CHANGED_EVENT, fetchMyProgress } from './lib/api';

// getRolesFromToken/AUTH_CHANGED_EVENT need their real implementations here (nav
// behavior IS the thing under test); only the network-touching calls are mocked.
vi.mock('./lib/api', async (importOriginal) => {
  const actual = await importOriginal();
  return { ...actual, fetchMyProgress: vi.fn() };
});

function fakeJwt(payload) {
  return `header.${btoa(JSON.stringify(payload))}.signature`;
}

const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

function renderApp() {
  return render(
    <MemoryRouter initialEntries={['/']}>
      <App />
    </MemoryRouter>
  );
}

describe('App nav', () => {
  beforeEach(() => {
    localStorage.clear();
    fetchMyProgress.mockResolvedValue({ confirmedCount: 0, goal: 200, mugEarned: false, confirmations: [] });
  });

  afterEach(() => {
    vi.resetAllMocks();
  });

  it('shows a privacy policy link in the footer', () => {
    renderApp();

    expect(screen.getByRole('link', { name: 'Privacy policy' })).toBeInTheDocument();
  });

  it('renders no bottom tab bar when signed out', () => {
    renderApp();

    expect(screen.queryByRole('navigation')).not.toBeInTheDocument();
  });

  it('shows the bottom tab bar for a signed-in customer, with Home/Beers/My Progress/Account only', () => {
    localStorage.setItem('beer-token', fakeJwt({ [roleClaim]: 'Customer' }));

    renderApp();
    const nav = within(screen.getByRole('navigation'));

    expect(nav.getByRole('link', { name: 'Home' })).toBeInTheDocument();
    expect(nav.getByRole('link', { name: 'Beers' })).toBeInTheDocument();
    expect(nav.getByRole('link', { name: 'My Progress' })).toBeInTheDocument();
    expect(nav.getByRole('link', { name: 'Account' })).toBeInTheDocument();
    expect(nav.queryByRole('link', { name: 'Manage Beers' })).not.toBeInTheDocument();
    expect(nav.queryByRole('button', { name: 'Sign out' })).not.toBeInTheDocument();
  });

  it('shows the same four tabs for a signed-in admin (admin links live under Account, not the tab bar)', () => {
    localStorage.setItem('beer-token', fakeJwt({ [roleClaim]: 'Admin' }));

    renderApp();
    const nav = within(screen.getByRole('navigation'));

    expect(nav.getByRole('link', { name: 'Account' })).toBeInTheDocument();
    expect(nav.queryByRole('link', { name: 'Manage Beers' })).not.toBeInTheDocument();
  });

  it('reacts to the auth-changed event without a page reload', async () => {
    renderApp();
    expect(screen.queryByRole('navigation')).not.toBeInTheDocument();

    localStorage.setItem('beer-token', fakeJwt({ [roleClaim]: 'Admin' }));
    act(() => {
      window.dispatchEvent(new Event(AUTH_CHANGED_EVENT));
    });

    await waitFor(() => expect(screen.getByRole('navigation')).toBeInTheDocument());
  });
});
