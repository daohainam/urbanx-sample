import { Package, ShoppingBag, Tags, TrendingUp } from 'lucide-react';

export default function DashboardPage() {
  const stats = [
    {
      title: 'Total Products',
      value: '0',
      icon: Package,
      color: 'bg-primary-50 text-primary-600',
    },
    {
      title: 'Active Orders',
      value: '0',
      icon: ShoppingBag,
      color: 'bg-accent-50 text-accent-600',
    },
    {
      title: 'Categories',
      value: '0',
      icon: Tags,
      color: 'bg-blue-50 text-blue-600',
    },
    {
      title: 'Revenue',
      value: '$0',
      icon: TrendingUp,
      color: 'bg-green-50 text-green-600',
    },
  ];

  return (
    <div className="animate-fade-in">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-neutral-900 mb-2">Dashboard</h1>
        <p className="text-neutral-600">Welcome to your merchant portal</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
        {stats.map((stat) => {
          const Icon = stat.icon;
          return (
            <div
              key={stat.title}
              className="bg-white rounded-lg shadow-soft p-6 border border-neutral-200"
            >
              <div className="flex items-center justify-between mb-4">
                <div className={`p-3 rounded-lg ${stat.color}`}>
                  <Icon className="w-6 h-6" />
                </div>
              </div>
              <p className="text-2xl font-bold text-neutral-900 mb-1">
                {stat.value}
              </p>
              <p className="text-sm text-neutral-600">{stat.title}</p>
            </div>
          );
        })}
      </div>

      <div className="bg-white rounded-lg shadow-soft p-8 border border-neutral-200">
        <h2 className="text-xl font-semibold text-neutral-900 mb-4">
          Quick Actions
        </h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <a
            href="/products"
            className="flex items-center gap-3 p-4 rounded-lg border border-neutral-200 hover:border-primary-300 hover:bg-primary-50 transition-colors"
          >
            <Package className="w-5 h-5 text-primary-600" />
            <span className="font-medium text-neutral-900">Manage Products</span>
          </a>
          <a
            href="/categories"
            className="flex items-center gap-3 p-4 rounded-lg border border-neutral-200 hover:border-primary-300 hover:bg-primary-50 transition-colors"
          >
            <Tags className="w-5 h-5 text-primary-600" />
            <span className="font-medium text-neutral-900">Manage Categories</span>
          </a>
          <a
            href="/orders"
            className="flex items-center gap-3 p-4 rounded-lg border border-neutral-200 hover:border-primary-300 hover:bg-primary-50 transition-colors"
          >
            <ShoppingBag className="w-5 h-5 text-primary-600" />
            <span className="font-medium text-neutral-900">View Orders</span>
          </a>
        </div>
      </div>
    </div>
  );
}
