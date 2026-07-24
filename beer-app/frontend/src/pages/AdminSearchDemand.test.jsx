import { render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import AdminSearchDemand from './AdminSearchDemand';
import { fetchExternalSearchDemand, getRolesFromToken } from '../lib/api';

vi.mock('../lib/api');

const demand = [
  { query: 'weird sour', count: 3, lastSearchedAt: '2026-07-22T20:00:00Z' },
  { query: 'obscure lager', count: 1, lastSearchedAt: '2026-07-20T18:00:00Z' },
];

describe('AdminSearchDemand', () => {
  beforeEach(() => {
    getRolesFromToken.mockReturnValue(['Admin']);
  });

  afterEach(() => {
    vi.resetAllMocks();
  });

  it('turns non-admins away without loading anything', () => {
    getRolesFromToken.mockReturnValue(['Customer']);

    render(<AdminSearchDemand />);

    expect(screen.getByText(/admin account/i)).toBeInTheDocument();
    expect(fetchExternalSearchDemand).not.toHaveBeenCalled();
  });

  it('lists demand rows sorted by count, most-searched first', async () => {
    fetchExternalSearchDemand.mockResolvedValue(demand);

    render(<AdminSearchDemand />);

    expect(await screen.findByText('weird sour')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();
    expect(screen.getByText('obscure lager')).toBeInTheDocument();
  });

  it('shows an empty state when there are no unmatched searches', async () => {
    fetchExternalSearchDemand.mockResolvedValue([]);

    render(<AdminSearchDemand />);

    expect(await screen.findByText('No unmatched searches yet.')).toBeInTheDocument();
  });

  it('shows the API error message on failure, without blanking the whole page', async () => {
    fetchExternalSearchDemand.mockRejectedValue(new Error('Failed to load search demand'));

    render(<AdminSearchDemand />);

    expect(await screen.findByText('Failed to load search demand')).toBeInTheDocument();
    expect(screen.getByText('Search demand')).toBeInTheDocument();
  });
});
