import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import AuthPage from './AuthPage';
import { login, register } from '../lib/api';

vi.mock('../lib/api');

describe('AuthPage', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    vi.resetAllMocks();
  });

  it('defaults to login mode', () => {
    render(<AuthPage />);

    expect(screen.getByRole('heading', { name: 'Log in' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Continue' })).toBeInTheDocument();
  });

  it('switches to register mode', async () => {
    const user = userEvent.setup();
    render(<AuthPage />);

    await user.click(screen.getByRole('button', { name: 'Register' }));

    expect(screen.getByRole('heading', { name: 'Create account' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Create account' })).toBeInTheDocument();
  });

  it('logs in, stores the token, and shows a success message', async () => {
    login.mockResolvedValue({ token: 'jwt-token', email: 'a@example.com' });
    const user = userEvent.setup();

    render(<AuthPage />);
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

    render(<AuthPage />);
    await user.type(screen.getByPlaceholderText('Email'), 'a@example.com');
    await user.type(screen.getByPlaceholderText('Password'), 'wrong');
    await user.click(screen.getByRole('button', { name: 'Continue' }));

    expect(await screen.findByText('Login failed')).toBeInTheDocument();
  });

  it('registers a new account', async () => {
    register.mockResolvedValue({ token: 'jwt-token', email: 'new@example.com' });
    const user = userEvent.setup();

    render(<AuthPage />);
    await user.click(screen.getByRole('button', { name: 'Register' }));
    await user.type(screen.getByPlaceholderText('Email'), 'new@example.com');
    await user.type(screen.getByPlaceholderText('Password'), 'Passw0rd!');
    await user.click(screen.getByRole('button', { name: 'Create account' }));

    expect(register).toHaveBeenCalledWith('new@example.com', 'Passw0rd!');
    expect(await screen.findByText('Registered successfully.')).toBeInTheDocument();
  });
});
