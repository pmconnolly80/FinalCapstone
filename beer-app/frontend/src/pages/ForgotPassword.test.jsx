import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import ForgotPassword from './ForgotPassword';
import { forgotPassword } from '../lib/api';

vi.mock('../lib/api');

function renderForgotPassword() {
  return render(
    <MemoryRouter>
      <ForgotPassword />
    </MemoryRouter>
  );
}

describe('ForgotPassword', () => {
  afterEach(() => {
    vi.resetAllMocks();
  });

  it('submits the email and shows the generic success message', async () => {
    forgotPassword.mockResolvedValue({ message: 'ok' });
    const user = userEvent.setup();

    renderForgotPassword();
    await user.type(screen.getByPlaceholderText('Email'), 'a@example.com');
    await user.click(screen.getByRole('button', { name: 'Send reset link' }));

    expect(forgotPassword).toHaveBeenCalledWith('a@example.com');
    expect(
      await screen.findByText(
        'If an account with that email exists, a password reset link has been sent.'
      )
    ).toBeInTheDocument();
  });

  it('hides the form after a successful submission', async () => {
    forgotPassword.mockResolvedValue({ message: 'ok' });
    const user = userEvent.setup();

    renderForgotPassword();
    await user.type(screen.getByPlaceholderText('Email'), 'a@example.com');
    await user.click(screen.getByRole('button', { name: 'Send reset link' }));

    expect(
      await screen.findByText(
        'If an account with that email exists, a password reset link has been sent.'
      )
    ).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Send reset link' })).not.toBeInTheDocument();
  });

  it('shows the API error message when the request fails', async () => {
    forgotPassword.mockRejectedValue(new Error('Request failed'));
    const user = userEvent.setup();

    renderForgotPassword();
    await user.type(screen.getByPlaceholderText('Email'), 'a@example.com');
    await user.click(screen.getByRole('button', { name: 'Send reset link' }));

    expect(await screen.findByText('Request failed')).toBeInTheDocument();
  });
});
