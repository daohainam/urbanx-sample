import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import Header from './components/Header';
import CatalogPage from './pages/CatalogPage';
import CartPage from './pages/CartPage';
import CheckoutPage from './pages/CheckoutPage';
import TrackingPage from './pages/TrackingPage';
import OrdersPage from './pages/OrdersPage';

function App() {
  return (
    <Router>
      <div className="min-h-screen bg-gray-50">
        <Header />
        <Routes>
          <Route path="/" element={<CatalogPage />} />
          <Route path="/cart" element={<CartPage />} />
          <Route path="/checkout" element={<CheckoutPage />} />
          <Route path="/tracking/:orderId" element={<TrackingPage />} />
          <Route path="/orders" element={<OrdersPage />} />
        </Routes>
      </div>
    </Router>
  );
}

export default App;
