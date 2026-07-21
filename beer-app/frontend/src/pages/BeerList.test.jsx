import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import BeerList from './BeerList';
import { searchBeers } from '../lib/api';

vi.mock('../lib/api');

function renderBeerList() {
  return render(
    <MemoryRouter>
      <BeerList />
    </MemoryRouter>
  );
}

const paleAle = {
  id: 1,
  name: 'Pale Ale',
  brewery: 'Sierra Nevada',
  style: 'American Pale Ale',
  availability: 'Available',
  confirmed: false,
};

const hefeweizen = {
  id: 2,
  name: 'Hefeweizen',
  brewery: 'Weihenstephaner',
  style: 'German Wheat Beer',
  availability: 'OnTap',
  confirmed: true,
};

describe('BeerList', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    vi.resetAllMocks();
  });

  it('shows a loading state before the beers resolve', () => {
    searchBeers.mockReturnValue(new Promise(() => {}));

    renderBeerList();

    expect(screen.getByText('Loading beers…')).toBeInTheDocument();
  });

  it('renders one entry per beer, with an availability badge and confirmed checkmark', async () => {
    searchBeers.mockResolvedValue({ items: [paleAle, hefeweizen], page: 1, pageSize: 200, totalCount: 2 });

    renderBeerList();

    expect(await screen.findByText('Pale Ale')).toBeInTheDocument();
    expect(screen.getByText('Hefeweizen')).toBeInTheDocument();
    expect(screen.getAllByText(/Sierra Nevada/).length).toBeGreaterThan(0);
    expect(screen.getAllByText('Available').length).toBeGreaterThan(0);
    expect(screen.getAllByText('On Tap').length).toBeGreaterThan(0);
    expect(screen.getByText('✓ Had it')).toBeInTheDocument();
  });

  it('shows an empty state when no beers match', async () => {
    searchBeers.mockResolvedValue({ items: [], page: 1, pageSize: 200, totalCount: 0 });

    renderBeerList();

    expect(await screen.findByText('No beers match.')).toBeInTheDocument();
  });

  it('shows the API error message when the search fails', async () => {
    searchBeers.mockRejectedValue(new Error('Failed to load beers'));

    renderBeerList();

    expect(await screen.findByText('Failed to load beers')).toBeInTheDocument();
  });

  it('debounces typed search input before calling the API', async () => {
    const user = userEvent.setup();
    searchBeers.mockResolvedValue({ items: [paleAle], page: 1, pageSize: 200, totalCount: 1 });

    renderBeerList();
    await screen.findByText('Pale Ale');
    searchBeers.mockClear();

    await user.type(screen.getByPlaceholderText('Search by name, brewery, or style'), 'pale');

    await waitFor(() => {
      expect(searchBeers).toHaveBeenCalledWith(expect.objectContaining({ search: 'pale' }));
    });
  });

  it('clicking an availability chip refetches with that availability', async () => {
    const user = userEvent.setup();
    searchBeers.mockResolvedValue({ items: [paleAle], page: 1, pageSize: 200, totalCount: 1 });

    renderBeerList();
    await screen.findByText('Pale Ale');
    searchBeers.mockClear();

    await user.click(screen.getByRole('button', { name: 'Out of Stock' }));

    await waitFor(() => {
      expect(searchBeers).toHaveBeenCalledWith(expect.objectContaining({ availability: 'OutOfStock' }));
    });
  });

  it('hides the had/not-had filter when signed out, shows it when signed in', async () => {
    searchBeers.mockResolvedValue({ items: [paleAle], page: 1, pageSize: 200, totalCount: 1 });
    renderBeerList();
    await screen.findByText('Pale Ale');

    expect(screen.queryByRole('button', { name: 'Had it' })).not.toBeInTheDocument();

    localStorage.setItem('beer-token', 'abc123');
    renderBeerList();
    await screen.findAllByText('Pale Ale');

    expect(screen.getByRole('button', { name: 'Had it' })).toBeInTheDocument();
  });

  it('clicking a had-status chip refetches with that hadStatus, only when signed in', async () => {
    const user = userEvent.setup();
    localStorage.setItem('beer-token', 'abc123');
    searchBeers.mockResolvedValue({ items: [paleAle], page: 1, pageSize: 200, totalCount: 1 });

    renderBeerList();
    await screen.findByText('Pale Ale');
    searchBeers.mockClear();

    await user.click(screen.getByRole('button', { name: 'Had it' }));

    await waitFor(() => {
      expect(searchBeers).toHaveBeenCalledWith(expect.objectContaining({ hadStatus: 'had' }));
    });
  });

  it('clicking a style quick-filter chip fills the search box and refetches', async () => {
    const user = userEvent.setup();
    searchBeers.mockResolvedValue({ items: [paleAle, hefeweizen], page: 1, pageSize: 200, totalCount: 2 });

    renderBeerList();
    await screen.findByText('Pale Ale');
    searchBeers.mockClear();

    await user.click(screen.getByRole('button', { name: 'American Pale Ale' }));

    expect(screen.getByPlaceholderText('Search by name, brewery, or style')).toHaveValue('American Pale Ale');
    await waitFor(() => {
      expect(searchBeers).toHaveBeenCalledWith(expect.objectContaining({ search: 'American Pale Ale' }));
    });
  });
});
