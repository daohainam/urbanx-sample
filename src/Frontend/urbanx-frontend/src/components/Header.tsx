import { Link } from 'react-router-dom';
import { ShoppingCart, Package, Sparkles } from 'lucide-react';

export default function Header() {
  return (
    <header className="bg-gradient-to-r from-primary-600 via-primary-500 to-accent-500 text-white shadow-large sticky top-0 z-50 backdrop-blur-sm">
      <div className="container mx-auto px-6 py-4">
        <div className="flex items-center justify-between">
          <Link to="/" className="flex items-center gap-2 text-2xl font-bold hover:opacity-90 transition-opacity">
            <Sparkles className="w-7 h-7" />
            <span className="bg-white bg-clip-text text-transparent">UrbanX</span>
          </Link>
          
          <nav className="flex items-center space-x-8">
            <Link to="/" className="flex items-center gap-1.5 hover:opacity-80 transition-opacity font-medium">
              Catalog
            </Link>
            <Link to="/cart" className="flex items-center gap-1.5 hover:opacity-80 transition-opacity font-medium">
              <ShoppingCart className="w-5 h-5" />
              Cart
            </Link>
            <Link to="/orders" className="flex items-center gap-1.5 hover:opacity-80 transition-opacity font-medium">
              <Package className="w-5 h-5" />
              Orders
            </Link>
          </nav>
        </div>
      </div>
    </header>
  );
}
