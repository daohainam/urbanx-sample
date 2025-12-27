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
	<footer className="border-t border-slate-200 text-xs text-slate-500 py-4 text-center">© {new Date().getFullYear()} UrbanX</footer>
    </div>
  );
}

export default App;
