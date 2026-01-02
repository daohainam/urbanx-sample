import { Link } from 'react-router-dom';

const Footer = () => {
    return (
        <footer className="bg-white border-t border-gray-100 pt-16 pb-8">
            <div className="container mx-auto px-6 grid grid-cols-1 md:grid-cols-4 gap-8 mb-16">
                <div className="md:col-span-1">
                    <h2 className="text-2xl font-serif font-bold text-gray-900 mb-4 tracking-tight">
                        URBAN<span className="text-secondary">X</span>
                    </h2>
                    <p className="text-gray-500 leading-relaxed text-sm">
                        A modern commerce platform for independent merchants and conscious customers.
                    </p>
                </div>

                <div className="flex flex-col gap-3">
                    <h3 className="font-serif font-semibold text-gray-900 mb-2">Shop</h3>
                    <Link to="/catalog" className="text-sm text-gray-500 hover:text-secondary transition-colors">All Products</Link>
                    <Link to="/catalog?category=electronics" className="text-sm text-gray-500 hover:text-secondary transition-colors">Electronics</Link>
                    <Link to="/catalog?category=fashion" className="text-sm text-gray-500 hover:text-secondary transition-colors">Fashion</Link>
                </div>

                <div className="flex flex-col gap-3">
                    <h3 className="font-serif font-semibold text-gray-900 mb-2">Help</h3>
                    <Link to="/orders" className="text-sm text-gray-500 hover:text-secondary transition-colors">Order Status</Link>
                    <Link to="/shipping" className="text-sm text-gray-500 hover:text-secondary transition-colors">Shipping Info</Link>
                    <Link to="/returns" className="text-sm text-gray-500 hover:text-secondary transition-colors">Returns</Link>
                </div>

                <div className="flex flex-col gap-3">
                    <h3 className="font-serif font-semibold text-gray-900 mb-2">Contact</h3>
                    <p className="text-sm text-gray-500">support@urbanx.com</p>
                    <div className="flex gap-4 mt-2">
                        {/* Social icons placeholders */}
                        <div className="w-8 h-8 rounded-full bg-gray-100 hover:bg-gray-200 transition-colors cursor-pointer"></div>
                        <div className="w-8 h-8 rounded-full bg-gray-100 hover:bg-gray-200 transition-colors cursor-pointer"></div>
                        <div className="w-8 h-8 rounded-full bg-gray-100 hover:bg-gray-200 transition-colors cursor-pointer"></div>
                    </div>
                </div>
            </div>

            <div className="container mx-auto px-6 border-t border-gray-100 pt-8 flex flex-col md:flex-row justify-between items-center text-xs text-gray-400">
                <p>&copy; {new Date().getFullYear()} UrbanX Commerce. All rights reserved.</p>
                <div className="flex gap-6 mt-4 md:mt-0">
                    <Link to="/privacy" className="hover:text-gray-600 transition-colors">Privacy Policy</Link>
                    <Link to="/terms" className="hover:text-gray-600 transition-colors">Terms of Service</Link>
                </div>
            </div>
        </footer>
    );
};

export default Footer;
