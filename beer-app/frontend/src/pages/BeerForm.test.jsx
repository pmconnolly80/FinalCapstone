import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import BeerForm from './BeerForm';
import { fetchBeer, saveBeer, searchBreweries, searchCatalogBeer } from '../lib/api';

// getRolesFromToken needs its real implementation — the admin-only gate (#32) is under
// test here, so it must actually read the role out of whatever token is in localStorage.
vi.mock('../lib/api', async (importOriginal) => {
  const actual = await importOriginal();
  return {
    ...actual,
    fetchBeer: vi.fn(),
    saveBeer: vi.fn(),
    searchBreweries: vi.fn(),
    searchCatalogBeer: vi.fn(),
  };
});

function fakeJwt(payload) {
  return `header.${btoa(JSON.stringify(payload))}.signature`;
}

const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

function renderAt(path) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/beers/new" element={<BeerForm />} />
        <Route path="/beers/:id/edit" element={<BeerForm />} />
        <Route path="/admin/beers" element={<div>Admin Beers Page</div>} />
      </Routes>
    </MemoryRouter>
  );
}

describe('BeerForm', () => {
  beforeEach(() => {
    localStorage.setItem('beer-token', fakeJwt({ [roleClaim]: 'Admin' }));
  });

  afterEach(() => {
    vi.resetAllMocks();
    localStorage.clear();
  });

  it('shows an admin-only message and does not load anything for a signed-out visitor', () => {
    localStorage.clear();

    renderAt('/beers/new');

    expect(screen.getByText(/sign in with an admin account/i)).toBeInTheDocument();
    expect(fetchBeer).not.toHaveBeenCalled();
  });

  it('shows an admin-only message for a signed-in customer', () => {
    localStorage.setItem('beer-token', fakeJwt({ [roleClaim]: 'Customer' }));

    renderAt('/beers/new');

    expect(screen.getByText(/sign in with an admin account/i)).toBeInTheDocument();
  });

  it('submits the entered values and navigates back to the list on success', async () => {
    saveBeer.mockResolvedValue({ id: 9 });
    const user = userEvent.setup();

    renderAt('/beers/new');
    await user.type(screen.getByPlaceholderText('Name'), 'Duvel');
    await user.type(screen.getByPlaceholderText('Brewery'), 'Duvel Moortgat');
    await user.type(screen.getByPlaceholderText('Style'), 'Belgian Strong Golden Ale');
    await user.click(screen.getByRole('button', { name: 'Save' }));

    expect(saveBeer).toHaveBeenCalledWith(
      expect.objectContaining({
        name: 'Duvel',
        brewery: 'Duvel Moortgat',
        style: 'Belgian Strong Golden Ale',
      }),
      undefined
    );
    expect(await screen.findByText('Admin Beers Page')).toBeInTheDocument();
  });

  it('loads and pre-fills the existing beer in edit mode', async () => {
    fetchBeer.mockResolvedValue({
      name: 'Fat Tire',
      brewery: 'New Belgium',
      style: 'Amber Ale',
      description: 'Toasty malt sweetness.',
    });

    renderAt('/beers/42/edit');

    expect(await screen.findByDisplayValue('Fat Tire')).toBeInTheDocument();
    expect(screen.getByDisplayValue('New Belgium')).toBeInTheDocument();
    expect(fetchBeer).toHaveBeenCalledWith('42');
  });

  it('submits blank beer-nerd stat fields as null rather than empty strings', async () => {
    saveBeer.mockResolvedValue({ id: 9 });
    const user = userEvent.setup();

    renderAt('/beers/new');
    await user.type(screen.getByPlaceholderText('Name'), 'Duvel');
    await user.type(screen.getByPlaceholderText('Brewery'), 'Duvel Moortgat');
    await user.type(screen.getByPlaceholderText('Style'), 'Belgian Strong Golden Ale');
    await user.click(screen.getByRole('button', { name: 'Save' }));

    expect(saveBeer).toHaveBeenCalledWith(
      expect.objectContaining({ abv: null, ibu: null, styleFamily: null, class: null }),
      undefined
    );
  });

  it('submits entered beer-nerd stats as numbers', async () => {
    saveBeer.mockResolvedValue({ id: 9 });
    const user = userEvent.setup();

    renderAt('/beers/new');
    await user.type(screen.getByPlaceholderText('Name'), 'Duvel');
    await user.type(screen.getByPlaceholderText('Brewery'), 'Duvel Moortgat');
    await user.type(screen.getByPlaceholderText('Style'), 'Belgian Strong Golden Ale');
    await user.type(screen.getByPlaceholderText('ABV %'), '8.5');
    await user.type(screen.getByPlaceholderText('IBU'), '30');
    await user.type(screen.getByPlaceholderText('Style family'), 'Strong Golden Ale');
    await user.selectOptions(screen.getByLabelText('Class'), 'Ale');
    await user.click(screen.getByRole('button', { name: 'Save' }));

    expect(saveBeer).toHaveBeenCalledWith(
      expect.objectContaining({ abv: 8.5, ibu: 30, styleFamily: 'Strong Golden Ale', class: 'Ale' }),
      undefined
    );
  });

  it('pre-fills beer-nerd stats in edit mode when present', async () => {
    fetchBeer.mockResolvedValue({
      name: 'Fat Tire',
      brewery: 'New Belgium',
      style: 'Amber Ale',
      abv: 5.2,
      ibu: 18,
      styleFamily: 'Amber',
      class: 'Ale',
    });

    renderAt('/beers/42/edit');

    expect(await screen.findByDisplayValue('5.2')).toBeInTheDocument();
    expect(screen.getByDisplayValue('18')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Amber')).toBeInTheDocument();
    expect(screen.getByLabelText('Class')).toHaveValue('Ale');
  });

  it('selecting a brewery suggestion fills the field and stores its OBDB id', async () => {
    const user = userEvent.setup();
    searchBreweries.mockResolvedValue([
      { id: 'obdb-1', name: 'Duvel Moortgat', city: 'Breendonk', state: 'Antwerp' },
    ]);
    saveBeer.mockResolvedValue({ id: 9 });

    renderAt('/beers/new');
    await user.type(screen.getByPlaceholderText('Name'), 'Duvel');
    await user.type(screen.getByPlaceholderText('Brewery'), 'Duv');

    const suggestion = await screen.findByRole('button', { name: /Duvel Moortgat/ });
    await user.click(suggestion);

    expect(screen.getByPlaceholderText('Brewery')).toHaveValue('Duvel Moortgat');

    await user.type(screen.getByPlaceholderText('Style'), 'Ale');
    await user.click(screen.getByRole('button', { name: 'Save' }));

    expect(saveBeer).toHaveBeenCalledWith(
      expect.objectContaining({ brewery: 'Duvel Moortgat', obdbBreweryId: 'obdb-1' }),
      undefined
    );
  });

  it('editing the brewery field after a selection clears the stored OBDB id', async () => {
    const user = userEvent.setup();
    searchBreweries.mockResolvedValue([{ id: 'obdb-1', name: 'Duvel Moortgat' }]);
    saveBeer.mockResolvedValue({ id: 9 });

    renderAt('/beers/new');
    await user.type(screen.getByPlaceholderText('Name'), 'Duvel');
    await user.type(screen.getByPlaceholderText('Brewery'), 'Duv');
    const suggestion = await screen.findByRole('button', { name: /Duvel Moortgat/ });
    await user.click(suggestion);

    await user.type(screen.getByPlaceholderText('Brewery'), ' Extra');
    await user.type(screen.getByPlaceholderText('Style'), 'Ale');
    await user.click(screen.getByRole('button', { name: 'Save' }));

    expect(saveBeer).toHaveBeenCalledWith(
      expect.objectContaining({ obdbBreweryId: null }),
      undefined
    );
  });

  it('selecting a Catalog.beer suggestion pre-fills nerd stats and shows CC BY attribution', async () => {
    const user = userEvent.setup();
    searchCatalogBeer.mockResolvedValue([
      {
        id: 'cb-1',
        name: 'Duvel',
        style: 'Belgian-Style Tripel',
        styleFamily: 'Belgian Ale',
        class: 'Ale',
        description: 'Deceptively light-bodied with a dry, spicy finish.',
        abv: 8.5,
        ibu: null,
        cbVerified: true,
        brewerName: 'Duvel Moortgat',
      },
    ]);
    saveBeer.mockResolvedValue({ id: 9 });

    renderAt('/beers/new');
    await user.type(screen.getByPlaceholderText('Name'), 'Duv');

    const suggestion = await screen.findByRole('button', { name: /Duvel/ });
    await user.click(suggestion);

    expect(screen.getByPlaceholderText('Name')).toHaveValue('Duvel');
    expect(screen.getByPlaceholderText('Style')).toHaveValue('Belgian-Style Tripel');
    expect(screen.getByPlaceholderText('ABV %')).toHaveValue(8.5);
    expect(screen.getByPlaceholderText('Style family')).toHaveValue('Belgian Ale');
    expect(screen.getByLabelText('Class')).toHaveValue('Ale');
    expect(screen.getByText(/Catalog\.beer/)).toBeInTheDocument();

    await user.type(screen.getByPlaceholderText('Brewery'), 'Duvel Moortgat');
    await user.click(screen.getByRole('button', { name: 'Save' }));

    expect(saveBeer).toHaveBeenCalledWith(
      expect.objectContaining({
        name: 'Duvel',
        style: 'Belgian-Style Tripel',
        styleFamily: 'Belgian Ale',
        class: 'Ale',
        abv: 8.5,
        description: 'Deceptively light-bodied with a dry, spicy finish.',
      }),
      undefined
    );
  });

  it('shows a visible error message when saving fails', async () => {
    const user = userEvent.setup();
    saveBeer.mockRejectedValue(new Error('Failed to save beer'));

    renderAt('/beers/new');
    await user.type(screen.getByPlaceholderText('Name'), 'Duvel');
    await user.type(screen.getByPlaceholderText('Brewery'), 'Duvel Moortgat');
    await user.type(screen.getByPlaceholderText('Style'), 'Belgian Strong Golden Ale');
    await user.click(screen.getByRole('button', { name: 'Save' }));

    expect(await screen.findByText('Failed to save beer')).toBeInTheDocument();
  });

  it('shows a visible error message when loading the beer to edit fails', async () => {
    fetchBeer.mockRejectedValue(new Error('nope'));

    renderAt('/beers/42/edit');

    expect(
      await screen.findByText('Could not load this beer. Try again.')
    ).toBeInTheDocument();
  });

  it('marks Name, Brewery, and Style as required', () => {
    renderAt('/beers/new');

    expect(screen.getByPlaceholderText('Name')).toBeRequired();
    expect(screen.getByPlaceholderText('Brewery')).toBeRequired();
    expect(screen.getByPlaceholderText('Style')).toBeRequired();
  });
});
