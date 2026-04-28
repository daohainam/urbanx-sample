import React from 'react';

type SkeletonProps = {
    className?: string;
    'aria-label'?: string;
};

/**
 * Visual placeholder that animates while data is loading. Marked with role="status"
 * so screen readers announce loading; pair it with a meaningful aria-label.
 */
export const Skeleton: React.FC<SkeletonProps> = ({ className = '', 'aria-label': ariaLabel = 'Loading' }) => (
    <div
        role="status"
        aria-label={ariaLabel}
        aria-busy="true"
        className={`animate-pulse bg-gray-200 rounded ${className}`}
    />
);

/** Card-shaped placeholder for product grids. */
export const ProductCardSkeleton: React.FC = () => (
    <div className="bg-white rounded-lg overflow-hidden border border-gray-100">
        <Skeleton className="aspect-square w-full" aria-label="Loading product image" />
        <div className="p-4 space-y-2">
            <Skeleton className="h-4 w-3/4" />
            <Skeleton className="h-4 w-1/3" />
        </div>
    </div>
);

/** Row-shaped placeholder for lists (orders, addresses). */
export const ListRowSkeleton: React.FC = () => (
    <div className="bg-white border border-gray-100 rounded-lg p-6 space-y-3">
        <div className="flex justify-between">
            <Skeleton className="h-5 w-1/3" />
            <Skeleton className="h-5 w-24" />
        </div>
        <Skeleton className="h-4 w-2/3" />
        <Skeleton className="h-4 w-1/2" />
    </div>
);
