import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import LinkedAccounts from './LinkedAccounts';
import { getLinkedProviders, startLinkingProvider } from '../lib/api';

vi.mock('../lib/api');

function renderLinkedAccounts(search = '') {
  return render(
    <MemoryRouter initialEntries={[`/account/linked-providers${search}`]}>
      <LinkedAccounts />
    </MemoryRouter>
  );
}

describe('LinkedAccounts', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    vi.resetAllMocks();
  });

  it('prompts for sign-in when no token is stored', () => {
    renderLinkedAccounts();

    expect(screen.getByText('Sign in')).toBeInTheDocument();
  });

  it('shows connected providers and a Connect button for the rest', async () => {
    localStorage.setItem('beer-token', 'abc');
    getLinkedProviders.mockResolvedValue(['Google']);

    renderLinkedAccounts();

    expect(await screen.findByText('Connected')).toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: 'Connect' })).toHaveLength(2);
  });

  it('clicking Connect starts the linking flow for that provider', async () => {
    localStorage.setItem('beer-token', 'abc');
    getLinkedProviders.mockResolvedValue([]);
    startLinkingProvider.mockResolvedValue(undefined);
    const user = userEvent.setup();

    renderLinkedAccounts();
    await screen.findAllByRole('button', { name: 'Connect' });
    await user.click(screen.getAllByRole('button', { name: 'Connect' })[0]);

    expect(startLinkingProvider).toHaveBeenCalledWith('Google');
  });

  it('shows a success message after being redirected back from a successful link', async () => {
    localStorage.setItem('beer-token', 'abc');
    getLinkedProviders.mockResolvedValue(['Facebook']);

    renderLinkedAccounts('?linked=Facebook');

    expect(await screen.findByText(/Facebook connected/)).toBeInTheDocument();
  });
});
