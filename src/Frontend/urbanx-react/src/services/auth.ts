import { UserManager, WebStorageStateStore } from 'oidc-client-ts';

export const userManager = new UserManager({
    authority: window.location.origin,
    client_id: 'urbanx-spa',
    redirect_uri: `${window.location.origin}/callback`,
    post_logout_redirect_uri: window.location.origin,
    scope: 'openid profile email catalog.read orders.read orders.write',
    userStore: new WebStorageStateStore({ store: localStorage }),
});
