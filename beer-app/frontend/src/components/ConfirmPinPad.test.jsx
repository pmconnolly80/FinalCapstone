import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import ConfirmPinPad from './ConfirmPinPad';
import { confirmBeer } from '../lib/api';

vi.mock('../lib/api');

const beer = { id: 7, name: 'Duvel', brewery: 'Duvel Moortgat' };

describe('ConfirmPinPad', () => {
  afterEach(() => {
    vi.resetAllMocks();
  });

  it('shows the beer name large so the bartender can verify before keying their PIN', () => {
    render(<ConfirmPinPad beer={beer} onClose={() => {}} />);

    expect(screen.getByText('Duvel')).toBeInTheDocument();
    expect(screen.getByText('Hand your phone to the bartender')).toBeInTheDocument();
  });

  it('masks PIN entry and only accepts digits, capped at 6', async () => {
    const user = userEvent.setup();
    render(<ConfirmPinPad beer={beer} onClose={() => {}} />);

    const input = screen.getByLabelText('Bartender PIN');
    expect(input).toHaveAttribute('type', 'password');

    await user.type(input, '12ab345678');

    expect(input).toHaveValue('123456');
  });

  it('requires 6 digits before submitting', async () => {
    const user = userEvent.setup();
    render(<ConfirmPinPad beer={beer} onClose={() => {}} />);

    await user.type(screen.getByLabelText('Bartender PIN'), '123');
    await user.click(screen.getByRole('button', { name: 'Confirm' }));

    expect(screen.getByText('Enter the 6-digit bartender PIN.')).toBeInTheDocument();
    expect(confirmBeer).not.toHaveBeenCalled();
  });

  it('confirms and shows the updated count', async () => {
    confirmBeer.mockResolvedValue({ confirmedCount: 87, goal: 200, mugEarned: false });
    const user = userEvent.setup();
    render(<ConfirmPinPad beer={beer} onClose={() => {}} />);

    await user.type(screen.getByLabelText('Bartender PIN'), '123456');
    await user.click(screen.getByRole('button', { name: 'Confirm' }));

    expect(await screen.findByText('87 of 200')).toBeInTheDocument();
    expect(confirmBeer).toHaveBeenCalledWith(7, '123456');
  });

  it('celebrates when the mug is earned', async () => {
    confirmBeer.mockResolvedValue({ confirmedCount: 200, goal: 200, mugEarned: true });
    const user = userEvent.setup();
    render(<ConfirmPinPad beer={beer} onClose={() => {}} />);

    await user.type(screen.getByLabelText('Bartender PIN'), '123456');
    await user.click(screen.getByRole('button', { name: 'Confirm' }));

    expect(await screen.findByText('🏆 Mug earned!')).toBeInTheDocument();
  });

  it('shows the API error and clears the PIN on a rejected confirmation', async () => {
    confirmBeer.mockRejectedValue(new Error('Invalid PIN.'));
    const user = userEvent.setup();
    render(<ConfirmPinPad beer={beer} onClose={() => {}} />);

    const input = screen.getByLabelText('Bartender PIN');
    await user.type(input, '654321');
    await user.click(screen.getByRole('button', { name: 'Confirm' }));

    expect(await screen.findByText('Invalid PIN.')).toBeInTheDocument();
    expect(input).toHaveValue('');
  });

  it('closes via the cancel button', async () => {
    const onClose = vi.fn();
    const user = userEvent.setup();
    render(<ConfirmPinPad beer={beer} onClose={onClose} />);

    await user.click(screen.getByRole('button', { name: 'Cancel' }));

    expect(onClose).toHaveBeenCalled();
  });
});
