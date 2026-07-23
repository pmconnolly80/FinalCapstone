import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import Home from './Home';
import { fetchMyProgress, getRolesFromToken } from '../lib/api';

vi.mock('../lib/api');

function renderHome() {
  return render(
    <MemoryRouter>
      <Home />
    </MemoryRouter>
  );
}

function renderHomeWithDashboardRoute() {
  return render(
    <MemoryRouter initialEntries={['/']}>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/admin/dashboard" element={<div>Admin Dashboard Page</div>} />
      </Routes>
    </MemoryRouter>
  );
}

describe('Home', () => {
  beforeEach(() => {
    localStorage.clear();
    getRolesFromToken.mockReturnValue([]);
  });

  afterEach(() => {
    vi.resetAllMocks();
  });

  it('pitches the mug club to a signed-out visitor, not a generic catalog', () => {
    renderHome();

    expect(
      screen.getByRole('heading', { name: /drink the list\. earn your mug\./i })
    ).toBeInTheDocument();
    expect(screen.getByText(/bartender confirms/i)).toBeInTheDocument();
  });

  it('links a signed-out visitor to progress, the beer list, and sign-in', () => {
    renderHome();

    expect(screen.getByRole('link', { name: /my progress/i })).toHaveAttribute(
      'href',
      '/progress'
    );
    expect(
      screen.getByRole('link', { name: /browse the beer list/i })
    ).toHaveAttribute('href', '/beers');
    expect(screen.getByRole('link', { name: /sign in/i })).toHaveAttribute(
      'href',
      '/auth'
    );
  });

  it('shows the signed-in customer their actual progress instead of the generic pitch', async () => {
    localStorage.setItem('beer-token', 'abc123');
    fetchMyProgress.mockResolvedValue({
      confirmedCount: 42,
      goal: 200,
      mugEarned: false,
      confirmations: [],
    });

    renderHome();

    expect(await screen.findByText('42 of 200')).toBeInTheDocument();
    expect(
      screen.queryByRole('heading', { name: /drink the list\. earn your mug\./i })
    ).not.toBeInTheDocument();
    expect(screen.getByRole('link', { name: /browse the beer list/i })).toHaveAttribute(
      'href',
      '/beers'
    );
  });

  it("shows the signed-in customer's mug-earned state", async () => {
    localStorage.setItem('beer-token', 'abc123');
    fetchMyProgress.mockResolvedValue({
      confirmedCount: 200,
      goal: 200,
      mugEarned: true,
      mugEarnedAt: '2026-07-15T21:00:00Z',
      confirmations: [],
    });

    renderHome();

    expect(await screen.findByText(/mug earned/i)).toBeInTheDocument();
  });

  it('shows a visible error when progress fails to load', async () => {
    localStorage.setItem('beer-token', 'abc123');
    fetchMyProgress.mockRejectedValue(new Error('Failed to load progress'));

    renderHome();

    expect(
      await screen.findByText('Could not load your progress. Try signing in again.')
    ).toBeInTheDocument();
  });

  it('redirects a signed-in admin to the admin dashboard instead of showing progress', async () => {
    localStorage.setItem('beer-token', 'abc123');
    getRolesFromToken.mockReturnValue(['Admin']);

    renderHomeWithDashboardRoute();

    expect(await screen.findByText('Admin Dashboard Page')).toBeInTheDocument();
    expect(fetchMyProgress).not.toHaveBeenCalled();
  });
});
