import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import AdminDashboard from './AdminDashboard';
import { fetchAdminAnomalies, fetchBeerConfirmationCounts, fetchDashboardSummary, getRolesFromToken } from '../lib/api';

vi.mock('../lib/api');

const summary = { totalBeers: 8, confirmationsToday: 3, activeMembers: 5, mugsAwarded: 1 };

const anomalies = [
  {
    type: 'BulkBeerAdd',
    occurredAt: '2026-07-23T16:00:00Z',
    summary: '11 beers added within 60 minutes',
    actorId: 'admin-1',
    actorEmail: 'admin@example.com',
    deepLink: '/admin/beers',
  },
];

const beerCounts = {
  mostConfirmed: [{ beerId: 1, name: 'Duvel', confirmedCount: 42 }],
  leastConfirmed: [{ beerId: 2, name: 'Mystery Stout', confirmedCount: 0 }],
};

function renderPage() {
  return render(
    <MemoryRouter>
      <AdminDashboard />
    </MemoryRouter>
  );
}

describe('AdminDashboard', () => {
  beforeEach(() => {
    localStorage.setItem('beer-token', 'abc');
    getRolesFromToken.mockReturnValue(['Admin']);
    fetchDashboardSummary.mockResolvedValue(summary);
    fetchAdminAnomalies.mockResolvedValue(anomalies);
    fetchBeerConfirmationCounts.mockResolvedValue(beerCounts);
  });

  afterEach(() => {
    localStorage.clear();
    vi.resetAllMocks();
  });

  it('turns non-admins away without loading anything', () => {
    getRolesFromToken.mockReturnValue(['Customer']);

    renderPage();

    expect(screen.getByText(/admin account/i)).toBeInTheDocument();
    expect(fetchDashboardSummary).not.toHaveBeenCalled();
    expect(fetchAdminAnomalies).not.toHaveBeenCalled();
    expect(fetchBeerConfirmationCounts).not.toHaveBeenCalled();
  });

  it('renders all four summary numbers', async () => {
    renderPage();

    expect(await screen.findByText('8')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();
    expect(screen.getByText('5')).toBeInTheDocument();
    expect(screen.getByText('1')).toBeInTheDocument();
    expect(screen.getByText('Total Beers')).toBeInTheDocument();
    expect(screen.getByText('Confirmations Today')).toBeInTheDocument();
    expect(screen.getByText('Active Members')).toBeInTheDocument();
    expect(screen.getByText('Mugs Awarded')).toBeInTheDocument();
  });

  it('renders anomaly items with badge, summary, actor email, and a working deep link', async () => {
    renderPage();

    expect(await screen.findByText('Bulk Beer Add')).toBeInTheDocument();
    expect(screen.getByText('11 beers added within 60 minutes')).toBeInTheDocument();
    expect(screen.getByText('(admin@example.com)')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'View' })).toHaveAttribute('href', '/admin/beers');
  });

  it('renders an UnavailabilityReport anomaly with its own badge and deep link to the beer', async () => {
    fetchAdminAnomalies.mockResolvedValue([
      {
        type: 'UnavailabilityReport',
        occurredAt: '2026-07-23T18:00:00Z',
        summary: "'Duvel' flagged unavailable by 3 customers in the last 24h",
        actorId: null,
        actorEmail: null,
        deepLink: '/beers/5',
      },
    ]);

    renderPage();

    expect(await screen.findByText('Unavailable Report')).toBeInTheDocument();
    expect(screen.getByText("'Duvel' flagged unavailable by 3 customers in the last 24h")).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'View' })).toHaveAttribute('href', '/beers/5');
  });

  it('shows a summary error while the anomaly panel still renders', async () => {
    fetchDashboardSummary.mockRejectedValue(new Error('Failed to load dashboard summary'));

    renderPage();

    expect(await screen.findByText('Failed to load dashboard summary')).toBeInTheDocument();
    expect(await screen.findByText('Bulk Beer Add')).toBeInTheDocument();
  });

  it('shows an anomalies error while the summary cards still render', async () => {
    fetchAdminAnomalies.mockRejectedValue(new Error('Failed to load anomalies'));

    renderPage();

    expect(await screen.findByText('Failed to load anomalies')).toBeInTheDocument();
    expect(await screen.findByText('Total Beers')).toBeInTheDocument();
    expect(screen.getByText('8')).toBeInTheDocument();
  });

  it('#78: reframes the dashboard as operational health, distinct from beer-purchasing intelligence', async () => {
    renderPage();

    expect(await screen.findByText('Operational health')).toBeInTheDocument();
    expect(screen.getAllByText(/beer-purchasing intelligence/i).length).toBeGreaterThan(0);
  });

  it('#78: renders most/least confirmed beers with links to each beer', async () => {
    renderPage();

    expect(await screen.findByText('Most / least confirmed beers')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Duvel' })).toHaveAttribute('href', '/beers/1');
    expect(screen.getByText('(42)')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Mystery Stout' })).toHaveAttribute('href', '/beers/2');
    expect(screen.getByText('(0)')).toBeInTheDocument();
  });

  it('#78: shows a beer-counts error while the rest of the dashboard still renders', async () => {
    fetchBeerConfirmationCounts.mockRejectedValue(new Error('Failed to load most/least-confirmed beers'));

    renderPage();

    expect(await screen.findByText('Failed to load most/least-confirmed beers')).toBeInTheDocument();
    expect(await screen.findByText('Total Beers')).toBeInTheDocument();
  });

  it('links to Manage Beers, Manage Users, and Confirmations', async () => {
    renderPage();
    await screen.findByText('Total Beers');

    expect(screen.getByRole('link', { name: 'Manage Beers' })).toHaveAttribute('href', '/admin/beers');
    expect(screen.getByRole('link', { name: 'Manage Users' })).toHaveAttribute('href', '/admin/users');
    expect(screen.getByRole('link', { name: 'Confirmations' })).toHaveAttribute('href', '/admin/confirmations');
  });
});
