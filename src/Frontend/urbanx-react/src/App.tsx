import { lazy, Suspense } from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import Navbar from './components/layout/Navbar';
import Footer from './components/layout/Footer';
import { AuthProvider } from './context/AuthContext';
import { ErrorBoundary } from './components/ErrorBoundary';
import { Skeleton } from './components/ui/Skeleton';
import './index.css';

// Routes are lazy-loaded so the initial bundle ships only Home + nav + auth context.
// Each page becomes its own chunk; CheckoutPage in particular (~24 KB source) was
// dragging the main bundle past the Vite 500 KB warning threshold.
const HomePage             = lazy(() => import('./pages/HomePage'));
const CatalogPage          = lazy(() => import('./pages/CatalogPage'));
const ProductDetailsPage   = lazy(() => import('./pages/ProductDetailsPage'));
const CartPage             = lazy(() => import('./pages/CartPage'));
const CheckoutPage         = lazy(() => import('./pages/CheckoutPage'));
const OrdersPage           = lazy(() => import('./pages/OrdersPage'));
const OrderDetailsPage     = lazy(() => import('./pages/OrderDetailsPage'));
const LoginPage            = lazy(() => import('./pages/LoginPage'));
const SignUpPage           = lazy(() => import('./pages/SignUpPage'));
const ForgotPasswordPage   = lazy(() => import('./pages/ForgotPasswordPage'));
const ResetPasswordPage    = lazy(() => import('./pages/ResetPasswordPage'));
const ProfilePage          = lazy(() => import('./pages/ProfilePage'));
const AddressesPage        = lazy(() => import('./pages/AddressesPage'));
const AddressEditPage      = lazy(() => import('./pages/AddressEditPage'));
const SettingsPage         = lazy(() => import('./pages/SettingsPage'));
const OidcCallbackPage     = lazy(() => import('./pages/OidcCallbackPage'));

const RouteFallback = () => (
  <div className="container mx-auto px-6 py-20" aria-busy="true">
    <Skeleton className="h-8 w-48 mb-6" aria-label="Loading page" />
    <Skeleton className="h-4 w-3/4 mb-3" />
    <Skeleton className="h-4 w-1/2 mb-3" />
    <Skeleton className="h-4 w-2/3" />
  </div>
);

function App() {
  return (
    <AuthProvider>
      <Router>
        <div className="app-wrapper">
          <Navbar />
          <main className="main-content">
            <ErrorBoundary>
              <Suspense fallback={<RouteFallback />}>
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
              </Suspense>
            </ErrorBoundary>
          </main>
          <Footer />
        </div>
      </Router>
    </AuthProvider>
  );
}

export default App;
