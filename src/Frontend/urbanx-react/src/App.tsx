import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import Navbar from './components/layout/Navbar';
import Footer from './components/layout/Footer';
import {
  CatalogPage,
  ProductDetailsPage,
  CartPage,
  CheckoutPage,
  OrdersPage,
  OrderDetailsPage,
  LoginPage,
  SignUpPage,
  ForgotPasswordPage,
  ResetPasswordPage,
  ProfilePage,
  AddressesPage,
  AddressEditPage,
  SettingsPage,
  OidcCallbackPage,
} from './pages/index.tsx';
import HomePage from './pages/HomePage';
import { AuthProvider } from './context/AuthContext';
import { ErrorBoundary } from './components/ErrorBoundary';
import './index.css';

function App() {
  return (
    <AuthProvider>
      <Router>
        <div className="app-wrapper">
          <Navbar />
          <main className="main-content">
            <ErrorBoundary>
              <Routes>
                <Route path="/" element={<HomePage />} />
                <Route path="/catalog" element={<CatalogPage />} />
                <Route path="/product/:id" element={<ProductDetailsPage />} />
                <Route path="/cart" element={<CartPage />} />
                <Route path="/checkout" element={<CheckoutPage />} />
                <Route path="/orders" element={<OrdersPage />} />
                <Route path="/orders/:id" element={<OrderDetailsPage />} />
                <Route path="/login" element={<LoginPage />} />
                <Route path="/signup" element={<SignUpPage />} />
                <Route path="/forgot-password" element={<ForgotPasswordPage />} />
                <Route path="/reset-password" element={<ResetPasswordPage />} />
                <Route path="/profile" element={<ProfilePage />} />
                <Route path="/profile/addresses" element={<AddressesPage />} />
                <Route path="/profile/addresses/new" element={<AddressEditPage />} />
                <Route path="/profile/addresses/:id" element={<AddressEditPage />} />
                <Route path="/profile/settings" element={<SettingsPage />} />
                <Route path="/callback" element={<OidcCallbackPage />} />
              </Routes>
            </ErrorBoundary>
          </main>
          <Footer />
        </div>
      </Router>
    </AuthProvider>
  );
}

export default App;
