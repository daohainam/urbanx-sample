import type { AuthProviderProps } from 'react-oidc-context';

const IDENTITY_URL = import.meta.env.VITE_IDENTITY_URL || 'http://localhost:5005';

export const oidcConfig: AuthProviderProps = {
  authority: IDENTITY_URL,
  client_id: 'urbanx-merchant-spa',
  redirect_uri: window.location.origin + '/callback',
  post_logout_redirect_uri: window.location.origin,
  scope: 'openid profile email merchant.manage',
  response_type: 'code',
  automaticSilentRenew: true,
  loadUserInfo: true,
};
