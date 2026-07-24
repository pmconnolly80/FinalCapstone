import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import ConfirmPinPad from './ConfirmPinPad';
import { confirmBeer, setBeerAvailabilityViaPin, setMyRating } from '../lib/api';

vi.mock('../lib/api');

const beer = { id: 7, name: 'Duvel', brewery: 'Duvel Moortgat', availability: 'OnTap' };

describe('ConfirmPinPad', () => {
  afterEach(() => {
    vi.resetAllMocks();
  });

  it('shows the beer name large so the bartender can verify before keying their PIN', () => {
    render(<ConfirmPinPad beer={beer} onClose={() => {}} />);

    expect(screen.getByText('Duvel')).toBeInTheDocument();
    expect(screen.getByText('Hand your phone to the bartender')).toBeInTheDocument();
  });

  it('masks PIN entry and only accepts digits, capped at 8', async () => {
    const user = userEvent.setup();
    render(<ConfirmPinPad beer={beer} onClose={() => {}} />);

    const input = screen.getByLabelText('Bartender PIN');
    expect(input).toHaveAttribute('type', 'password');

    await user.type(input, '12ab34567890');

    expect(input).toHaveValue('12345678');
  });

  it('requires at least 6 digits before submitting', async () => {
    const user = userEvent.setup();
    render(<ConfirmPinPad beer={beer} onClose={() => {}} />);

    await user.type(screen.getByLabelText('Bartender PIN'), '123');
    await user.click(screen.getByRole('button', { name: 'Confirm' }));

    expect(screen.getByText("Enter the bartender's PIN.")).toBeInTheDocument();
    expect(confirmBeer).not.toHaveBeenCalled();
  });

  it('accepts an 8-digit PIN (e.g. a birthday format) and confirms', async () => {
    confirmBeer.mockResolvedValue({ confirmedCount: 87, goal: 200, mugEarned: false });
    const user = userEvent.setup();
    render(<ConfirmPinPad beer={beer} onClose={() => {}} />);

    await user.type(screen.getByLabelText('Bartender PIN'), '07041999');
    await user.click(screen.getByRole('button', { name: 'Confirm' }));

    expect(await screen.findByText('87 of 200')).toBeInTheDocument();
    expect(confirmBeer).toHaveBeenCalledWith(7, '07041999');
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

  it('shows a repeated-failure cue after 3 consecutive rejections, without revealing the cause', async () => {
    confirmBeer.mockRejectedValue(new Error('Invalid PIN.'));
    const user = userEvent.setup();
    render(<ConfirmPinPad beer={beer} onClose={() => {}} />);

    for (let i = 0; i < 3; i += 1) {
      // eslint-disable-next-line no-await-in-loop
      await user.type(screen.getByLabelText('Bartender PIN'), '654321');
      // eslint-disable-next-line no-await-in-loop
      await user.click(screen.getByRole('button', { name: 'Confirm' }));
      // eslint-disable-next-line no-await-in-loop
      await screen.findByText('Invalid PIN.');
    }

    expect(
      screen.getByText(/If this keeps happening, ask an admin/)
    ).toBeInTheDocument();
  });

  it('resets the failure count after a successful confirmation', async () => {
    confirmBeer.mockRejectedValueOnce(new Error('Invalid PIN.'));
    confirmBeer.mockRejectedValueOnce(new Error('Invalid PIN.'));
    confirmBeer.mockResolvedValueOnce({ confirmedCount: 88, goal: 200, mugEarned: false });
    const user = userEvent.setup();
    render(<ConfirmPinPad beer={beer} onClose={() => {}} />);

    await user.type(screen.getByLabelText('Bartender PIN'), '654321');
    await user.click(screen.getByRole('button', { name: 'Confirm' }));
    await screen.findByText('Invalid PIN.');
    await user.type(screen.getByLabelText('Bartender PIN'), '654321');
    await user.click(screen.getByRole('button', { name: 'Confirm' }));
    await screen.findByText('Invalid PIN.');

    expect(
      screen.queryByText(/If this keeps happening, ask an admin/)
    ).not.toBeInTheDocument();

    await user.type(screen.getByLabelText('Bartender PIN'), '123456');
    await user.click(screen.getByRole('button', { name: 'Confirm' }));

    expect(await screen.findByText('88 of 200')).toBeInTheDocument();
  });

  it('shows a distinct offline message on a network failure, not the generic auth error', async () => {
    const networkError = new Error('No network connection');
    networkError.isNetworkError = true;
    confirmBeer.mockRejectedValue(networkError);
    const user = userEvent.setup();
    render(<ConfirmPinPad beer={beer} onClose={() => {}} />);

    await user.type(screen.getByLabelText('Bartender PIN'), '654321');
    await user.click(screen.getByRole('button', { name: 'Confirm' }));

    expect(
      await screen.findByText(/No signal — ask the bartender to note it/)
    ).toBeInTheDocument();
    expect(screen.queryByText('No network connection')).not.toBeInTheDocument();
  });

  it('closes via the cancel button', async () => {
    const onClose = vi.fn();
    const user = userEvent.setup();
    render(<ConfirmPinPad beer={beer} onClose={onClose} />);

    await user.click(screen.getByRole('button', { name: 'Cancel' }));

    expect(onClose).toHaveBeenCalled();
  });

  describe('#80: mark availability via the same PIN', () => {
    it('offers "mark out of stock" for an in-stock beer, and requires a deliberate second tap', async () => {
      const user = userEvent.setup();
      setBeerAvailabilityViaPin.mockResolvedValue(undefined);
      render(<ConfirmPinPad beer={beer} onClose={() => {}} />);

      await user.type(screen.getByLabelText('Bartender PIN'), '123456');
      await user.click(screen.getByRole('button', { name: 'Mark this beer as out of stock' }));

      expect(setBeerAvailabilityViaPin).not.toHaveBeenCalled();
      expect(screen.getByRole('button', { name: 'Yes, mark out of stock' })).toBeInTheDocument();
      expect(screen.getByText(/what customers see immediately/i)).toBeInTheDocument();

      await user.click(screen.getByRole('button', { name: 'Yes, mark out of stock' }));

      expect(setBeerAvailabilityViaPin).toHaveBeenCalledWith(7, '123456', 'OutOfStock');
      expect(await screen.findByText(/marked out of stock/i)).toBeInTheDocument();
    });

    it('offers "mark available" for an out-of-stock beer', async () => {
      const user = userEvent.setup();
      setBeerAvailabilityViaPin.mockResolvedValue(undefined);
      const outOfStockBeer = { ...beer, availability: 'OutOfStock' };
      render(<ConfirmPinPad beer={outOfStockBeer} onClose={() => {}} />);

      await user.type(screen.getByLabelText('Bartender PIN'), '123456');
      await user.click(screen.getByRole('button', { name: 'Mark this beer as available' }));
      await user.click(screen.getByRole('button', { name: 'Yes, mark available' }));

      expect(setBeerAvailabilityViaPin).toHaveBeenCalledWith(7, '123456', 'Available');
      expect(await screen.findByText(/marked available/i)).toBeInTheDocument();
    });

    it('cancels back to the closed state without submitting', async () => {
      const user = userEvent.setup();
      render(<ConfirmPinPad beer={beer} onClose={() => {}} />);

      await user.click(screen.getByRole('button', { name: 'Mark this beer as out of stock' }));
      // Two Cancel buttons exist while the availability panel is open: the main form's
      // and this panel's — the panel's is the one rendered after opening it.
      await user.click(screen.getAllByRole('button', { name: 'Cancel' })[1]);

      expect(setBeerAvailabilityViaPin).not.toHaveBeenCalled();
      expect(screen.getByRole('button', { name: 'Mark this beer as out of stock' })).toBeInTheDocument();
    });

    it('requires a PIN to already be typed before submitting the availability change', async () => {
      const user = userEvent.setup();
      render(<ConfirmPinPad beer={beer} onClose={() => {}} />);

      await user.click(screen.getByRole('button', { name: 'Mark this beer as out of stock' }));
      await user.click(screen.getByRole('button', { name: 'Yes, mark out of stock' }));

      expect(setBeerAvailabilityViaPin).not.toHaveBeenCalled();
      expect(await screen.findByText(/enter the bartender's pin above first/i)).toBeInTheDocument();
    });

    it('shows the API error and stays on the confirm step when the PIN is wrong', async () => {
      const user = userEvent.setup();
      setBeerAvailabilityViaPin.mockRejectedValue(new Error('Invalid PIN.'));
      render(<ConfirmPinPad beer={beer} onClose={() => {}} />);

      await user.type(screen.getByLabelText('Bartender PIN'), '000000');
      await user.click(screen.getByRole('button', { name: 'Mark this beer as out of stock' }));
      await user.click(screen.getByRole('button', { name: 'Yes, mark out of stock' }));

      expect(await screen.findByText('Invalid PIN.')).toBeInTheDocument();
      expect(screen.queryByText(/marked out of stock/i)).not.toBeInTheDocument();
    });

    it('shows a distinct offline message on a network failure', async () => {
      const user = userEvent.setup();
      const networkError = new Error('No network connection');
      networkError.isNetworkError = true;
      setBeerAvailabilityViaPin.mockRejectedValue(networkError);
      render(<ConfirmPinPad beer={beer} onClose={() => {}} />);

      await user.type(screen.getByLabelText('Bartender PIN'), '123456');
      await user.click(screen.getByRole('button', { name: 'Mark this beer as out of stock' }));
      await user.click(screen.getByRole('button', { name: 'Yes, mark out of stock' }));

      expect(await screen.findByText(/no signal — try again/i)).toBeInTheDocument();
    });
  });

  describe('#74: "How was it?" rating prompt and milestone moment', () => {
    it('shows the rating prompt on the success screen, skippable', async () => {
      confirmBeer.mockResolvedValue({ confirmedCount: 87, goal: 200, mugEarned: false, milestoneReached: false });
      const user = userEvent.setup();
      render(<ConfirmPinPad beer={beer} onClose={() => {}} />);

      await user.type(screen.getByLabelText('Bartender PIN'), '123456');
      await user.click(screen.getByRole('button', { name: 'Confirm' }));
      await screen.findByText('87 of 200');

      expect(screen.getByText('How was it?')).toBeInTheDocument();

      await user.click(screen.getByRole('button', { name: 'Skip' }));

      expect(screen.queryByText('How was it?')).not.toBeInTheDocument();
      expect(setMyRating).not.toHaveBeenCalled();
    });

    it('submits a rating and shows a thank-you message', async () => {
      confirmBeer.mockResolvedValue({ confirmedCount: 87, goal: 200, mugEarned: false, milestoneReached: false });
      setMyRating.mockResolvedValue(undefined);
      const user = userEvent.setup();
      render(<ConfirmPinPad beer={beer} onClose={() => {}} />);

      await user.type(screen.getByLabelText('Bartender PIN'), '123456');
      await user.click(screen.getByRole('button', { name: 'Confirm' }));
      await screen.findByText('87 of 200');

      await user.click(screen.getByRole('button', { name: 'Rate 5 stars' }));

      expect(setMyRating).toHaveBeenCalledWith(7, 5);
      expect(await screen.findByText(/thanks! rated ★5/i)).toBeInTheDocument();
      expect(screen.queryByText('How was it?')).not.toBeInTheDocument();
    });

    it('shows a milestone moment distinct from the mug, only when the mug was not also earned', async () => {
      confirmBeer.mockResolvedValue({ confirmedCount: 100, goal: 200, mugEarned: false, milestoneReached: true });
      const user = userEvent.setup();
      render(<ConfirmPinPad beer={beer} onClose={() => {}} />);

      await user.type(screen.getByLabelText('Bartender PIN'), '123456');
      await user.click(screen.getByRole('button', { name: 'Confirm' }));

      expect(await screen.findByText(/nice milestone/i)).toBeInTheDocument();
      expect(screen.queryByText('🏆 Mug earned!')).not.toBeInTheDocument();
    });

    it('does not show the milestone moment on an ordinary confirmation', async () => {
      confirmBeer.mockResolvedValue({ confirmedCount: 87, goal: 200, mugEarned: false, milestoneReached: false });
      const user = userEvent.setup();
      render(<ConfirmPinPad beer={beer} onClose={() => {}} />);

      await user.type(screen.getByLabelText('Bartender PIN'), '123456');
      await user.click(screen.getByRole('button', { name: 'Confirm' }));
      await screen.findByText('87 of 200');

      expect(screen.queryByText(/nice milestone/i)).not.toBeInTheDocument();
    });
  });
});
