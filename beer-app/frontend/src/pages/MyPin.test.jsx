import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import MyPin from './MyPin';
import { setMyPin } from '../lib/api';

vi.mock('../lib/api');

function renderMyPin() {
  return render(
    <MemoryRouter>
      <MyPin />
    </MemoryRouter>
  );
}

describe('MyPin', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    vi.resetAllMocks();
  });

  it('prompts for sign-in when no token is stored', () => {
    renderMyPin();

    expect(screen.getByText('Sign in')).toBeInTheDocument();
  });

  it('submits a valid 6-digit PIN and shows success', async () => {
    localStorage.setItem('beer-token', 'abc');
    setMyPin.mockResolvedValue(undefined);
    const user = userEvent.setup();

    renderMyPin();
    await user.type(screen.getByPlaceholderText('New 6-digit PIN'), '654321');
    await user.click(screen.getByRole('button', { name: 'Save PIN' }));

    expect(setMyPin).toHaveBeenCalledWith('654321');
    expect(await screen.findByText('PIN updated.')).toBeInTheDocument();
  });

  it('blocks a malformed PIN without calling the API', async () => {
    localStorage.setItem('beer-token', 'abc');
    const user = userEvent.setup();

    renderMyPin();
    await user.type(screen.getByPlaceholderText('New 6-digit PIN'), '123');
    await user.click(screen.getByRole('button', { name: 'Save PIN' }));

    expect(setMyPin).not.toHaveBeenCalled();
    expect(await screen.findByText('PINs are exactly 6 digits.')).toBeInTheDocument();
  });

  it('shows the API error message when the change fails', async () => {
    localStorage.setItem('beer-token', 'abc');
    setMyPin.mockRejectedValue(new Error('That PIN is already in use by another staff member.'));
    const user = userEvent.setup();

    renderMyPin();
    await user.type(screen.getByPlaceholderText('New 6-digit PIN'), '654321');
    await user.click(screen.getByRole('button', { name: 'Save PIN' }));

    expect(
      await screen.findByText('That PIN is already in use by another staff member.')
    ).toBeInTheDocument();
  });
});
