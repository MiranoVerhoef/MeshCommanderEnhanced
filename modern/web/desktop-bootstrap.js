(function () {
    'use strict';

    if (localStorage.getItem('mce:migration-v1') === 'complete') return;

    try {
        var request = new XMLHttpRequest();
        request.open('GET', '/api/desktop/bootstrap', false);
        request.send(null);
        if (request.status !== 200) return;

        var migration = JSON.parse(request.responseText);
        var values = migration.values || {};
        Object.keys(values).forEach(function (key) {
            if (localStorage.getItem(key) === null) localStorage.setItem(key, values[key]);
        });
        localStorage.setItem('mce:migration-v1', 'complete');
        if (migration.migrated) localStorage.setItem('mce:migration-source', migration.source || 'MeshCommander');
    } catch (error) {
        console.warn('Legacy MeshCommander configuration migration was skipped.', error);
    }
})();
