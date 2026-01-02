import React, { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Save } from 'lucide-react';

const AddressEditPage = () => {
    const navigate = useNavigate();
    const { id } = useParams();
    const isEditing = Boolean(id);

    const [formData, setFormData] = useState({
        type: 'Home',
        name: 'John Doe',
        street: '',
        city: '',
        zip: '',
        country: 'USA',
        isDefault: false
    });

    useEffect(() => {
        if (isEditing) {
            // Mock fetch data if editing
            if (id === '1') {
                setFormData({
                    type: 'Home',
                    name: 'John Doe',
                    street: '123 Luxury St',
                    city: 'New York',
                    zip: '10001',
                    country: 'USA',
                    isDefault: true
                });
            } else if (id === '2') {
                setFormData({
                    type: 'Office',
                    name: 'John Doe',
                    street: '456 Business Blvd',
                    city: 'San Francisco',
                    zip: '94105',
                    country: 'USA',
                    isDefault: false
                });
            }
        }
    }, [id, isEditing]);

    const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
        const { name, value, type } = e.target;
        const checked = (e.target as HTMLInputElement).checked;

        setFormData(prev => ({
            ...prev,
            [name]: type === 'checkbox' ? checked : value
        }));
    };

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        // Here you would send data to API
        console.log('Saving address:', formData);
        navigate('/profile/addresses');
    };

    return (
        <div className="container mx-auto px-6 py-12">
            <button className="flex items-center gap-2 text-sm text-gray-500 hover:text-primary transition-colors mb-8" onClick={() => navigate('/profile/addresses')}>
                <ArrowLeft size={16} /> Back to Addresses
            </button>

            <div className="border-b border-gray-100 pb-8 mb-8">
                <h1 className="text-3xl font-serif font-bold text-gray-900 mb-2">{isEditing ? 'Edit Address' : 'Add New Address'}</h1>
                <p className="text-gray-500">{isEditing ? 'Update your shipping details.' : 'Add a new destination.'}</p>
            </div>

            <div className="max-w-2xl bg-white border border-gray-100 rounded-lg p-8 shadow-sm">
                <form onSubmit={handleSubmit} className="space-y-6">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                        <div className="flex flex-col gap-2 md:col-span-2">
                            <label className="text-xs font-bold uppercase tracking-wider text-gray-500">Address Type (e.g., Home, Office)</label>
                            <input type="text" name="type" value={formData.type} onChange={handleChange} placeholder="Home" required className="w-full border border-gray-300 rounded-md px-4 py-3 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary transition-all" />
                        </div>
                        <div className="flex flex-col gap-2 md:col-span-2">
                            <label className="text-xs font-bold uppercase tracking-wider text-gray-500">Full Name</label>
                            <input type="text" name="name" value={formData.name} onChange={handleChange} required className="w-full border border-gray-300 rounded-md px-4 py-3 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary transition-all" />
                        </div>
                        <div className="flex flex-col gap-2 md:col-span-2">
                            <label className="text-xs font-bold uppercase tracking-wider text-gray-500">Street Address</label>
                            <input type="text" name="street" value={formData.street} onChange={handleChange} placeholder="123 Main St" required className="w-full border border-gray-300 rounded-md px-4 py-3 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary transition-all" />
                        </div>
                        <div className="flex flex-col gap-2">
                            <label className="text-xs font-bold uppercase tracking-wider text-gray-500">City</label>
                            <input type="text" name="city" value={formData.city} onChange={handleChange} required className="w-full border border-gray-300 rounded-md px-4 py-3 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary transition-all" />
                        </div>
                        <div className="flex flex-col gap-2">
                            <label className="text-xs font-bold uppercase tracking-wider text-gray-500">ZIP Code</label>
                            <input type="text" name="zip" value={formData.zip} onChange={handleChange} required className="w-full border border-gray-300 rounded-md px-4 py-3 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary transition-all" />
                        </div>
                        <div className="flex flex-col gap-2">
                            <label className="text-xs font-bold uppercase tracking-wider text-gray-500">Country</label>
                            <select name="country" value={formData.country} onChange={handleChange} className="w-full border border-gray-300 rounded-md px-4 py-3 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary transition-all bg-white">
                                <option value="USA">United States</option>
                                <option value="CAN">Canada</option>
                                <option value="UK">United Kingdom</option>
                            </select>
                        </div>
                    </div>

                    <div className="pt-6 border-t border-gray-100">
                        <label className="flex items-center gap-3 cursor-pointer">
                            <input
                                type="checkbox"
                                name="isDefault"
                                checked={formData.isDefault}
                                onChange={handleChange}
                                className="w-5 h-5 text-primary border-gray-300 rounded focus:ring-primary cursor-pointer accent-primary"
                            />
                            <span className="text-sm font-medium text-gray-700">Set as default shipping address</span>
                        </label>
                    </div>

                    <div className="pt-6">
                        <button type="submit" className="flex items-center justify-center gap-2 bg-primary text-white px-8 py-3 rounded-md font-medium hover:bg-gray-900 transition-colors shadow-lg shadow-gray-200">
                            <Save size={18} /> Save Address
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
};

export default AddressEditPage;
