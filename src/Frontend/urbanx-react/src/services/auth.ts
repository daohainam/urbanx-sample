import { UserManager, WebStorageStateStore } from 'oidc-client-ts';
import { env } from '../config/env';

export const userManager = new UserManager({
    authority: env.oidc.authority,
    client_id: env.oidc.clientId,
    redirect_uri: `${window.location.origin}/callback`,
    post_logout_redirect_uri: window.location.origin,
    scope: env.oidc.scopes,
    userStore: new WebStorageStateStore({ store: localStorage }),
});
