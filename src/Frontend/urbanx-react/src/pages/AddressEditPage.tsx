import { useNavigate, useParams } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { ArrowLeft, Save } from 'lucide-react';
import { logger } from '../lib/logger';
import { savedAddressSchema, type SavedAddressForm } from '../schemas/address';
import { TextField } from '../components/forms/TextField';

// Mock address store for the editing flow. The real list comes from a profile/addresses
// endpoint once the merchant/customer profile API is finalised.
const MOCK_ADDRESSES: Record<string, SavedAddressForm> = {
    '1': { type: 'Home', name: 'John Doe', street: '123 Luxury St', city: 'New York', zip: '10001', country: 'USA', isDefault: true },
    '2': { type: 'Office', name: 'John Doe', street: '456 Business Blvd', city: 'San Francisco', zip: '94105', country: 'USA', isDefault: false },
};

const DEFAULT_VALUES: SavedAddressForm = {
    type: 'Home',
    name: '',
    street: '',
    city: '',
    zip: '',
    country: 'USA',
    isDefault: false,
};

const AddressEditPage = () => {
    const navigate = useNavigate();
    const { id } = useParams();
    const isEditing = Boolean(id);

    const {
        register,
        handleSubmit,
        formState: { errors, isSubmitting },
    } = useForm<SavedAddressForm>({
        resolver: zodResolver(savedAddressSchema),
        mode: 'onBlur',
        defaultValues: isEditing && id && MOCK_ADDRESSES[id] ? MOCK_ADDRESSES[id] : DEFAULT_VALUES,
    });

    const onSubmit = async (values: SavedAddressForm) => {
        // Simulate API call. Replace with addressService.save(values) once the endpoint exists.
        await new Promise((r) => setTimeout(r, 300));
        logger.info('Saving address (stub)', { values });
        navigate('/profile/addresses');
    };

    return (
        <div className="container mx-auto px-6 py-12">
            <title>{`${isEditing ? 'Edit' : 'New'} address — UrbanX`}</title>
            <button
                type="button"
                className="flex items-center gap-2 text-sm text-gray-500 hover:text-primary transition-colors mb-8"
                onClick={() => navigate('/profile/addresses')}
            >
                <ArrowLeft size={16} aria-hidden="true" /> Back to Addresses
            </button>

            <div className="border-b border-gray-100 pb-8 mb-8">
                <h1 className="text-3xl font-serif font-bold text-gray-900 mb-2">
                    {isEditing ? 'Edit Address' : 'Add New Address'}
                </h1>
                <p className="text-gray-500">
                    {isEditing ? 'Update your shipping details.' : 'Add a new destination.'}
                </p>
            </div>

            <div className="max-w-2xl bg-white border border-gray-100 rounded-lg p-8 shadow-sm">
                <form onSubmit={handleSubmit(onSubmit)} className="space-y-6" noValidate>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                        <div className="md:col-span-2">
                            <TextField
                                id="type"
                                label="Address Type (e.g., Home, Office)"
                                placeholder="Home"
                                error={errors.type?.message}
                                {...register('type')}
                            />
                        </div>
                        <div className="md:col-span-2">
                            <TextField
                                id="name"
                                label="Full Name"
                                autoComplete="name"
                                error={errors.name?.message}
                                {...register('name')}
                            />
                        </div>
                        <div className="md:col-span-2">
                            <TextField
                                id="street"
                                label="Street Address"
                                placeholder="123 Main St"
                                autoComplete="street-address"
                                error={errors.street?.message}
                                {...register('street')}
                            />
                        </div>
                        <TextField
                            id="city"
                            label="City"
                            autoComplete="address-level2"
                            error={errors.city?.message}
                            {...register('city')}
                        />
                        <TextField
                            id="zip"
                            label="ZIP Code"
                            autoComplete="postal-code"
                            error={errors.zip?.message}
                            {...register('zip')}
                        />
                        <div>
                            <label htmlFor="country" className="text-xs font-bold uppercase tracking-wider text-gray-500 block mb-2">
                                Country
                            </label>
                            <select
                                id="country"
                                aria-invalid={Boolean(errors.country)}
                                className={`w-full border rounded-md px-4 py-3 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary transition-all bg-white ${
                                    errors.country ? 'border-red-400' : 'border-gray-300'
                                }`}
                                {...register('country')}
                            >
                                <option value="USA">United States</option>
                                <option value="CAN">Canada</option>
                                <option value="UK">United Kingdom</option>
                            </select>
                            {errors.country && (
                                <p role="alert" className="mt-1.5 text-sm text-red-600">
                                    {errors.country.message}
                                </p>
                            )}
                        </div>
                    </div>

                    <div className="pt-6 border-t border-gray-100">
                        <label className="flex items-center gap-3 cursor-pointer">
                            <input
                                type="checkbox"
                                className="w-5 h-5 text-primary border-gray-300 rounded focus:ring-primary cursor-pointer accent-primary"
                                {...register('isDefault')}
                            />
                            <span className="text-sm font-medium text-gray-700">
                                Set as default shipping address
                            </span>
                        </label>
                    </div>

                    <div className="pt-6">
                        <button
                            type="submit"
                            disabled={isSubmitting}
                            className="flex items-center justify-center gap-2 bg-primary text-white px-8 py-3 rounded-md font-medium hover:bg-gray-900 transition-colors shadow-lg shadow-gray-200 disabled:opacity-60 disabled:cursor-not-allowed"
                        >
                            <Save size={18} /> {isSubmitting ? 'Saving...' : 'Save Address'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
};

export default AddressEditPage;
