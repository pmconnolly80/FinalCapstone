import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import MyProgress from './MyProgress';
import { fetchMyProgress } from '../lib/api';

vi.mock('../lib/api');

function renderMyProgress() {
  return render(
    <MemoryRouter>
      <MyProgress />
    </MemoryRouter>
  );
}

describe('MyProgress', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    vi.resetAllMocks();
  });

  it('prompts for sign-in when no token is stored', () => {
    renderMyProgress();

    expect(screen.getByText('Sign in')).toBeInTheDocument();
    expect(fetchMyProgress).not.toHaveBeenCalled();
  });

  it('shows the count, progress bar, and confirmed beers', async () => {
    localStorage.setItem('beer-token', 'abc');
    fetchMyProgress.mockResolvedValue({
      confirmedCount: 2,
      goal: 200,
      mugEarned: false,
      confirmations: [
        { beerId: 1, name: 'Duvel', brewery: 'Duvel Moortgat', style: 'Belgian Strong Golden Ale', confirmedAt: '2026-07-14T20:00:00Z' },
        { beerId: 2, name: 'Pale Ale', brewery: 'Sierra Nevada', style: 'American Pale Ale', confirmedAt: '2026-07-01T20:00:00Z' },
      ],
    });

    renderMyProgress();

    expect(await screen.findByText('2 of 200')).toBeInTheDocument();
    expect(screen.getByRole('progressbar')).toHaveAttribute('aria-valuenow', '2');
    expect(screen.getByText('Duvel')).toBeInTheDocument();
    expect(screen.getByText('Pale Ale')).toBeInTheDocument();
    expect(screen.queryByText(/Mug earned/)).not.toBeInTheDocument();
  });

  it('shows the mug-earned state with the earned date', async () => {
    localStorage.setItem('beer-token', 'abc');
    fetchMyProgress.mockResolvedValue({
      confirmedCount: 200,
      goal: 200,
      mugEarned: true,
      mugEarnedAt: '2026-07-15T21:00:00Z',
      confirmations: [],
    });

    renderMyProgress();

    expect(await screen.findByText(/Mug earned/)).toBeInTheDocument();
    expect(screen.getByText(/7\/15\/2026/)).toBeInTheDocument();
  });

  it('shows an empty-state nudge when nothing is confirmed yet', async () => {
    localStorage.setItem('beer-token', 'abc');
    fetchMyProgress.mockResolvedValue({ confirmedCount: 0, goal: 200, mugEarned: false, confirmations: [] });

    renderMyProgress();

    expect(await screen.findByText(/Nothing confirmed yet/)).toBeInTheDocument();
  });

  it('shows an error message when loading fails', async () => {
    localStorage.setItem('beer-token', 'abc');
    fetchMyProgress.mockRejectedValue(new Error('boom'));

    renderMyProgress();

    expect(await screen.findByText(/Could not load your progress/)).toBeInTheDocument();
  });
});
