import { NavLink } from 'react-router-dom';
import { LayoutDashboard, Tags, Package, ShoppingBag, LogOut } from 'lucide-react';
import { useAuth } from 'react-oidc-context';

export default function Navigation() {
  const auth = useAuth();

  const navItems = [
    { path: '/', icon: LayoutDashboard, label: 'Dashboard' },
    { path: '/categories', icon: Tags, label: 'Categories' },
    { path: '/products', icon: Package, label: 'Products' },
    { path: '/orders', icon: ShoppingBag, label: 'Orders' },
  ];

  return (
    <nav className="w-64 bg-white border-r border-neutral-200 flex flex-col">
      <div className="p-6 border-b border-neutral-200">
        <h1 className="text-2xl font-bold gradient-text">UrbanX</h1>
        <p className="text-sm text-neutral-500 mt-1">Merchant Portal</p>
      </div>

      <div className="flex-1 py-6">
        {navItems.map((item) => {
          const Icon = item.icon;
          return (
            <NavLink
              key={item.path}
              to={item.path}
              end={item.path === '/'}
              className={({ isActive }) =>
                `flex items-center gap-3 px-6 py-3 text-sm font-medium transition-colors ${
                  isActive
                    ? 'text-primary-600 bg-primary-50 border-r-2 border-primary-600'
                    : 'text-neutral-600 hover:text-primary-600 hover:bg-neutral-50'
                }`
              }
            >
              <Icon className="w-5 h-5" />
              <span>{item.label}</span>
            </NavLink>
          );
        })}
      </div>

      <div className="p-6 border-t border-neutral-200">
        <div className="mb-4">
          <p className="text-sm font-medium text-neutral-900">
            {auth.user?.profile.name || auth.user?.profile.email}
          </p>
          <p className="text-xs text-neutral-500">Merchant Account</p>
        </div>
        <button
          onClick={() => auth.signoutRedirect()}
          className="flex items-center gap-2 text-sm text-neutral-600 hover:text-primary-600 transition-colors"
        >
          <LogOut className="w-4 h-4" />
          <span>Sign Out</span>
        </button>
      </div>
    </nav>
  );
}
