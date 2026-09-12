// Per-user storage for VetoGantt edits.
//
// Tries PGlite - Postgres compiled to WebAssembly, held in IndexedDB - and keeps localStorage
// underneath it. The fallback is not a lesser option here: what is stored is a handful of date
// overrides, and PGlite is several megabytes. PGlite is offered because an app that already
// uses it gets one storage engine instead of two, and because a real table is worth having once
// edits grow beyond a flat list.
//
// Two rules keep this honest, and both exist because PGlite is fetched over the network:
//
//  1. Every read and write awaits the same opening promise, so a save that happens while PGlite
//     is still arriving cannot land in one store while the next read comes from the other.
//  2. localStorage is written on every save whatever the backend, as a mirror. Whether PGlite
//     loads at all depends on the network, so it may be there on one visit and not the next;
//     without the mirror a slow CDN would look to the reader like their edits had vanished.
//
// Either way this only ever holds one person's local edits, in their own browser.

let database = null;
let backend = 'memory';

// Whether the mirror can be kept in localStorage. Separate from the backend name, because the
// mirror has to outlive a reload whether or not PGlite is the one holding the rows.
let persistable = false;
let ready = null;
const memory = new Map();

/**
 * Opens the store. Safe to call repeatedly; only the first call does any work.
 * @param {string} url module URL for PGlite, or null to skip straight to localStorage
 * @returns {Promise<string>} which backend is in use: 'pglite', 'localstorage' or 'memory'
 */
export function open(url) {
    ready ??= start(url);
    return ready;
}

async function start(url) {
    // A working backend is chosen first and PGlite is an upgrade on top of it. Waiting on a
    // network import before deciding would mean that a slow or blocked CDN leaves edits with
    // nowhere to go - saved into memory and lost on the next reload.
    persistable = canUseLocalStorage();
    backend = persistable ? 'localstorage' : 'memory';

    if (!url) {
        return backend;
    }

    try {
        // Bounded, because a hanging request should not hold up storage indefinitely.
        const module = await Promise.race([
            import(/* webpackIgnore: true */ url),
            new Promise((_, reject) => setTimeout(() => reject(new Error('timed out')), 4000))
        ]);

        const candidate = new module.PGlite('idb://veto-gantt');

        await candidate.exec(`
            CREATE TABLE IF NOT EXISTS gantt_overrides (
                chart TEXT NOT NULL,
                item  TEXT NOT NULL,
                start DATE NOT NULL,
                finish DATE NOT NULL,
                PRIMARY KEY (chart, item)
            );
        `);

        database = candidate;
        backend = 'pglite';
    } catch {
        // No network, a blocked CDN, or an unsupported browser. Nothing is broken: what is
        // stored is a handful of dates and localStorage holds them perfectly well.
        database = null;
    }

    return backend;
}

/** Returns the saved overrides for a chart, as an array of { item, start, finish }. */
export async function load(chart) {
    await ready;

    const local = mirrored(chart);

    // The mirror is written on every save and the table only when PGlite is loaded, so where
    // the mirror has anything to say - including that the reader cleared the chart - it is the
    // newer of the two. The table is read only when the mirror has never been written, or has
    // been wiped along with the rest of the site's storage; then it is what makes PGlite worth
    // having, because it survives that.
    if (local !== null) {
        return local;
    }

    if (!database) {
        return [];
    }

    const result = await database.query(
        'SELECT item, start, finish FROM gantt_overrides WHERE chart = $1',
        [chart]);

    const rows = result.rows.map(row => ({
        item: row.item,
        start: asDate(row.start),
        finish: asDate(row.finish)
    }));

    mirror(chart, rows);

    return rows;
}

/** Saves one override, replacing any previous one for that item. */
export async function save(chart, item, start, finish) {
    const rows = (await load(chart)).filter(row => row.item !== item);
    rows.push({ item, start, finish });

    mirror(chart, rows);

    if (database) {
        await database.query(
            `INSERT INTO gantt_overrides (chart, item, start, finish)
             VALUES ($1, $2, $3, $4)
             ON CONFLICT (chart, item) DO UPDATE SET start = $3, finish = $4`,
            [chart, item, start, finish]);
    }
}

/** Removes everything saved for a chart, from both the table and the mirror. */
export async function clear(chart) {
    await ready;

    // Recorded as an empty list rather than by removing the key: an absent mirror means nobody
    // has saved anything here, which is the one case where the table is allowed to speak. If
    // clearing removed the key, a reset made while PGlite happened not to load would be undone
    // by the table on the next visit.
    mirror(chart, []);

    if (database) {
        await database.query('DELETE FROM gantt_overrides WHERE chart = $1', [chart]);
    }
}

/** The mirrored rows for a chart, or null when nothing has ever been mirrored for it. */
function mirrored(chart) {
    if (!persistable) {
        return memory.get(chart) ?? null;
    }

    try {
        const stored = localStorage.getItem(key(chart));
        return stored === null ? null : JSON.parse(stored);
    } catch {
        return null;
    }
}

function mirror(chart, rows) {
    memory.set(chart, rows);

    if (!persistable) {
        return;
    }

    try {
        localStorage.setItem(key(chart), JSON.stringify(rows));
    } catch {
        // Private window or blocked storage; the in-memory copy still carries the session.
    }
}

function key(chart) {
    return `veto-gantt:${chart}`;
}

function asDate(value) {
    // PGlite hands back a Date for a DATE column; the rest of the component speaks yyyy-MM-dd.
    return value instanceof Date ? value.toISOString().slice(0, 10) : String(value);
}

function canUseLocalStorage() {
    try {
        const probe = '__veto__';
        localStorage.setItem(probe, probe);
        localStorage.removeItem(probe);
        return true;
    } catch {
        return false;
    }
}
