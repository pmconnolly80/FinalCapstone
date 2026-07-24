import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import AdminRecommendations from './AdminRecommendations';
import { fetchAdminRecommendations, getRolesFromToken, updateRecommendationStatus } from '../lib/api';

vi.mock('../lib/api');

const recommendations = [
  { id: 1, customerId: 'c1', customerEmail: 'customer@example.com', beerName: 'Duvel', breweryName: 'Duvel Moortgat', note: 'Great beer', status: 'New' },
  { id: 2, customerId: 'c2', customerEmail: 'other@example.com', beerName: 'Orval', breweryName: null, note: null, status: 'Added' },
];

describe('AdminRecommendations', () => {
  beforeEach(() => {
    getRolesFromToken.mockReturnValue(['Admin']);
    fetchAdminRecommendations.mockResolvedValue(recommendations);
  });

  afterEach(() => {
    vi.resetAllMocks();
  });

  it('turns non-admins away without loading anything', () => {
    getRolesFromToken.mockReturnValue(['Customer']);

    render(<AdminRecommendations />);

    expect(screen.getByText(/admin account/i)).toBeInTheDocument();
    expect(fetchAdminRecommendations).not.toHaveBeenCalled();
  });

  it('lists recommendations with customer email and status', async () => {
    render(<AdminRecommendations />);

    expect(await screen.findByText('Duvel')).toBeInTheDocument();
    expect(screen.getByText('customer@example.com')).toBeInTheDocument();
    expect(screen.getByText('Great beer')).toBeInTheDocument();
    expect(screen.getByText('Orval')).toBeInTheDocument();
  });

  it('filtering by status refetches with that status', async () => {
    const user = userEvent.setup();
    render(<AdminRecommendations />);
    await screen.findByText('Duvel');
    fetchAdminRecommendations.mockClear();

    await user.click(screen.getByRole('button', { name: 'Added' }));

    await waitFor(() => {
      expect(fetchAdminRecommendations).toHaveBeenCalledWith('Added');
    });
  });

  it('marking a recommendation reviewed calls the API immediately, no reason guard', async () => {
    const user = userEvent.setup();
    updateRecommendationStatus.mockResolvedValue();
    render(<AdminRecommendations />);
    await screen.findByText('Duvel');

    const duvelCard = screen.getByText('Duvel').closest('div.rounded-2xl');
    await user.click(within(duvelCard).getByRole('button', { name: 'Mark Reviewed' }));

    await waitFor(() => {
      expect(updateRecommendationStatus).toHaveBeenCalledWith(1, 'Reviewed');
    });
  });

  it('shows the API error message when loading fails', async () => {
    fetchAdminRecommendations.mockRejectedValue(new Error('Failed to load recommendations'));

    render(<AdminRecommendations />);

    expect(await screen.findByText('Failed to load recommendations')).toBeInTheDocument();
  });
});
