/** @type {import('next').NextConfig} */
const nextConfig = {
  // One image promotable across environments: the runtime stage runs the
  // standalone server and reads addresses from env at request time (P12).
  output: 'standalone',
  transpilePackages: ['@auto-veritas/web-kit'],
};

export default nextConfig;
