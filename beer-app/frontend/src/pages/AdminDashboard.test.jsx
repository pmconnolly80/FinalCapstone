import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import AdminDashboard from './AdminDashboard';
import { fetchAdminAnomalies, fetchDashboardSummary, getRolesFromToken } from '../lib/api';

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

  it('links to Manage Beers, Manage Users, and Confirmations', async () => {
    renderPage();
    await screen.findByText('Total Beers');

    expect(screen.getByRole('link', { name: 'Manage Beers' })).toHaveAttribute('href', '/admin/beers');
    expect(screen.getByRole('link', { name: 'Manage Users' })).toHaveAttribute('href', '/admin/users');
    expect(screen.getByRole('link', { name: 'Confirmations' })).toHaveAttribute('href', '/admin/confirmations');
  });
});
