import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';

const SettingsPage = () => {
    const navigate = useNavigate();
    const [formData, setFormData] = useState({
        firstName: 'John',
        lastName: 'Doe',
        email: 'john.doe@example.com',
        currentPassword: '',
        newPassword: '',
        confirmPassword: ''
    });

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setFormData(prev => ({ ...prev, [name]: value }));
    };

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        alert('Settings updated successfully!');
    };

    return (
        <div className="container mx-auto px-6 py-12">
            <button className="flex items-center gap-2 text-sm text-gray-500 hover:text-primary transition-colors mb-8" onClick={() => navigate('/profile')}>
                <ArrowLeft size={16} /> Back to Dashboard
            </button>

            <div className="border-b border-gray-100 pb-8 mb-8">
                <h1 className="text-3xl font-serif font-bold text-gray-900 mb-2">Account Settings</h1>
                <p className="text-gray-500">Update your personal information and security.</p>
            </div>

            <div className="max-w-2xl bg-white border border-gray-100 rounded-lg p-8 shadow-sm">
                <form onSubmit={handleSubmit} className="space-y-10">
                    <div className="space-y-6">
                        <h3 className="font-bold text-gray-900 border-b border-gray-100 pb-2">Personal Information</h3>
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                            <div className="flex flex-col gap-2">
                                <label className="text-xs font-bold uppercase tracking-wider text-gray-500">First Name</label>
                                <input type="text" name="firstName" value={formData.firstName} onChange={handleChange} className="w-full border border-gray-300 rounded-md px-4 py-3 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary transition-all" />
                            </div>
                            <div className="flex flex-col gap-2">
                                <label className="text-xs font-bold uppercase tracking-wider text-gray-500">Last Name</label>
                                <input type="text" name="lastName" value={formData.lastName} onChange={handleChange} className="w-full border border-gray-300 rounded-md px-4 py-3 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary transition-all" />
                            </div>
                            <div className="flex flex-col gap-2 md:col-span-2">
                                <label className="text-xs font-bold uppercase tracking-wider text-gray-500">Email Address</label>
                                <input type="email" name="email" value={formData.email} onChange={handleChange} className="w-full border border-gray-300 rounded-md px-4 py-3 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary transition-all" />
                            </div>
                        </div>
                    </div>

                    <div className="space-y-6">
                        <h3 className="font-bold text-gray-900 border-b border-gray-100 pb-2">Security</h3>
                        <div className="flex flex-col gap-4">
                            <div className="flex flex-col gap-2">
                                <label className="text-xs font-bold uppercase tracking-wider text-gray-500">Current Password</label>
                                <input type="password" name="currentPassword" value={formData.currentPassword} onChange={handleChange} className="w-full border border-gray-300 rounded-md px-4 py-3 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary transition-all" />
                            </div>
                            <div className="flex flex-col gap-2">
                                <label className="text-xs font-bold uppercase tracking-wider text-gray-500">New Password</label>
                                <input type="password" name="newPassword" value={formData.newPassword} onChange={handleChange} className="w-full border border-gray-300 rounded-md px-4 py-3 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary transition-all" />
                            </div>
                            <div className="flex flex-col gap-2">
                                <label className="text-xs font-bold uppercase tracking-wider text-gray-500">Confirm New Password</label>
                                <input type="password" name="confirmPassword" value={formData.confirmPassword} onChange={handleChange} className="w-full border border-gray-300 rounded-md px-4 py-3 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary transition-all" />
                            </div>
                        </div>
                    </div>

                    <div className="pt-4">
                        <button type="submit" className="bg-primary text-white px-8 py-3 rounded-md font-medium hover:bg-gray-900 transition-colors shadow-lg shadow-gray-200">Save Changes</button>
                    </div>
                </form>
            </div>
        </div>
    );
};

export default SettingsPage;
