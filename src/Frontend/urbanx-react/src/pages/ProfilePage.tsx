import { Link, useLocation } from 'react-router-dom';
import { User, Package, MapPin, Settings, LogOut, ChevronRight } from 'lucide-react';
import { useAuth } from '../context/useAuth';

const ProfilePage = () => {
    const location = useLocation();
    const { user, logout } = useAuth();

    const displayName = user?.profile.name ?? user?.profile.email ?? 'User';
    const email = user?.profile.email ?? '';
    const avatarUrl = `https://ui-avatars.com/api/?name=${encodeURIComponent(displayName)}&background=0D8ABC&color=fff`;

    const isActive = (path: string) => location.pathname === path;

    const navItems = [
        { path: '/profile', label: 'Overview', icon: User },
        { path: '/orders', label: 'Order History', icon: Package },
        { path: '/profile/addresses', label: 'Addresses', icon: MapPin },
        { path: '/profile/settings', label: 'Account Settings', icon: Settings },
    ];

    return (
        <div className="container mx-auto px-6 py-12 min-h-[70vh]">
            <div className="flex flex-col md:flex-row gap-12">
                {/* Sidebar */}
                <aside className="w-full md:w-64 flex-shrink-0">
                    <div className="bg-white rounded-lg border border-gray-100 shadow-sm p-6 mb-8 text-center">
                        <div className="w-20 h-20 rounded-full mx-auto mb-4 overflow-hidden border-2 border-gray-100">
                            <img src={avatarUrl} alt={displayName} className="w-full h-full object-cover" />
                        </div>
                        <h2 className="font-serif font-bold text-lg text-gray-900">{displayName}</h2>
                        <p className="text-sm text-gray-500 mb-2">{email}</p>
                    </div>

                    <nav className="bg-white rounded-lg border border-gray-100 shadow-sm overflow-hidden">
                        {navItems.map(item => (
                            <Link
                                key={item.path}
                                to={item.path}
                                className={`flex items-center gap-3 px-6 py-4 text-sm font-medium transition-colors border-b border-gray-50 last:border-0 ${isActive(item.path)
                                    ? 'bg-gray-50 text-secondary border-l-4 border-l-secondary'
                                    : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900 border-l-4 border-l-transparent'
                                    }`}
                            >
                                <item.icon size={18} />
                                <span className="flex-1">{item.label}</span>
                                <ChevronRight size={14} className="text-gray-300" />
                            </Link>
                        ))}
                        <button
                            className="w-full flex items-center gap-3 px-6 py-4 text-sm font-medium text-red-500 hover:bg-red-50 transition-colors border-l-4 border-l-transparent text-left"
                            onClick={logout}
                        >
                            <LogOut size={18} />
                            <span>Sign Out</span>
                        </button>
                    </nav>
                </aside>

                {/* Main Content Area */}
                <main className="flex-1">
                    <div className="bg-white rounded-lg border border-gray-100 shadow-sm p-8">
                        <h1 className="text-2xl font-serif font-bold text-gray-900 mb-2">Hello, {displayName.split(' ')[0]}!</h1>
                        <p className="text-gray-500 mb-8">This is your profile dashboard. You can view your recent orders, manage your shipping addresses, and edit your password and account details.</p>

                        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                            <div className="bg-gray-50 rounded-lg p-6 border border-gray-100">
                                <div className="flex items-center gap-3 mb-2 text-primary">
                                    <Package size={24} />
                                    <h3 className="font-bold">Total Orders</h3>
                                </div>
                                <p className="text-3xl font-bold text-gray-900">12</p>
                                <Link to="/orders" className="text-xs font-semibold uppercase tracking-wider text-secondary hover:text-primary mt-4 inline-block">View All Orders</Link>
                            </div>

                            <div className="bg-gray-50 rounded-lg p-6 border border-gray-100">
                                <div className="flex items-center gap-3 mb-2 text-primary">
                                    <MapPin size={24} />
                                    <h3 className="font-bold">Saved Addresses</h3>
                                </div>
                                <p className="text-3xl font-bold text-gray-900">3</p>
                                <Link to="/profile/addresses" className="text-xs font-semibold uppercase tracking-wider text-secondary hover:text-primary mt-4 inline-block">Manage Addresses</Link>
                            </div>

                            <div className="bg-gray-50 rounded-lg p-6 border border-gray-100">
                                <div className="flex items-center gap-3 mb-2 text-primary">
                                    <Settings size={24} />
                                    <h3 className="font-bold">Account Status</h3>
                                </div>
                                <p className="text-sm font-bold text-green-600 uppercase tracking-wider mb-1">Active</p>
                                <p className="text-xs text-gray-400">Identify Verified</p>
                            </div>
                        </div>
                    </div>
                </main>
            </div>
        </div>
    );
};

export default ProfilePage;
