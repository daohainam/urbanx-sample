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

    const fieldClass =
        'w-full border border-gray-300 rounded-md px-4 py-3 focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary transition-all';
    const labelClass = 'text-xs font-bold uppercase tracking-wider text-gray-500';

    return (
        <div className="container mx-auto px-6 py-12">
            <title>Account Settings — UrbanX</title>
            <button
                type="button"
                className="flex items-center gap-2 text-sm text-gray-500 hover:text-primary transition-colors mb-8"
                onClick={() => navigate('/profile')}
            >
                <ArrowLeft size={16} aria-hidden="true" /> Back to Dashboard
            </button>

            <div className="border-b border-gray-100 pb-8 mb-8">
                <h1 className="text-3xl font-serif font-bold text-gray-900 mb-2">Account Settings</h1>
                <p className="text-gray-500">Update your personal information and security.</p>
            </div>

            <div className="max-w-2xl bg-white border border-gray-100 rounded-lg p-8 shadow-sm">
                <form onSubmit={handleSubmit} className="space-y-10">
                    <fieldset className="space-y-6">
                        <legend className="font-bold text-gray-900 border-b border-gray-100 pb-2 w-full">Personal Information</legend>
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                            <div className="flex flex-col gap-2">
                                <label htmlFor="firstName" className={labelClass}>First Name</label>
                                <input id="firstName" type="text" name="firstName" autoComplete="given-name" value={formData.firstName} onChange={handleChange} className={fieldClass} />
                            </div>
                            <div className="flex flex-col gap-2">
                                <label htmlFor="lastName" className={labelClass}>Last Name</label>
                                <input id="lastName" type="text" name="lastName" autoComplete="family-name" value={formData.lastName} onChange={handleChange} className={fieldClass} />
                            </div>
                            <div className="flex flex-col gap-2 md:col-span-2">
                                <label htmlFor="email" className={labelClass}>Email Address</label>
                                <input id="email" type="email" name="email" autoComplete="email" value={formData.email} onChange={handleChange} className={fieldClass} />
                            </div>
                        </div>
                    </fieldset>

                    <fieldset className="space-y-6">
                        <legend className="font-bold text-gray-900 border-b border-gray-100 pb-2 w-full">Security</legend>
                        <div className="flex flex-col gap-4">
                            <div className="flex flex-col gap-2">
                                <label htmlFor="currentPassword" className={labelClass}>Current Password</label>
                                <input id="currentPassword" type="password" name="currentPassword" autoComplete="current-password" value={formData.currentPassword} onChange={handleChange} className={fieldClass} />
                            </div>
                            <div className="flex flex-col gap-2">
                                <label htmlFor="newPassword" className={labelClass}>New Password</label>
                                <input id="newPassword" type="password" name="newPassword" autoComplete="new-password" value={formData.newPassword} onChange={handleChange} className={fieldClass} />
                            </div>
                            <div className="flex flex-col gap-2">
                                <label htmlFor="confirmPassword" className={labelClass}>Confirm New Password</label>
                                <input id="confirmPassword" type="password" name="confirmPassword" autoComplete="new-password" value={formData.confirmPassword} onChange={handleChange} className={fieldClass} />
                            </div>
                        </div>
                    </fieldset>

                    <div className="pt-4">
                        <button type="submit" className="bg-primary text-white px-8 py-3 rounded-md font-medium hover:bg-gray-900 transition-colors shadow-lg shadow-gray-200">Save Changes</button>
                    </div>
                </form>
            </div>
        </div>
    );
};

export default SettingsPage;
