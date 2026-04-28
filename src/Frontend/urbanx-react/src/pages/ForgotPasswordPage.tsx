import { useNavigate, Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Mail, ArrowLeft } from 'lucide-react';
import { logger } from '../lib/logger';
import { forgotPasswordSchema, type ForgotPasswordForm } from '../schemas/auth';
import { TextField } from '../components/forms/TextField';

const ForgotPasswordPage = () => {
    const navigate = useNavigate();
    const {
        register,
        handleSubmit,
        formState: { errors, isSubmitting },
    } = useForm<ForgotPasswordForm>({
        resolver: zodResolver(forgotPasswordSchema),
        mode: 'onBlur',
        defaultValues: { email: '' },
    });

    const onSubmit = async (values: ForgotPasswordForm) => {
        // Simulate API call (real implementation lands with the BFF migration).
        await new Promise((r) => setTimeout(r, 1000));
        logger.info('Reset code sent (stub)', { email: values.email });
        navigate('/reset-password');
    };

    return (
        <div className="min-h-[80vh] flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
            <title>Reset password — UrbanX</title>
            <div className="max-w-md w-full space-y-8 bg-white p-10 rounded-xl shadow-lg border border-gray-100">
                <div>
                    <h2 className="mt-2 text-3xl font-serif font-bold text-center text-gray-900">
                        Forgot Password
                    </h2>
                    <p className="mt-2 text-center text-sm text-gray-600">
                        Enter your email address and we'll send you a code to reset your password.
                    </p>
                </div>

                <form className="mt-8 space-y-6" onSubmit={handleSubmit(onSubmit)} noValidate>
                    <TextField
                        id="email"
                        label="Email address"
                        visuallyHiddenLabel
                        placeholder="Email Address"
                        type="email"
                        autoComplete="email"
                        icon={<Mail size={20} />}
                        error={errors.email?.message}
                        {...register('email')}
                    />

                    <div>
                        <button
                            type="submit"
                            disabled={isSubmitting}
                            className={`group relative w-full flex justify-center py-3 px-4 border border-transparent text-sm font-medium rounded-md text-white btn-action-black hover:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary transition-colors ${
                                isSubmitting ? 'opacity-70 cursor-not-allowed' : ''
                            }`}
                        >
                            {isSubmitting ? 'Sending...' : 'Send Reset Code'}
                        </button>
                    </div>
                </form>

                <div className="text-center mt-4">
                    <Link to="/login" className="font-medium text-sm text-gray-600 hover:text-primary flex items-center justify-center gap-2 transition-colors">
                        <ArrowLeft size={16} /> Back to Login
                    </Link>
                </div>
            </div>
        </div>
    );
};

export default ForgotPasswordPage;
