import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import Header from './components/Header';
import CatalogPage from './pages/CatalogPage';
import CartPage from './pages/CartPage';
import CheckoutPage from './pages/CheckoutPage';
import TrackingPage from './pages/TrackingPage';
import OrdersPage from './pages/OrdersPage';

function App() {
  return (
  <div className="min-h-dvh flex flex-col">
    <Router>
        <Header />
      <div className="flex-1">
        <Routes>
          <Route path="/" element={<CatalogPage />} />
          <Route path="/cart" element={<CartPage />} />
          <Route path="/checkout" element={<CheckoutPage />} />
          <Route path="/tracking/:orderId" element={<TrackingPage />} />
          <Route path="/orders" element={<OrdersPage />} />
        </Routes>
      </div>
    </Router>
	<footer className="border-t border-neutral-200 bg-white/80 backdrop-blur-sm text-xs text-neutral-500 py-6 text-center">
      <div className="container mx-auto px-4">
        <p>© {new Date().getFullYear()} <span className="font-semibold text-primary-600">UrbanX</span> - Elegant Commerce Platform</p>
      </div>
    </footer>
    </div>
  );
}

export default App;
