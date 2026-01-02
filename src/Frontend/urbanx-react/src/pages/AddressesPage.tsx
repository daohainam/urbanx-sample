import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Plus, Edit2, Trash2, MapPin, ArrowLeft } from 'lucide-react';

const AddressesPage = () => {
    const navigate = useNavigate();
    // Mock addresses
    const [addresses, setAddresses] = useState([
        { id: 1, type: 'Home', name: 'John Doe', street: '123 Luxury St', city: 'New York', zip: '10001', country: 'USA', isDefault: true },
        { id: 2, type: 'Office', name: 'John Doe', street: '456 Business Blvd', city: 'San Francisco', zip: '94105', country: 'USA', isDefault: false },
    ]);

    const handleDelete = (id: number) => {
        setAddresses(addresses.filter(addr => addr.id !== id));
    };

    return (
        <div className="container mx-auto px-6 py-12">
            <button className="flex items-center gap-2 text-sm text-gray-500 hover:text-primary transition-colors mb-8" onClick={() => navigate('/profile')}>
                <ArrowLeft size={16} /> Back to Dashboard
            </button>

            <div className="flex flex-col md:flex-row justify-between items-start md:items-center border-b border-gray-100 pb-8 mb-8 gap-4">
                <div>
                    <h1 className="text-3xl font-serif font-bold text-gray-900 mb-2">Saved Addresses</h1>
                    <p className="text-gray-500">Manage your shipping and billing addresses.</p>
                </div>
                <button
                    className="flex items-center gap-2 bg-primary text-white px-6 py-3 rounded-md font-medium hover:bg-gray-900 transition-colors shadow-lg shadow-gray-200"
                    onClick={() => navigate('/profile/addresses/new')}
                >
                    <Plus size={18} /> Add New Address
                </button>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {addresses.map((addr) => (
                    <div key={addr.id} className={`bg-white border rounded-lg p-6 relative transition-all hover:shadow-md ${addr.isDefault ? 'border-secondary ring-1 ring-secondary bg-yellow-50/5' : 'border-gray-200'}`}>
                        <div className="flex justify-between items-start mb-4">
                            <span className="flex items-center gap-2 font-bold text-gray-900 uppercase tracking-wide text-sm">
                                <MapPin size={16} className={addr.isDefault ? 'text-secondary' : 'text-gray-400'} /> {addr.type}
                            </span>
                            {addr.isDefault && <span className="text-xs font-bold uppercase tracking-wider bg-secondary text-primary px-2 py-1 rounded">Default</span>}
                        </div>

                        <div className="text-gray-600 text-sm leading-relaxed mb-6">
                            <p className="font-semibold text-gray-900 mb-1">{addr.name}</p>
                            <p>{addr.street}</p>
                            <p>{addr.city}, {addr.zip}</p>
                            <p>{addr.country}</p>
                        </div>

                        <div className="flex gap-2 pt-4 border-t border-gray-100">
                            <button className="p-2 text-gray-400 hover:text-primary hover:bg-gray-50 rounded-full transition-colors" title="Edit" onClick={() => navigate(`/profile/addresses/${addr.id}`)}><Edit2 size={16} /></button>
                            <button className="p-2 text-gray-400 hover:text-red-500 hover:bg-red-50 rounded-full transition-colors" title="Delete" onClick={() => handleDelete(addr.id)}><Trash2 size={16} /></button>
                        </div>
                    </div>
                ))}
            </div>
        </div>
    );
};

export default AddressesPage;
