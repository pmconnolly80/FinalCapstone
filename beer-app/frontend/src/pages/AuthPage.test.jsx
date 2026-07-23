import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import AuthPage from './AuthPage';
import { login, register } from '../lib/api';

vi.mock('../lib/api');

function renderAuthPage() {
  return render(
    <MemoryRouter>
      <AuthPage />
    </MemoryRouter>
  );
}

describe('AuthPage', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    vi.resetAllMocks();
  });

  it('defaults to login mode', () => {
    renderAuthPage();

    expect(screen.getByRole('heading', { name: 'Log in' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Continue' })).toBeInTheDocument();
  });

  it('shows a forgot password link in login mode only', async () => {
    const user = userEvent.setup();
    renderAuthPage();

    expect(screen.getByRole('link', { name: 'Forgot password?' })).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Register' }));

    expect(screen.queryByRole('link', { name: 'Forgot password?' })).not.toBeInTheDocument();
  });

  it('switches to register mode', async () => {
    const user = userEvent.setup();
    renderAuthPage();

    await user.click(screen.getByRole('button', { name: 'Register' }));

    expect(screen.getByRole('heading', { name: 'Create account' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Create account' })).toBeInTheDocument();
  });

  it('logs in, stores the token, and shows a success message', async () => {
    login.mockResolvedValue({ token: 'jwt-token', email: 'a@example.com' });
    const user = userEvent.setup();

    renderAuthPage();
    await user.type(screen.getByPlaceholderText('Email'), 'a@example.com');
    await user.type(screen.getByPlaceholderText('Password'), 'Passw0rd!');
    await user.click(screen.getByRole('button', { name: 'Continue' }));

    expect(login).toHaveBeenCalledWith('a@example.com', 'Passw0rd!');
    expect(await screen.findByText('Logged in successfully.')).toBeInTheDocument();
    expect(localStorage.getItem('beer-token')).toBe('jwt-token');
  });

  it('shows the error message when login fails', async () => {
    login.mockRejectedValue(new Error('Login failed'));
    const user = userEvent.setup();

    renderAuthPage();
    await user.type(screen.getByPlaceholderText('Email'), 'a@example.com');
    await user.type(screen.getByPlaceholderText('Password'), 'wrong');
    await user.click(screen.getByRole('button', { name: 'Continue' }));

    expect(await screen.findByText('Login failed')).toBeInTheDocument();
  });

  it('registers a new account', async () => {
    register.mockResolvedValue({ token: 'jwt-token', email: 'new@example.com' });
    const user = userEvent.setup();

    renderAuthPage();
    await user.click(screen.getByRole('button', { name: 'Register' }));
    await user.type(screen.getByPlaceholderText('Email'), 'new@example.com');
    await user.type(screen.getByPlaceholderText('Password'), 'Passw0rd!');
    await user.click(screen.getByRole('button', { name: 'Create account' }));

    expect(register).toHaveBeenCalledWith('new@example.com', 'Passw0rd!');
    expect(await screen.findByText('Registered successfully.')).toBeInTheDocument();
  });

  it('shows the password requirement in register mode before submitting', async () => {
    const user = userEvent.setup();
    renderAuthPage();

    expect(
      screen.queryByText('Passwords need at least 8 characters.')
    ).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Register' }));

    expect(screen.getByText('Passwords need at least 8 characters.')).toBeInTheDocument();
  });

  it('blocks registration with a short password without calling the API', async () => {
    const user = userEvent.setup();
    renderAuthPage();

    await user.click(screen.getByRole('button', { name: 'Register' }));
    await user.type(screen.getByPlaceholderText('Email'), 'new@example.com');
    await user.type(screen.getByPlaceholderText('Password'), 'beer123');
    await user.click(screen.getByRole('button', { name: 'Create account' }));

    expect(register).not.toHaveBeenCalled();
    expect(await screen.findByText('Password is too short.')).toBeInTheDocument();
  });

  it('shows the API error message when registration fails', async () => {
    register.mockRejectedValue(new Error('A user with that email already exists.'));
    const user = userEvent.setup();

    renderAuthPage();
    await user.click(screen.getByRole('button', { name: 'Register' }));
    await user.type(screen.getByPlaceholderText('Email'), 'dup@example.com');
    await user.type(screen.getByPlaceholderText('Password'), 'Passw0rd!');
    await user.click(screen.getByRole('button', { name: 'Create account' }));

    expect(
      await screen.findByText('A user with that email already exists.')
    ).toBeInTheDocument();
  });
});
