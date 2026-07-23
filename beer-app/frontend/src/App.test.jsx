import { act, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import App from './App';
import { AUTH_CHANGED_EVENT, fetchMyProgress, logout } from './lib/api';

// getRolesFromToken/AUTH_CHANGED_EVENT need their real implementations here (nav
// behavior IS the thing under test); only the network-touching calls are mocked.
vi.mock('./lib/api', async (importOriginal) => {
  const actual = await importOriginal();
  return { ...actual, fetchMyProgress: vi.fn(), logout: vi.fn() };
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

  it('shows Sign in and hides Add Beer when signed out', () => {
    renderApp();
    const nav = within(screen.getByRole('navigation'));

    expect(nav.getByRole('link', { name: 'Sign in' })).toBeInTheDocument();
    expect(nav.queryByRole('link', { name: 'Add Beer' })).not.toBeInTheDocument();
    expect(nav.queryByRole('button', { name: 'Sign out' })).not.toBeInTheDocument();
    expect(nav.queryByRole('link', { name: 'Linked accounts' })).not.toBeInTheDocument();
  });

  it('hides Add Beer for a signed-in customer, shows Linked accounts', () => {
    localStorage.setItem('beer-token', fakeJwt({ [roleClaim]: 'Customer' }));

    renderApp();
    const nav = within(screen.getByRole('navigation'));

    expect(nav.queryByRole('link', { name: 'Add Beer' })).not.toBeInTheDocument();
    expect(nav.getByRole('button', { name: 'Sign out' })).toBeInTheDocument();
    expect(nav.getByRole('link', { name: 'Linked accounts' })).toBeInTheDocument();
  });

  it('shows Add Beer for a signed-in admin', () => {
    localStorage.setItem('beer-token', fakeJwt({ [roleClaim]: 'Admin' }));

    renderApp();
    const nav = within(screen.getByRole('navigation'));

    expect(nav.getByRole('link', { name: 'Add Beer' })).toBeInTheDocument();
  });

  it('reacts to the auth-changed event without a page reload', async () => {
    renderApp();
    const nav = within(screen.getByRole('navigation'));
    expect(nav.getByRole('link', { name: 'Sign in' })).toBeInTheDocument();

    localStorage.setItem('beer-token', fakeJwt({ [roleClaim]: 'Admin' }));
    act(() => {
      window.dispatchEvent(new Event(AUTH_CHANGED_EVENT));
    });

    await waitFor(() => expect(nav.getByRole('button', { name: 'Sign out' })).toBeInTheDocument());
    expect(nav.getByRole('link', { name: 'Add Beer' })).toBeInTheDocument();
  });

  it('clicking Sign out calls logout', async () => {
    const user = userEvent.setup();
    localStorage.setItem('beer-token', fakeJwt({ [roleClaim]: 'Customer' }));
    logout.mockImplementation(() => {
      localStorage.removeItem('beer-token');
      window.dispatchEvent(new Event(AUTH_CHANGED_EVENT));
    });

    renderApp();
    const nav = within(screen.getByRole('navigation'));
    await user.click(nav.getByRole('button', { name: 'Sign out' }));

    expect(logout).toHaveBeenCalled();
    expect(nav.getByRole('link', { name: 'Sign in' })).toBeInTheDocument();
  });
});
