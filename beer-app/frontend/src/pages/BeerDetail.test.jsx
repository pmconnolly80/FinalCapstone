import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import BeerDetail from './BeerDetail';
import { fetchBeer, setMyRating } from '../lib/api';

vi.mock('../lib/api');

function renderBeerDetail(id = '1') {
  return render(
    <MemoryRouter initialEntries={[`/beers/${id}`]}>
      <Routes>
        <Route path="/beers/:id" element={<BeerDetail />} />
      </Routes>
    </MemoryRouter>
  );
}

describe('BeerDetail', () => {
  afterEach(() => {
    vi.resetAllMocks();
  });

  it('shows a loading state before the beer resolves', () => {
    fetchBeer.mockReturnValue(new Promise(() => {}));

    renderBeerDetail();

    expect(screen.getByText('Loading...')).toBeInTheDocument();
  });

  it('renders the beer once loaded', async () => {
    fetchBeer.mockResolvedValue({
      id: 1,
      name: 'Duvel',
      brewery: 'Duvel Moortgat',
      style: 'Belgian Strong Golden Ale',
      description: 'Deceptively light-bodied with a dry, spicy finish.',
    });

    renderBeerDetail('1');

    expect(await screen.findByText('Duvel')).toBeInTheDocument();
    expect(screen.getByText('Duvel Moortgat')).toBeInTheDocument();
    expect(screen.getByText('Belgian Strong Golden Ale')).toBeInTheDocument();
    expect(fetchBeer).toHaveBeenCalledWith('1');
  });

  it('hides the confirm action when the customer is not signed in', async () => {
    localStorage.clear();
    fetchBeer.mockResolvedValue({ id: 1, name: 'Duvel', brewery: 'Duvel Moortgat', style: 'Ale' });

    renderBeerDetail('1');

    expect(await screen.findByText('Duvel')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Confirm with bartender' })).not.toBeInTheDocument();
  });

  it('shows the confirm action for a signed-in customer', async () => {
    localStorage.setItem('beer-token', 'abc');
    fetchBeer.mockResolvedValue({ id: 1, name: 'Duvel', brewery: 'Duvel Moortgat', style: 'Ale' });

    renderBeerDetail('1');

    expect(await screen.findByRole('button', { name: 'Confirm with bartender' })).toBeInTheDocument();
    localStorage.clear();
  });

  it('renders the beer-nerd stats block when present', async () => {
    fetchBeer.mockResolvedValue({
      id: 1,
      name: 'Duvel',
      brewery: 'Duvel Moortgat',
      style: 'Belgian Strong Golden Ale',
      abv: 8.5,
      ibu: 30,
      styleFamily: 'Strong Golden Ale',
      class: 'Ale',
    });

    renderBeerDetail('1');

    expect(await screen.findByText('8.5%')).toBeInTheDocument();
    expect(screen.getByText('30')).toBeInTheDocument();
    expect(screen.getByText('Strong Golden Ale')).toBeInTheDocument();
    expect(screen.getByText('Ale')).toBeInTheDocument();
  });

  it('omits the nerd stats block when no nerd stats are present', async () => {
    fetchBeer.mockResolvedValue({ id: 1, name: 'Duvel', brewery: 'Duvel Moortgat', style: 'Ale' });

    renderBeerDetail('1');

    expect(await screen.findByText('Duvel')).toBeInTheDocument();
    expect(screen.queryByText('ABV')).not.toBeInTheDocument();
  });

  it('renders the brewery card with a website link when present', async () => {
    fetchBeer.mockResolvedValue({
      id: 1,
      name: 'Duvel',
      brewery: 'Duvel Moortgat',
      style: 'Ale',
      breweryInfo: {
        id: 'obdb-1',
        name: 'Duvel Moortgat',
        breweryType: 'regional',
        city: 'Breendonk',
        state: 'Antwerp',
        websiteUrl: 'https://duvel.com',
      },
    });

    renderBeerDetail('1');

    expect(await screen.findByText(/regional/)).toBeInTheDocument();
    expect(screen.getByText(/Breendonk/)).toBeInTheDocument();
    const link = screen.getByRole('link', { name: 'Visit website' });
    expect(link).toHaveAttribute('href', 'https://duvel.com');
  });

  it('omits the brewery card when no brewery info is present', async () => {
    fetchBeer.mockResolvedValue({ id: 1, name: 'Duvel', brewery: 'Duvel Moortgat', style: 'Ale' });

    renderBeerDetail('1');

    expect(await screen.findByText('Duvel')).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Visit website' })).not.toBeInTheDocument();
  });

  it('shows a visible error message when the beer fails to load', async () => {
    fetchBeer.mockRejectedValue(new Error('nope'));

    renderBeerDetail('1');

    expect(await screen.findByText('Could not load this beer. Try again.')).toBeInTheDocument();
  });

  describe('#74: view/edit your rating', () => {
    it('omits the rating section for a beer the customer has not confirmed', async () => {
      fetchBeer.mockResolvedValue({ id: 1, name: 'Duvel', brewery: 'Duvel Moortgat', style: 'Ale', confirmed: false });

      renderBeerDetail('1');

      expect(await screen.findByText('Duvel')).toBeInTheDocument();
      expect(screen.queryByText('Your rating')).not.toBeInTheDocument();
    });

    it('shows the rating section for a confirmed beer, highlighting the existing rating', async () => {
      fetchBeer.mockResolvedValue({
        id: 1, name: 'Duvel', brewery: 'Duvel Moortgat', style: 'Ale', confirmed: true, myRating: 4,
      });

      renderBeerDetail('1');

      expect(await screen.findByText('Your rating')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Rate 4 stars' })).toHaveAttribute('aria-pressed', 'true');
      expect(screen.getByRole('button', { name: 'Rate 3 stars' })).toHaveAttribute('aria-pressed', 'false');
    });

    it('submits a new rating and highlights it once saved', async () => {
      const user = userEvent.setup();
      fetchBeer.mockResolvedValue({ id: 1, name: 'Duvel', brewery: 'Duvel Moortgat', style: 'Ale', confirmed: true, myRating: null });
      setMyRating.mockResolvedValue(undefined);

      renderBeerDetail('1');
      await screen.findByText('Your rating');

      await user.click(screen.getByRole('button', { name: 'Rate 5 stars' }));

      expect(setMyRating).toHaveBeenCalledWith('1', 5);
      expect(await screen.findByRole('button', { name: 'Rate 5 stars' })).toHaveAttribute('aria-pressed', 'true');
    });

    it('surfaces an API error from a failed rating submission', async () => {
      const user = userEvent.setup();
      fetchBeer.mockResolvedValue({ id: 1, name: 'Duvel', brewery: 'Duvel Moortgat', style: 'Ale', confirmed: true, myRating: null });
      setMyRating.mockRejectedValue(new Error('Rating must be between 1 and 5.'));

      renderBeerDetail('1');
      await screen.findByText('Your rating');

      await user.click(screen.getByRole('button', { name: 'Rate 5 stars' }));

      expect(await screen.findByText('Rating must be between 1 and 5.')).toBeInTheDocument();
    });
  });
});
