import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import AdminConfirmations from './AdminConfirmations';
import {
  fetchAdminConfirmations,
  fetchConfirmationAudits,
  getRolesFromToken,
  voidConfirmation,
} from '../lib/api';

vi.mock('../lib/api');

const rows = [
  {
    id: 1,
    customerId: 'cust-1',
    customerEmail: 'drinker@example.com',
    beerId: 5,
    beerName: 'Duvel',
    bartenderEmail: 'bartender@example.com',
    confirmedAt: '2026-07-15T21:00:00Z',
  },
  {
    id: 2,
    customerId: 'cust-2',
    customerEmail: 'other@example.com',
    beerId: 6,
    beerName: 'Orval',
    bartenderEmail: 'bartender@example.com',
    confirmedAt: '2026-07-14T20:00:00Z',
  },
];

const audits = [
  {
    id: 1,
    customerEmail: 'fixed@example.com',
    beerName: 'Chimay Blue',
    bartenderEmail: 'bartender@example.com',
    confirmedAt: '2026-07-10T20:00:00Z',
    adminEmail: 'admin@example.com',
    correctedAt: '2026-07-15T22:00:00Z',
    reason: 'wrong beer tapped in',
  },
];

function renderPage() {
  return render(
    <MemoryRouter>
      <AdminConfirmations />
    </MemoryRouter>
  );
}

describe('AdminConfirmations', () => {
  beforeEach(() => {
    localStorage.setItem('beer-token', 'abc');
    getRolesFromToken.mockReturnValue(['Admin']);
    fetchAdminConfirmations.mockResolvedValue(rows);
    fetchConfirmationAudits.mockResolvedValue(audits);
  });

  afterEach(() => {
    localStorage.clear();
    vi.resetAllMocks();
  });

  it('turns non-admins away without loading anything', () => {
    getRolesFromToken.mockReturnValue(['Customer']);

    renderPage();

    expect(screen.getByText(/admin account/i)).toBeInTheDocument();
    expect(fetchAdminConfirmations).not.toHaveBeenCalled();
  });

  it('shows the confirmation history with customer, beer, and bartender', async () => {
    renderPage();

    expect(await screen.findByText('drinker@example.com')).toBeInTheDocument();
    expect(screen.getByText('Duvel')).toBeInTheDocument();
    expect(screen.getByText('Orval')).toBeInTheDocument();
  });

  it('filters rows by text', async () => {
    const user = userEvent.setup();
    renderPage();
    await screen.findByText('drinker@example.com');

    await user.type(screen.getByPlaceholderText(/filter/i), 'orval');

    expect(screen.queryByText('drinker@example.com')).not.toBeInTheDocument();
    expect(screen.getByText('other@example.com')).toBeInTheDocument();
  });

  it('requires a reason and an explicit confirm before voiding', async () => {
    const user = userEvent.setup();
    renderPage();
    await screen.findByText('drinker@example.com');

    // Void is a two-step action: the row's Void button only reveals the guard.
    await user.click(screen.getAllByRole('button', { name: 'Void' })[0]);
    expect(voidConfirmation).not.toHaveBeenCalled();

    // Confirming without a reason is blocked client-side.
    await user.click(screen.getByRole('button', { name: 'Confirm void' }));
    expect(voidConfirmation).not.toHaveBeenCalled();
    expect(await screen.findByText(/reason is required/i)).toBeInTheDocument();

    voidConfirmation.mockResolvedValue(undefined);
    await user.type(screen.getByPlaceholderText(/reason/i), 'wrong customer confirmed');
    await user.click(screen.getByRole('button', { name: 'Confirm void' }));

    expect(voidConfirmation).toHaveBeenCalledWith(1, 'wrong customer confirmed');
  });

  it('shows consequence microcopy at the void step, not before', async () => {
    const user = userEvent.setup();
    renderPage();
    await screen.findByText('drinker@example.com');

    expect(screen.queryByText(/not revoked/i)).not.toBeInTheDocument();

    await user.click(screen.getAllByRole('button', { name: 'Void' })[0]);

    expect(screen.getByText(/mug was already awarded, it is not revoked/i)).toBeInTheDocument();
  });

  it('shows the audit trail for voided confirmations', async () => {
    renderPage();

    expect(await screen.findByText('wrong beer tapped in')).toBeInTheDocument();
    expect(screen.getByText('Chimay Blue')).toBeInTheDocument();
    expect(screen.getByText(/admin@example.com/)).toBeInTheDocument();
  });

  it('surfaces API errors from a failed void', async () => {
    const user = userEvent.setup();
    voidConfirmation.mockRejectedValue(new Error('Confirmation not found.'));
    renderPage();
    await screen.findByText('drinker@example.com');

    await user.click(screen.getAllByRole('button', { name: 'Void' })[0]);
    await user.type(screen.getByPlaceholderText(/reason/i), 'stale row');
    await user.click(screen.getByRole('button', { name: 'Confirm void' }));

    expect(await screen.findByText('Confirmation not found.')).toBeInTheDocument();
  });
});
