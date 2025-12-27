# UrbanX Merchant Portal

A modern merchant management portal built with React, TypeScript, Vite, and TailwindCSS.

## Features

- **Dashboard**: Overview of merchant statistics and quick actions
- **Categories Management**: Create, edit, and delete product categories
- **Products Management**: Manage product inventory with pricing and stock
- **Orders Management**: View and manage customer orders
- **OAuth2 Authentication**: Secure authentication using OIDC/OAuth2

## Tech Stack

- **React 19** - UI library
- **TypeScript** - Type-safe development
- **Vite** - Fast build tool and dev server
- **TailwindCSS 4** - Utility-first CSS framework
- **React Router** - Client-side routing
- **OIDC Client** - OAuth2/OIDC authentication
- **Lucide React** - Icon library

## Getting Started

### Prerequisites

- Node.js 18+ 
- npm or yarn
- Backend services running (see main repository README)

### Installation

1. Copy the environment variables:
```bash
cp .env.example .env
```

2. Update the `.env` file with your configuration:
```env
VITE_API_BASE_URL=http://localhost:5000
VITE_IDENTITY_URL=http://localhost:5005
```

3. Install dependencies:
```bash
npm install
```

4. Start the development server:
```bash
npm run dev
```

The merchant portal will be available at `http://localhost:5174`

### Build for Production

```bash
npm run build
```

The production build will be in the `dist` directory.

### Preview Production Build

```bash
npm run preview
```

## Authentication

The merchant portal uses OAuth2/OIDC for authentication with the following configuration:

- **Client ID**: `urbanx-merchant-spa`
- **Scopes**: `openid profile email merchant.manage`
- **Grant Type**: Authorization Code with PKCE
- **Redirect URI**: `http://localhost:5174/callback`

### Test Credentials

Username: `merchant@test.com`
Password: `Password123!`

## Project Structure

```
merchant-app/
├── src/
│   ├── components/       # Reusable UI components
│   │   └── Navigation.tsx
│   ├── layouts/          # Layout components
│   │   └── Layout.tsx
│   ├── lib/              # Utilities and configurations
│   │   ├── api.ts        # API client
│   │   └── auth-config.ts
│   ├── pages/            # Page components
│   │   ├── CallbackPage.tsx
│   │   ├── CategoriesPage.tsx
│   │   ├── DashboardPage.tsx
│   │   ├── OrdersPage.tsx
│   │   └── ProductsPage.tsx
│   ├── types/            # TypeScript type definitions
│   │   └── index.ts
│   ├── App.tsx           # Main app component
│   ├── main.tsx          # Entry point
│   └── index.css         # Global styles
├── public/               # Static assets
├── .env.example          # Environment variables template
├── package.json
├── tailwind.config.js    # TailwindCSS configuration
├── tsconfig.json         # TypeScript configuration
└── vite.config.ts        # Vite configuration
```

## API Integration

The merchant portal integrates with the following backend endpoints:

### Categories
- `GET /api/merchants/{merchantId}/categories` - List all categories
- `POST /api/merchants/{merchantId}/categories` - Create category
- `PUT /api/merchants/{merchantId}/categories/{id}` - Update category
- `DELETE /api/merchants/{merchantId}/categories/{id}` - Delete category

### Products
- `GET /api/merchants/{merchantId}/products` - List all products
- `POST /api/merchants/{merchantId}/products` - Create product
- `PUT /api/merchants/{merchantId}/products/{id}` - Update product
- `DELETE /api/merchants/{merchantId}/products/{id}` - Delete product

### Orders
- `GET /api/merchants/{merchantId}/orders` - List merchant orders
- `GET /api/orders/{orderId}` - Get order details
- `PUT /api/orders/{orderId}/status` - Update order status

## Development

### Available Scripts

- `npm run dev` - Start development server
- `npm run build` - Build for production
- `npm run preview` - Preview production build
- `npm run lint` - Run ESLint

### Code Style

The project uses ESLint with React and TypeScript plugins. Run `npm run lint` to check for issues.

## License

MIT
