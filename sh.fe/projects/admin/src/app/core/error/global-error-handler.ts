import { ErrorHandler, Injectable } from '@angular/core';

const CHUNK_RELOAD_KEY = 'chunk_reloaded';
const CHUNK_FAILED_MESSAGE = /Loading chunk [\d]+ failed|Failed to fetch dynamically imported module/;

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
    handleError(error: unknown): void {
        console.error(error);
        const isChunkError = error instanceof Error && CHUNK_FAILED_MESSAGE.test(error.message);
        if (isChunkError && !sessionStorage.getItem(CHUNK_RELOAD_KEY)) {
            sessionStorage.setItem(CHUNK_RELOAD_KEY, '1');
            window.location.reload();
        }
    }
}
