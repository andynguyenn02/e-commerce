# ecommerce.Web

The React SPA for the wallet-paid shop: customer storefront (products, cart, checkout, orders,
wallet) plus the admin screens (product CRUD, inventory upload). Talks to `ecommerce.Api` over
axios. See the [repo README](../README.md) for the backend and the full docker stack.

**Stack:** React 19 · TypeScript · Vite · Redux Toolkit · React Router · Tailwind v4 · Radix Dialog ·
sonner · oxlint

## Running it

The API must be up (`dotnet run --project ecommerce.Api`, or the full docker stack) on
`http://localhost:5118`.

```bash
npm install
npm run dev
```

Dev server is pinned to **port 5173, strict** — the API's CORS policy allows only that exact origin,
so a fallback port would break every request.

| Script | Does |
|---|---|
| `npm run dev` | Vite dev server with HMR on :5173 |
| `npm run build` | `tsc -b` then a production bundle into `dist/` |
| `npm run preview` | Serve the built bundle |
| `npm run lint` | oxlint |

`VITE_API_URL` is the API base URL. Vite inlines it at build time, so it is read from
`.env.development` for `npm run dev` and passed as the `VITE_API_URL` build arg to the Docker image
(`Dockerfile` → nginx serving `dist/`, with `try_files` so client-side routes resolve).

## Layout

```
src/
  api/          one module per backend area + the shared axios client
  store/        Redux slices: auth, products, cart, orders
  pages/        one component per route (pages/admin/ for the admin screens)
  components/   shared pieces; components/ui/ primitives, components/admin/ admin-only
  hooks/        useAsync, useProductList, useInventoryJobs, useDebouncedValue
  routes/       router.tsx and the RequireRole guard
  session.ts    startup: restore session, wire the 401 handler
```

## How it hangs together

**Auth is a cookie, not a token.** The backend sets an httpOnly cookie; `withCredentials: true` on
the axios instance makes the browser attach it. No component ever sees a token, and nothing is kept
in `localStorage`. On reload `initSession()` dispatches `restoreSession()`, which asks `GET /me` who
we are — that's why `auth.initialized` exists, and why the route guards wait on it instead of
bouncing a logged-in user to `/login`.

**One 401 handler.** `api/client.ts` takes a callback rather than importing the store (circular
import), and `session.ts` registers it: a 401 while `auth.user` is set means the session expired →
clear it, toast, redirect to `/login`. A 401 for a guest or a bad password is ignored there and
handled by the caller.

**One error shape.** The API returns `{ error, code }`; `getErrorMessage(err)` unwraps that and
covers the cases that never reach the handler (network down, a 413 from Kestrel on an oversized
upload). Use it everywhere instead of reading `err.message`.

**Four-state async, not a boolean.** `AsyncStatus` is `'idle' | 'loading' | 'success' | 'error'`, so
"never fetched" and "fetched, found nothing" render differently. Slices use it via `AsyncState`;
`useAsync` gives page-local data the same contract.

**Routes** (`routes/router.tsx`, guarded by `RequireRole`):

| Path | Access |
|---|---|
| `/login` | public |
| `/products` | public |
| `/cart` · `/orders` · `/orders/:id` · `/wallet` | Customer |
| `/admin/products` · `/admin/inventory` | Admin |

Inventory uploads return a job id; `useInventoryJobs` polls `GET /api/inventory/jobs` for status.

## Theme

Tokens live in `src/index.css` under `@theme` — a catalogue look: rules instead of shadows (there
are none anywhere), Fraunces for display type and prices, Inter for the interface, mono reserved for
real data (product codes, job ids, order ids). Take colours, fonts and radii from those variables
rather than hardcoding hex or arbitrary values.
