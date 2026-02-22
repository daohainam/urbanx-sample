import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Search, User, ShoppingBag, Menu, X, LogIn, LogOut } from 'lucide-react';
import { useCart } from '../../context/useCart';
import { useAuth } from '../../context/useAuth';

const Navbar = () => {
    const { totalItems } = useCart();
    const { user, login, logout } = useAuth();
    const [isMenuOpen, setIsMenuOpen] = useState(false);

    return (
        <nav className="h-20 bg-white border-b border-gray-100 sticky top-0 z-50">
            <div className="container mx-auto px-6 h-full flex justify-between items-center">
                {/* Left: Mobile Menu & Logo */}
                <div className="flex items-center gap-4">
                    <button
                        className="lg:hidden p-2 -ml-2 text-gray-600 hover:bg-gray-100 rounded-full"
                        onClick={() => setIsMenuOpen(!isMenuOpen)}
                    >
                        {isMenuOpen ? <X size={24} /> : <Menu size={24} />}
                    </button>
                    <Link to="/" className="text-2xl font-serif font-bold tracking-widest text-primary">
                        URBAN<span className="text-secondary">X</span>
                    </Link>
                </div>

                {/* Center: Desktop Navigation */}
                <div className="hidden lg:flex items-center gap-8">
                    <Link to="/catalog" className="text-sm font-medium uppercase tracking-wider text-gray-600 hover:text-black hover:underline underline-offset-4 transition-all">Shop All</Link>
                    <Link to="/catalog?category=electronics" className="text-sm font-medium uppercase tracking-wider text-gray-600 hover:text-black hover:underline underline-offset-4 transition-all">Electronics</Link>
                    <Link to="/catalog?category=fashion" className="text-sm font-medium uppercase tracking-wider text-gray-600 hover:text-black hover:underline underline-offset-4 transition-all">Fashion</Link>
                    <Link to="/catalog?category=home" className="text-sm font-medium uppercase tracking-wider text-gray-600 hover:text-black hover:underline underline-offset-4 transition-all">Home</Link>
                </div>

                {/* Right: Actions */}
                <div className="flex items-center gap-2">
                    <div className="hidden lg:flex items-center bg-gray-100 rounded-full px-4 py-2 mr-2">
                        <Search size={18} className="text-gray-400" />
                        <input
                            type="text"
                            placeholder="Search..."
                            className="bg-transparent border-none outline-none text-sm ml-2 w-24 focus:w-40 transition-all placeholder:text-gray-400"
                        />
                    </div>

                    {user ? (
                        <>
                            <Link to="/profile" className="p-2 text-primary hover:bg-gray-100 rounded-full transition-colors" title={user.profile.name ?? user.profile.email}>
                                <User size={20} />
                            </Link>
                            <button
                                onClick={logout}
                                className="p-2 text-gray-500 hover:bg-gray-100 rounded-full transition-colors"
                                title="Sign out"
                            >
                                <LogOut size={20} />
                            </button>
                        </>
                    ) : (
                        <button
                            onClick={login}
                            className="p-2 text-primary hover:bg-gray-100 rounded-full transition-colors"
                            title="Sign in"
                        >
                            <LogIn size={20} />
                        </button>
                    )}

                    <Link to="/cart" className="p-2 text-primary hover:bg-gray-100 rounded-full transition-colors relative">
                        <ShoppingBag size={20} />
                        {totalItems > 0 && (
                            <span className="absolute -top-1 -right-1 bg-secondary text-primary text-[10px] font-bold h-4 w-4 rounded-full flex items-center justify-center border border-white">
                                {totalItems}
                            </span>
                        )}
                    </Link>
                </div>
            </div>

            {/* Mobile Menu Dropdown */}
            {isMenuOpen && (
                <div className="lg:hidden absolute top-20 left-0 w-full bg-white border-b border-gray-100 shadow-lg py-4 px-6 flex flex-col gap-4 animate-fade-in">
                    <div className="flex items-center bg-gray-100 rounded-lg px-4 py-3">
                        <Search size={18} className="text-gray-400" />
                        <input
                            type="text"
                            placeholder="Search products..."
                            className="bg-transparent border-none outline-none text-sm ml-2 w-full"
                        />
                    </div>
                    <Link to="/catalog" className="text-sm font-medium uppercase tracking-wider py-2 border-b border-gray-50" onClick={() => setIsMenuOpen(false)}>Shop All</Link>
                    <Link to="/catalog?category=electronics" className="text-sm font-medium uppercase tracking-wider py-2 border-b border-gray-50" onClick={() => setIsMenuOpen(false)}>Electronics</Link>
                    <Link to="/catalog?category=fashion" className="text-sm font-medium uppercase tracking-wider py-2 border-b border-gray-50" onClick={() => setIsMenuOpen(false)}>Fashion</Link>
                    <Link to="/catalog?category=home" className="text-sm font-medium uppercase tracking-wider py-2" onClick={() => setIsMenuOpen(false)}>Home & Living</Link>
                </div>
            )}
        </nav>
    );
};

export default Navbar;
