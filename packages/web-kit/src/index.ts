export { ACCESS_COOKIE, REFRESH_COOKIE, clearAuthCookies, setAuthCookies } from './cookies';
export type { TokenPair } from './cookies';
export { authBaseUrl, candidates } from './backends';
export { isExpired, verifyAccessToken } from './session';
export type { VerifiedSession } from './session';
export {
  handleConsentVersions,
  handleLogin,
  handleLogout,
  handleRegister,
  handleSession,
} from './auth-routes';
export { handleProxy } from './proxy';
export { handleConfig } from './config-route';
export { createAuthMiddleware } from './middleware';
export type { AuthMiddlewareOptions } from './middleware';
