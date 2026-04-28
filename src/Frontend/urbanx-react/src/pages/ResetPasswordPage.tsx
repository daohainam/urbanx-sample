import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Key, Lock, CheckCircle } from 'lucide-react';
import { logger } from '../lib/logger';
import { resetPasswordSchema, type ResetPasswordForm } from '../schemas/auth';
import { TextField } from '../components/forms/TextField';

const ResetPasswordPage = () => {
    const navigate = useNavigate();
    const {
        register,
        handleSubmit,
        formState: { errors, isSubmitting },
    } = useForm<ResetPasswordForm>({
        resolver: zodResolver(resetPasswordSchema),
        mode: 'onBlur',
        defaultValues: { code: '', newPassword: '', confirmPassword: '' },
    });

    const onSubmit = async (values: ResetPasswordForm) => {
        // Simulate API call (real implementation lands with the BFF migration).
        await new Promise((r) => setTimeout(r, 1000));
        logger.info('Password reset (stub)', { code: values.code });
        navigate('/login');
    };

    return (
        <div className="min-h-[80vh] flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
            <div className="max-w-md w-full space-y-8 bg-white p-10 rounded-xl shadow-lg border border-gray-100">
                <div>
                    <h2 className="mt-2 text-3xl font-serif font-bold text-center text-gray-900">
                        Reset Password
                    </h2>
                    <p className="mt-2 text-center text-sm text-gray-600">
                        Enter the code sent to your email and your new password.
                    </p>
                </div>

                <form className="mt-8 space-y-6" onSubmit={handleSubmit(onSubmit)} noValidate>
                    <div className="space-y-4">
                        <TextField
                            visuallyHiddenLabel
                            id="code"
                            label="Verification code"
                            placeholder="Verification Code"
                            type="text"
                            autoComplete="one-time-code"
                            icon={<Key size={20} />}
                            error={errors.code?.message}
                            {...register('code')}
                        />
                        <TextField
                            visuallyHiddenLabel
                            id="newPassword"
                            label="New password"
                            placeholder="New Password"
                            type="password"
                            autoComplete="new-password"
                            icon={<Lock size={20} />}
                            error={errors.newPassword?.message}
                            {...register('newPassword')}
                        />
                        <TextField
                            visuallyHiddenLabel
                            id="confirmPassword"
                            label="Confirm new password"
                            placeholder="Confirm New Password"
                            type="password"
                            autoComplete="new-password"
                            icon={<CheckCircle size={20} />}
                            error={errors.confirmPassword?.message}
                            {...register('confirmPassword')}
                        />
                    </div>

                    <div>
                        <button
                            type="submit"
                            disabled={isSubmitting}
                            className={`group relative w-full flex justify-center py-3 px-4 border border-transparent text-sm font-medium rounded-md text-white btn-action-black hover:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary transition-colors ${
                                isSubmitting ? 'opacity-70 cursor-not-allowed' : ''
                            }`}
                        >
                            {isSubmitting ? 'Resetting...' : 'Reset Password'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
};

export default ResetPasswordPage;
