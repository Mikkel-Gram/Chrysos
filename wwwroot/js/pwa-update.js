// Service worker registration plus an "update available" banner.
//
// The default Blazor service worker installs a new version but leaves it in the
// "waiting" state until every window of the app is closed. An installed PWA on a
// phone is almost never closed, so a deploy could take days to reach the user.
// Here we actively poll for updates and let the user apply one with a tap.
(function () {
    if (!('serviceWorker' in navigator)) return;

    const UPDATE_CHECK_INTERVAL_MS = 15 * 60 * 1000;
    let reloading = false;

    navigator.serviceWorker.addEventListener('controllerchange', () => {
        if (reloading) return;
        reloading = true;
        window.location.reload();
    });

    function showBanner(worker) {
        if (document.getElementById('pwa-update-ui')) return;

        const bar = document.createElement('div');
        bar.id = 'pwa-update-ui';

        const label = document.createElement('span');
        label.textContent = '✨ New version available';

        const button = document.createElement('button');
        button.type = 'button';
        button.textContent = 'Update';
        button.addEventListener('click', () => {
            button.disabled = true;
            button.textContent = 'Updating…';
            worker.postMessage({ type: 'SKIP_WAITING' });
        });

        bar.appendChild(label);
        bar.appendChild(button);
        document.body.appendChild(bar);
    }

    function watch(registration) {
        // Only offer an update when a previous version is already in control,
        // otherwise this is the very first install.
        if (registration.waiting && navigator.serviceWorker.controller) {
            showBanner(registration.waiting);
        }

        registration.addEventListener('updatefound', () => {
            const installing = registration.installing;
            if (!installing) return;
            installing.addEventListener('statechange', () => {
                if (installing.state === 'installed' && navigator.serviceWorker.controller) {
                    showBanner(installing);
                }
            });
        });
    }

    navigator.serviceWorker
        .register(new URL('service-worker.js', document.baseURI), { updateViaCache: 'none' })
        .then(registration => {
            watch(registration);

            const check = () => registration.update().catch(() => { });
            setInterval(check, UPDATE_CHECK_INTERVAL_MS);
            // Resuming an installed PWA from the background does not reload the
            // page, so this is the check that usually finds a fresh deploy.
            document.addEventListener('visibilitychange', () => {
                if (document.visibilityState === 'visible') check();
            });
        })
        .catch(error => console.warn('Service worker registration failed', error));
})();
