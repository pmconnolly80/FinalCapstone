import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import Account from './Account';
import { logout } from '../lib/api';

vi.mock('../lib/api', async (importOriginal) => {
  const actual = await importOriginal();
  return { ...actual, logout: vi.fn() };
});

function fakeJwt(payload) {
  return `header.${btoa(JSON.stringify(payload))}.signature`;
}

const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

function renderAccount() {
  return render(
    <MemoryRouter>
      <Account />
    </MemoryRouter>
  );
}

describe('Account', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    vi.resetAllMocks();
  });

  it('shows Linked accounts and Privacy policy for a plain customer, hides My PIN and admin links', () => {
    localStorage.setItem('beer-token', fakeJwt({ [roleClaim]: 'Customer' }));

    renderAccount();

    expect(screen.getByRole('link', { name: 'Linked accounts' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Privacy policy' })).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'My PIN' })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Dashboard' })).not.toBeInTheDocument();
  });

  it('shows My PIN for a bartender', () => {
    localStorage.setItem('beer-token', fakeJwt({ [roleClaim]: 'Bartender' }));

    renderAccount();

    expect(screen.getByRole('link', { name: 'My PIN' })).toBeInTheDocument();
  });

  it('shows admin links and My PIN for an admin', () => {
    localStorage.setItem('beer-token', fakeJwt({ [roleClaim]: 'Admin' }));

    renderAccount();

    expect(screen.getByRole('link', { name: 'My PIN' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Dashboard' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Confirmations' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Users' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Manage Beers' })).toBeInTheDocument();
  });

  it('signs out and navigates home', async () => {
    localStorage.setItem('beer-token', fakeJwt({ [roleClaim]: 'Customer' }));
    const user = userEvent.setup();

    renderAccount();
    await user.click(screen.getByRole('button', { name: 'Sign out' }));

    expect(logout).toHaveBeenCalled();
  });
});
