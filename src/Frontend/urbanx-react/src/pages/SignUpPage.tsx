import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { User, Mail, Lock, CheckCircle, ArrowRight } from 'lucide-react';
import { accountService } from '../services/api';
import { signUpSchema, type SignUpForm } from '../schemas/auth';
import { logger } from '../lib/logger';
import { TextField } from '../components/forms/TextField';

const SignUpPage = () => {
    const navigate = useNavigate();
    const [submitError, setSubmitError] = useState<string | null>(null);
    const {
        register,
        handleSubmit,
        formState: { errors, isSubmitting },
    } = useForm<SignUpForm>({
        resolver: zodResolver(signUpSchema),
        mode: 'onBlur',
        defaultValues: { name: '', email: '', password: '', confirmPassword: '', termsAccepted: false },
    });

    const onSubmit = async (values: SignUpForm) => {
        setSubmitError(null);
        try {
            await accountService.register(values.email, values.password, values.name);
            navigate('/login');
        } catch (err) {
            const message = err instanceof Error ? err.message : 'Registration failed. Please try again.';
            logger.error('Sign-up failed', err);
            setSubmitError(message);
        }
    };

    return (
        <div className="min-h-[80vh] flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
            <div className="max-w-md w-full space-y-8 bg-white p-10 rounded-xl shadow-lg border border-gray-100">
                <div className="text-center">
                    <h2 className="mt-2 text-3xl font-serif font-bold text-gray-900">
                        Create Account
                    </h2>
                    <p className="mt-2 text-sm text-gray-600">
                        Join UrbanX for an exclusive shopping experience
                    </p>
                </div>

                <form className="mt-8 space-y-6" onSubmit={handleSubmit(onSubmit)} noValidate>
                    <div className="space-y-4">
                        <TextField
                            visuallyHiddenLabel
                            id="name"
                            label="Full name"
                            placeholder="Full Name"
                            type="text"
                            autoComplete="name"
                            icon={<User size={20} />}
                            error={errors.name?.message}
                            {...register('name')}
                        />
                        <TextField
                            visuallyHiddenLabel
                            id="email"
                            label="Email"
                            placeholder="Email Address"
                            type="email"
                            autoComplete="email"
                            icon={<Mail size={20} />}
                            error={errors.email?.message}
                            {...register('email')}
                        />
                        <TextField
                            visuallyHiddenLabel
                            id="password"
                            label="Password"
                            placeholder="Password"
                            type="password"
                            autoComplete="new-password"
                            icon={<Lock size={20} />}
                            error={errors.password?.message}
                            {...register('password')}
                        />
                        <TextField
                            visuallyHiddenLabel
                            id="confirmPassword"
                            label="Confirm password"
                            placeholder="Confirm Password"
                            type="password"
                            autoComplete="new-password"
                            icon={<CheckCircle size={20} />}
                            error={errors.confirmPassword?.message}
                            {...register('confirmPassword')}
                        />
                    </div>

                    <div>
                        <label htmlFor="terms" className="flex items-center cursor-pointer">
                            <input
                                id="terms"
                                type="checkbox"
                                aria-invalid={Boolean(errors.termsAccepted)}
                                aria-describedby={errors.termsAccepted ? 'terms-error' : undefined}
                                className="h-4 w-4 text-primary focus:ring-primary border-gray-300 rounded accent-primary cursor-pointer"
                                {...register('termsAccepted')}
                            />
                            <span className="ml-2 block text-sm text-gray-900">
                                I agree to the <a href="#" className="text-secondary hover:text-primary">Terms of Service</a> and <a href="#" className="text-secondary hover:text-primary">Privacy Policy</a>
                            </span>
                        </label>
                        {errors.termsAccepted && (
                            <p id="terms-error" className="mt-1.5 text-sm text-red-600">
                                {errors.termsAccepted.message}
                            </p>
                        )}
                    </div>

                    {submitError && (
                        <div role="alert" className="text-sm text-red-600 bg-red-50 px-4 py-3 rounded-md border border-red-100">
                            {submitError}
                        </div>
                    )}

                    <div>
                        <button
                            type="submit"
                            disabled={isSubmitting}
                            className={`group relative w-full flex justify-center py-3 px-4 border border-transparent text-sm font-medium rounded-md text-white btn-action-black hover:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary transition-colors ${
                                isSubmitting ? 'opacity-70 cursor-not-allowed' : ''
                            }`}
                        >
                            <span className="absolute left-0 inset-y-0 flex items-center pl-3">
                                <ArrowRight size={20} className="text-gray-300 group-hover:text-white" />
                            </span>
                            {isSubmitting ? 'Creating Account...' : 'Create Account'}
                        </button>
                    </div>
                </form>

                <div className="text-center mt-6">
                    <p className="text-sm text-gray-600">
                        Already have an account?{' '}
                        <Link to="/login" className="font-medium text-secondary hover:text-primary transition-colors">
                            Sign in
                        </Link>
                    </p>
                </div>
            </div>
        </div>
    );
};

export default SignUpPage;
