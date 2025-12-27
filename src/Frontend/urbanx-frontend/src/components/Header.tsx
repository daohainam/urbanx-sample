import { Link } from 'react-router-dom';
import { ShoppingCart, Package } from 'lucide-react';

export default function Header() {
  return (
    <header className="bg-blue-600 text-white shadow-lg">
      <div className="container mx-auto px-4 py-4">
        <div className="flex items-center justify-between">
          <Link to="/" className="text-2xl font-bold hover:text-blue-100 transition">
            UrbanX
          </Link>
          
          <nav className="flex items-center space-x-6">
            <Link to="/" className="hover:text-blue-100 transition">
              Catalog
            </Link>
            <Link to="/cart" className="flex items-center hover:text-blue-100 transition">
              <ShoppingCart className="w-5 h-5 mr-1" />
              Cart
            </Link>
            <Link to="/orders" className="flex items-center hover:text-blue-100 transition">
              <Package className="w-5 h-5 mr-1" />
              Orders
            </Link>
          </nav>
        </div>
      </div>
    </header>
  );
}
