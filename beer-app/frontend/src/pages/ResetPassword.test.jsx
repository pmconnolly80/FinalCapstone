import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import ResetPassword from './ResetPassword';
import { resetPassword } from '../lib/api';

vi.mock('../lib/api');

function renderResetPassword(search = '?email=a%40example.com&token=abc123') {
  return render(
    <MemoryRouter initialEntries={[`/reset-password${search}`]}>
      <ResetPassword />
    </MemoryRouter>
  );
}

describe('ResetPassword', () => {
  afterEach(() => {
    vi.resetAllMocks();
  });

  it('shows an invalid-link message when email or token is missing from the URL', () => {
    renderResetPassword('');

    expect(screen.getByText('This password reset link is invalid or has expired.')).toBeInTheDocument();
    expect(screen.queryByPlaceholderText('New password')).not.toBeInTheDocument();
  });

  it('submits the new password with the email/token from the URL', async () => {
    resetPassword.mockResolvedValue({ message: 'ok' });
    const user = userEvent.setup();

    renderResetPassword();
    await user.type(screen.getByPlaceholderText('New password'), 'NewPassw0rd!');
    await user.click(screen.getByRole('button', { name: 'Reset password' }));

    expect(resetPassword).toHaveBeenCalledWith('a@example.com', 'abc123', 'NewPassw0rd!');
    expect(
      await screen.findByText('Your password has been reset. You can now sign in.')
    ).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Sign in' })).toBeInTheDocument();
  });

  it('blocks a short new password without calling the API', async () => {
    const user = userEvent.setup();

    renderResetPassword();
    await user.type(screen.getByPlaceholderText('New password'), 'short');
    await user.click(screen.getByRole('button', { name: 'Reset password' }));

    expect(resetPassword).not.toHaveBeenCalled();
    expect(await screen.findByText('Password is too short.')).toBeInTheDocument();
  });

  it('shows the API error message when the reset fails', async () => {
    resetPassword.mockRejectedValue(new Error('This password reset link is invalid or has expired.'));
    const user = userEvent.setup();

    renderResetPassword();
    await user.type(screen.getByPlaceholderText('New password'), 'NewPassw0rd!');
    await user.click(screen.getByRole('button', { name: 'Reset password' }));

    expect(
      await screen.findByText('This password reset link is invalid or has expired.')
    ).toBeInTheDocument();
  });
});
