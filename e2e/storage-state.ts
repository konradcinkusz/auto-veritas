import path from 'node:path';

/** Where the core tier's shared viewer session lives. Gitignored. */
export const STORAGE_STATE = path.join(__dirname, '.auth', 'viewer.json');
