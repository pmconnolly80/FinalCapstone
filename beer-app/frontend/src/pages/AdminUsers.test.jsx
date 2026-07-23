import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import AdminUsers from './AdminUsers';
import {
  assignRole,
  deactivateAccount,
  deactivateStaffPin,
  getAdminUsers,
  getRolesFromToken,
  issueOrResetStaffPin,
  reactivateAccount,
} from '../lib/api';

vi.mock('../lib/api');

const users = [
  { id: 'cust-1', email: 'customer@example.com', role: 'Customer', isActive: true, hasActivePin: false },
  { id: 'bart-1', email: 'bartender@example.com', role: 'Bartender', isActive: true, hasActivePin: true },
  { id: 'deact-1', email: 'deactivated@example.com', role: 'Bartender', isActive: false, hasActivePin: false },
];

function renderPage() {
  return render(
    <MemoryRouter>
      <AdminUsers />
    </MemoryRouter>
  );
}

describe('AdminUsers', () => {
  beforeEach(() => {
    localStorage.setItem('beer-token', 'abc');
    getRolesFromToken.mockReturnValue(['Admin']);
    getAdminUsers.mockResolvedValue(users);
  });

  afterEach(() => {
    localStorage.clear();
    vi.resetAllMocks();
  });

  it('turns non-admins away without loading anything', () => {
    getRolesFromToken.mockReturnValue(['Customer']);

    renderPage();

    expect(screen.getByText(/admin account/i)).toBeInTheDocument();
    expect(getAdminUsers).not.toHaveBeenCalled();
  });

  it('shows the user list with role, status, and PIN badges', async () => {
    renderPage();

    expect(await screen.findByText('customer@example.com')).toBeInTheDocument();
    expect(screen.getByText('bartender@example.com')).toBeInTheDocument();
    expect(screen.getAllByText('Active')).toHaveLength(2);
    expect(screen.getByText('Deactivated')).toBeInTheDocument();
    expect(screen.getByText('PIN active')).toBeInTheDocument();
    expect(screen.getAllByText('No PIN')).toHaveLength(2);
  });

  it('requires a reason and an explicit confirm before changing role', async () => {
    const user = userEvent.setup();
    renderPage();
    await screen.findByText('customer@example.com');

    const roleSelect = screen.getByDisplayValue('Customer');
    await user.selectOptions(roleSelect, 'Bartender');
    expect(assignRole).not.toHaveBeenCalled();

    await user.click(screen.getByRole('button', { name: 'Confirm' }));
    expect(assignRole).not.toHaveBeenCalled();
    expect(await screen.findByText(/reason is required/i)).toBeInTheDocument();

    assignRole.mockResolvedValue(undefined);
    await user.type(screen.getByPlaceholderText('Reason'), 'promoted to staff');
    await user.click(screen.getByRole('button', { name: 'Confirm' }));

    expect(assignRole).toHaveBeenCalledWith('cust-1', 'Bartender', 'promoted to staff');
  });

  it('requires a reason and an explicit confirm before deactivating', async () => {
    const user = userEvent.setup();
    renderPage();
    await screen.findByText('customer@example.com');

    await user.click(screen.getAllByRole('button', { name: 'Deactivate' })[0]);
    expect(deactivateAccount).not.toHaveBeenCalled();

    await user.click(screen.getByRole('button', { name: 'Confirm' }));
    expect(deactivateAccount).not.toHaveBeenCalled();
    expect(await screen.findByText(/reason is required/i)).toBeInTheDocument();

    deactivateAccount.mockResolvedValue(undefined);
    await user.type(screen.getByPlaceholderText('Reason'), 'policy violation');
    await user.click(screen.getByRole('button', { name: 'Confirm' }));

    expect(deactivateAccount).toHaveBeenCalledWith('cust-1', 'policy violation');
  });

  it('offers reactivate only for a deactivated account, gated by a reason', async () => {
    const user = userEvent.setup();
    renderPage();
    await screen.findByText('deactivated@example.com');

    expect(screen.queryAllByRole('button', { name: 'Deactivate' })).toHaveLength(2);
    await user.click(screen.getByRole('button', { name: 'Reactivate' }));

    reactivateAccount.mockResolvedValue(undefined);
    await user.type(screen.getByPlaceholderText('Reason'), 'appeal approved');
    await user.click(screen.getByRole('button', { name: 'Confirm' }));

    expect(reactivateAccount).toHaveBeenCalledWith('deact-1', 'appeal approved');
  });

  it('offers Set PIN only for staff rows, and validates the PIN format', async () => {
    const user = userEvent.setup();
    renderPage();
    await screen.findByText('customer@example.com');

    const rows = screen.getAllByRole('row');
    const customerRow = rows.find((r) => r.textContent.includes('customer@example.com'));
    expect(within(customerRow).queryByRole('button', { name: 'Set PIN' })).not.toBeInTheDocument();

    await user.click(screen.getAllByRole('button', { name: 'Set PIN' })[0]);
    await user.type(screen.getByPlaceholderText('6-digit PIN'), '123');
    await user.click(screen.getByRole('button', { name: 'Confirm' }));

    expect(issueOrResetStaffPin).not.toHaveBeenCalled();
    expect(await screen.findByText(/exactly 6 digits/i)).toBeInTheDocument();

    issueOrResetStaffPin.mockResolvedValue(undefined);
    await user.clear(screen.getByPlaceholderText('6-digit PIN'));
    await user.type(screen.getByPlaceholderText('6-digit PIN'), '135790');
    await user.click(screen.getByRole('button', { name: 'Confirm' }));

    expect(issueOrResetStaffPin).toHaveBeenCalledWith('bart-1', '135790');
  });

  it('deactivates a PIN directly, with no reason step', async () => {
    const user = userEvent.setup();
    deactivateStaffPin.mockResolvedValue(undefined);
    renderPage();
    await screen.findByText('bartender@example.com');

    await user.click(screen.getByRole('button', { name: 'Deactivate PIN' }));

    expect(deactivateStaffPin).toHaveBeenCalledWith('bart-1');
  });

  it('surfaces API errors from a failed action', async () => {
    const user = userEvent.setup();
    deactivateAccount.mockRejectedValue(new Error('User not found.'));
    renderPage();
    await screen.findByText('customer@example.com');

    await user.click(screen.getAllByRole('button', { name: 'Deactivate' })[0]);
    await user.type(screen.getByPlaceholderText('Reason'), 'policy violation');
    await user.click(screen.getByRole('button', { name: 'Confirm' }));

    expect(await screen.findByText('User not found.')).toBeInTheDocument();
  });
});
