import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import AdminBeers from './AdminBeers';
import { deleteBeer, getRolesFromToken, searchBeers, updateBeerAvailability } from '../lib/api';

vi.mock('../lib/api');

const beers = [
  { id: 1, name: 'Duvel', brewery: 'Duvel Moortgat', style: 'Belgian Strong Golden Ale', availability: 'OnTap' },
  { id: 2, name: 'Orval', brewery: "Brasserie d'Orval", style: 'Belgian Pale Ale', availability: 'Retired' },
];

function renderPage() {
  return render(
    <MemoryRouter>
      <AdminBeers />
    </MemoryRouter>
  );
}

describe('AdminBeers', () => {
  beforeEach(() => {
    localStorage.setItem('beer-token', 'abc');
    getRolesFromToken.mockReturnValue(['Admin']);
    searchBeers.mockResolvedValue({ items: beers, page: 1, pageSize: 200, totalCount: 2 });
  });

  afterEach(() => {
    localStorage.clear();
    vi.resetAllMocks();
  });

  it('turns non-admins away without loading anything', () => {
    getRolesFromToken.mockReturnValue(['Customer']);

    renderPage();

    expect(screen.getByText(/admin account/i)).toBeInTheDocument();
    expect(searchBeers).not.toHaveBeenCalled();
  });

  it('shows the beer list with name, brewery, style, and availability', async () => {
    renderPage();

    expect(await screen.findByText('Duvel')).toBeInTheDocument();
    expect(screen.getByText('Duvel Moortgat')).toBeInTheDocument();
    expect(screen.getByText('Belgian Strong Golden Ale')).toBeInTheDocument();
    expect(screen.getByDisplayValue('OnTap')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Retired')).toBeInTheDocument();
  });

  it('requests all availability states, not just in-stock', async () => {
    renderPage();
    await screen.findByText('Duvel');

    expect(searchBeers).toHaveBeenCalledWith(expect.objectContaining({ availability: 'all' }));
  });

  it('changes availability immediately on select, with no confirm step', async () => {
    const user = userEvent.setup();
    updateBeerAvailability.mockResolvedValue(undefined);
    renderPage();
    await screen.findByText('Duvel');

    await user.selectOptions(screen.getByDisplayValue('OnTap'), 'OutOfStock');

    expect(updateBeerAvailability).toHaveBeenCalledWith(1, 'OutOfStock');
  });

  it('shows a general note on availability/delete behavior, and delete-step microcopy at the delete step, not before', async () => {
    const user = userEvent.setup();
    renderPage();
    await screen.findByText('Duvel');

    expect(screen.getByText(/availability changes apply immediately/i)).toBeInTheDocument();
    expect(screen.queryByText(/delete will fail/i)).not.toBeInTheDocument();

    await user.click(screen.getAllByRole('button', { name: 'Delete' })[0]);

    expect(screen.getByText(/delete will fail/i)).toBeInTheDocument();
  });

  it('requires a reason and an explicit confirm before deleting', async () => {
    const user = userEvent.setup();
    renderPage();
    await screen.findByText('Duvel');

    await user.click(screen.getAllByRole('button', { name: 'Delete' })[0]);
    expect(deleteBeer).not.toHaveBeenCalled();

    await user.click(screen.getByRole('button', { name: 'Confirm delete' }));
    expect(deleteBeer).not.toHaveBeenCalled();
    expect(await screen.findByText(/reason is required/i)).toBeInTheDocument();

    deleteBeer.mockResolvedValue(undefined);
    await user.type(screen.getByPlaceholderText(/reason/i), 'discontinued by brewery');
    await user.click(screen.getByRole('button', { name: 'Confirm delete' }));

    expect(deleteBeer).toHaveBeenCalledWith(1, 'discontinued by brewery');
  });

  it('links Add Beer and each row Edit to the existing BeerForm routes', async () => {
    renderPage();
    await screen.findByText('Duvel');

    expect(screen.getByRole('link', { name: 'Add Beer' })).toHaveAttribute('href', '/beers/new');
    expect(screen.getAllByRole('link', { name: 'Edit' })[0]).toHaveAttribute('href', '/beers/1/edit');
  });

  it('surfaces API errors from a failed action', async () => {
    const user = userEvent.setup();
    updateBeerAvailability.mockRejectedValue(new Error('Beer not found.'));
    renderPage();
    await screen.findByText('Duvel');

    await user.selectOptions(screen.getByDisplayValue('OnTap'), 'OutOfStock');

    expect(await screen.findByText('Beer not found.')).toBeInTheDocument();
  });
});
