import React from 'react';

interface EmptyStateProps {
    icon?: React.ReactNode;
    title: string;
    description?: string;
    action?: React.ReactNode;
}

/** Friendly "no results / no data" panel — same shape as ErrorState for visual consistency. */
export const EmptyState: React.FC<EmptyStateProps> = ({ icon, title, description, action }) => (
    <div className="text-center py-16 px-6 max-w-md mx-auto">
        {icon && <div className="flex justify-center mb-6 text-gray-300">{icon}</div>}
        <h2 className="text-2xl font-serif font-bold text-gray-900 mb-3">{title}</h2>
        {description && <p className="text-gray-500 mb-6">{description}</p>}
        {action}
    </div>
);
