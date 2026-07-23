import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it } from 'vitest';
import AuthCallback from './AuthCallback';
import { AUTH_CHANGED_EVENT } from '../lib/api';

function renderAuthCallback(search) {
  return render(
    <MemoryRouter initialEntries={[`/auth/callback${search}`]}>
      <Routes>
        <Route path="/auth/callback" element={<AuthCallback />} />
        <Route path="/" element={<div>Home page</div>} />
      </Routes>
    </MemoryRouter>
  );
}

describe('AuthCallback', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('stores the token, dispatches the auth-changed event, and redirects home', async () => {
    let eventFired = false;
    window.addEventListener(AUTH_CHANGED_EVENT, () => {
      eventFired = true;
    });

    renderAuthCallback('?token=jwt-token-value');

    expect(await screen.findByText('Home page')).toBeInTheDocument();
    expect(localStorage.getItem('beer-token')).toBe('jwt-token-value');
    expect(eventFired).toBe(true);
  });

  it('shows a failure message and a link back to sign in when given an error', () => {
    renderAuthCallback('?error=email_not_verified');

    expect(screen.getByRole('heading', { name: 'Sign-in failed' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Back to sign in' })).toBeInTheDocument();
    expect(localStorage.getItem('beer-token')).toBeNull();
  });
});
