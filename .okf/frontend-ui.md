---
type: Reference
title: Frontend UI
description: Look-and-feel target for the HelpDesk SvelteKit UI, aligned with the FastEndpoints docs site theme.
tags: [frontend, ui, theme]
resource: frontend/src/app.css
---

# Frontend UI

## Intent

HelpDesk frontend should look and feel like the FastEndpoints public docs site: dark-first, cyan-on-navy developer UI with restrained chrome, soft surfaces, and high-contrast accent actions.

Reference implementation (sibling, not a monorepo package):

| Item | Location |
| --- | --- |
| Live site | https://fast-endpoints.com |
| Docs app | `../FE-Docs/` |
| Theme tokens | `../FE-Docs/src/vars.css` |
| Global styles / shell CSS | `../FE-Docs/src/app.css` |
| Tailwind palettes | `../FE-Docs/tailwind.config.cjs` (`feBlue`, `feDarkBlue`, `feLightBlue`, gray semantic tokens) |
| Fonts | `../FE-Docs/src/lib/styles/fonts.css` (Inter + Fira Code) |
| Shell / chrome | `../FE-Docs/src/lib/components/site/*` |

Do not import FE-Docs as a dependency. Re-express the same visual language inside `frontend/` with Tailwind v4 and local CSS variables. Match the *theme*, not the docs-site layout/nav product surface.

## Mode and foundation

- **Dark-first.** Default to dark; light mode is not required unless product work explicitly adds it.
- **Body:** near-black navy `rgba(9, 14, 24, 1)` / `#090e18` (`feDarkBlue-800`).
- **Primary text:** cool gray `#d1d5db`-`#d5d5d5`; muted/secondary around `#9ca3af` / soft gray `#aaa`.
- **Headings:** light gray (`#d5d5d5`), medium-light weight, not pure white walls of text.
- **Accent / brand:** cyan `feLightBlue-500` `#00DFFF` (`rgb(0, 223, 255)`).
- **Secondary brand blue:** `feBlue-500` `#096EEB` and darker `feBlue-600`/`700` for gradients and shadows.
- **Surfaces / cards:** elevated dark blues (`feDarkBlue-600` `#141A24`, `feDarkBlue-500` `#192845`); avoid bright white panels.
- **Dividers / borders:** low-contrast navy/gray (`rgba(21, 32, 56, 1)` or `rgba(255,255,255,0.08)`).
- **Focus ring:** brand cyan at ~70% alpha.
- **Fonts:** Inter (UI sans); Fira Code or equivalent for mono/code.
- **Max shell width:** ~1460px centered content; generous horizontal padding.

## Interaction chrome

- Sticky top bar: translucent navy (`rgba(9,14,24,0.88)`), light bottom border, backdrop blur.
- Nav/link default `#d5d5d5`; hover/active white or near-white.
- Active/sidebar selection: soft cyan gradient wash (`rgba(0,223,255,0.16)` → transparent) and brighter cyan text.
- Primary CTA: diagonal gradient `from-feLightBlue-600 to-feBlue-600`, uppercase label, `rounded-md`, soft blue shadow; reverse gradient on hover; cyan focus ring.
- Chips/pills: muted gray border/text, large radius; brand chips may use cyan border/fill at low opacity (current landing badge is acceptable).
- Progress / strong activity indicator: solid cyan `#00DFFF`.
- Scrollbars: thin, dark track (`#1b1b1b`) and darker thumb.

## Content patterns

- Prefer calm navy canvases and sparse accent (cyan for brand labels, active states, focus, and primary actions only).
- Cards/feature tiles: dark surface + transparent border; cyan border/background on hover when interactive.
- Inline code / technical emphasis: cool blue-lilac tone (docs use ~`#8ba3d9` on faint tinted background) rather than pure white or yellow.
- Code blocks (if present): deep navy fence (`#131a24` / `#17171e`), subtle border, rounded corners; not light themes.
- Links in body copy: underline/border treatment preferred over loud color fills; hover brightens to white.
- Keep motion subtle (opacity, shadow, short color transitions). No playful/pastel/light SaaS skins.

## HelpDesk mapping

| Concern | Guidance |
| --- | --- |
| Stack | SvelteKit + Tailwind CSS v4 in `frontend/` |
| Token home | Prefer CSS variables in `frontend/src/app.css` (or colocated theme CSS) named for brand/surface/text/focus; mirror FE-Docs values above |
| Utilities | Map Tailwind theme colors to the `feBlue` / `feDarkBlue` / `feLightBlue` scales (or equivalent CSS-var-backed tokens) |
| Components | Build app-local UI; do not copy FE-Docs docs chrome (search modal, docs sidebar, kit-docs prose) unless a page truly needs it |
| Current state | Dark-first FE-Docs navy/cyan tokens live in `frontend/src/app.css` (`fe-*` Tailwind theme); shared sticky shell in `+layout.svelte` + `+layout.server.ts` |
| Auth/profile UI | Registration (`/register`), email verify (`/verify/[code]`), login (`/login`), forgot password (`/forgot-password`), reset password (`/reset-password/[code]`), logout (`POST /logout`), and profile (`/settings/profile`) use this theme; shell shows signed-in name/avatar menu (Edit Profile, Log Out) from Profile `GET /profiles/me` |

## Non-goals

- Pixel-perfect clone of every docs page
- Shipping FE-Docs components/search/docs layout as HelpDesk product UI
- Light-mode parity by default
- Third-party component libraries that impose a conflicting design system (e.g. Material-style light themes)

## Registration UI notes

- Route: `/register` → `frontend/src/routes/register/+page.svelte` + `+page.server.ts`
- Browser never calls Identity directly; named actions only (no `default`, Kit forbids mixing with named): `?/register` → `createIdentityApi().POST('/identities/register')`; success card `?/resend` → Identity `POST /identities/resend-verification`
- Client-only confirm password; only email + password reach the backend
- Success: hide form, show centered check-email notice with backend message; keeps email in form state
- Resend: secondary button; generic success copy; stays on check-email state
- Client-only 30-minute cooldown starts with the success card (register just issued verification; aligns with Identity `ResendCooldown`, no remaining-time from API); live `m:ss` countdown disables the button; after a successful resend the label is **Send again** (CSS uppercase → SEND AGAIN) and the cooldown restarts
- Validation: local field rules mirror Identity (email format, password 12-128); backend problem details mapped via `mapProblemFieldErrors`
- Shell: sticky translucent navy header with brand + Create account nav link

## Email verification UI notes

- Route: `/verify/[code]` → `frontend/src/routes/verify/[code]/`
- Email links open the page only; activation is a deliberate **Verify email** button → SvelteKit action → Identity `GET /identities/verify/{verificationCode}`
- Missing code → error state without button; invalid/backend errors surface after click
- Success → “Account verified” + CTA to `/login`
- Backend email link path is `/verify/{code}` on `UserIdentity:FrontendBaseUrl`, not Identity HTTP

## Login UI notes

- Route: `/login` → `frontend/src/routes/login/+page.svelte` + `+page.server.ts`
- Browser never calls Identity directly; named actions only (no `default`, Kit forbids mixing with named): `?/login` → `createIdentityApi().POST('/identities/login')`; recovery `?/resend` → Identity resend-verification
- On success: BFF writes JWT to HttpOnly `helpdesk_session` cookie via `writeSessionToken` (maxAge from Identity `expiresAt`, capped at 7 days; `Secure` in production) then redirects to a safe relative `redirectTo` (default `/`; rejects protocol-relative/absolute URLs)
- Optional `?redirectTo=` query (e.g. from protected profile page) is echoed as a hidden form field
- Not verified: Identity `Account not verified.` sets `needsVerification`; recovery block offers `?/resend` (prefilled email); generic success banner; keeps sign-in form
- After successful resend: button label **Send again** (CSS uppercase → SEND AGAIN); client-only 30-minute cooldown (aligned with Identity `ResendCooldown`, no remaining-time from API) disables the button and shows a live `m:ss` countdown; first resend from the not-verified state stays immediately available
- Password label row: right-aligned **Forgot password?** → `/forgot-password` (not in shell nav or register)
- Validation: local field rules mirror Identity login (email format/max 320; password required/max 128); backend problem details mapped via `mapProblemFieldErrors`
- Shell nav includes Sign in + Create account when anonymous; post-verify CTA targets this page

## Forgot password UI notes

- Route: `/forgot-password` → `frontend/src/routes/forgot-password/`
- Named action `?/request` → Identity `POST /identities/forgot-password` via `postForgotPassword` helper (returns message + `resetAvailableInSeconds` from `Reset-Available-In`)
- Success: hide form, show opaque check-email card (same generic copy whether or not mail was sent) with secondary **Send again** (CSS uppercase → SEND AGAIN) reusing `?/request` + hidden email, and primary CTA back to `/login`
- 30-minute request cooldown: BFF seeds the client timer from Identity `Reset-Available-In` (falls back to full 30m if header missing); live `m:ss` countdown disables **Send again**; after a successful re-request the timer restarts from the new header value
- Validation: email format/max 320; backend problem details mapped via `mapProblemFieldErrors`

## Reset password UI notes

- Route: `/reset-password/[code]` → `frontend/src/routes/reset-password/[code]/`
- Email links open the page only; password change is a deliberate **Update password** submit → `?/reset` → Identity `POST /identities/reset-password/{resetCode}`
- Missing code → invalid-link UI + link to `/forgot-password`; invalid/backend errors surface after submit
- Client-only confirm password; password rules match register (12-128)
- Success → message + CTA to `/login` (no auto session)
- Backend email link path is `/reset-password/{code}` on `UserIdentity:FrontendBaseUrl`

## Profile UI notes

- Route: `/settings/profile` → `frontend/src/routes/settings/profile/+page.svelte` + `+page.server.ts`
- Auth-gated: no session or Profile 401/403/404 → clear cookie (auth failures) and redirect to `/login?redirectTo=/settings/profile`
- Load: BFF `GET /profiles/me` → `{ profile: { id, email, displayName, status, pictureUrl } }`
- UX: read-only summary by default; **Edit profile** toggles edit mode
- Actions (named): `update` → `PUT /profiles/me` (`displayName` required, max 100, trim); `uploadPicture` → `PUT /profiles/me/picture` multipart field `file` (PNG/JPEG, max 5 MiB); `deletePicture` → `DELETE /profiles/me/picture` when a picture exists
- Browser never calls Profile directly; JWT stays server-only; email is display-only (not client-writable)
- Local validation mirrors Profile rules; backend problem details mapped via `mapProblemFieldErrors`

## Shell session chrome

- Root layout load: `frontend/src/routes/+layout.server.ts`
- Reads `helpdesk_session` via `readSessionToken`; if present, BFF calls Profile `GET /profiles/me` with bearer token via `createProfileApi(token)`
- On success: layout data `{ user: { displayName, pictureUrl } }`. Header shows avatar (or initials) + name as a menu button (`data-testid="shell-profile"`, cyan wash when open or on profile page); Sign in / Create account hidden
- Menu (`data-testid="shell-profile-menu"`): **Edit Profile** → `/settings/profile`; **Log Out** → `POST /logout` (clears session, redirects `/`). Closes on outside pointer, Escape, or Edit Profile click
- On 401/403/404: clear invalid session cookie and treat as anonymous; other failures keep cookie and fall back to anonymous chrome (no throw)
- JWT never sent to the browser; only display fields reach the client

## Logout

- Route: `/logout` → `frontend/src/routes/logout/+page.server.ts`
- `POST` default action: `clearSessionToken` then `303` to `/` (no Identity API call; JWT is BFF cookie only)
- `GET`/`load`: `303` to `/` without clearing (bookmark-safe; logout is POST-only from the shell form)
- Destination `/` is temporary; see [Todo](todo.md) for public issues list redirect

## Sources

- `../FE-Docs/src/vars.css`
- `../FE-Docs/src/app.css`
- `../FE-Docs/tailwind.config.cjs`
- `../FE-Docs/src/lib/styles/fonts.css`
- `../FE-Docs/src/lib/components/site/`
- `../FE-Docs/src/routes/+page.svelte`
- `frontend/src/app.css`
- `frontend/src/routes/+layout.svelte`
- `frontend/src/routes/+layout.server.ts`
- `frontend/src/routes/+page.svelte`
- `frontend/src/routes/register/`
- `frontend/src/routes/verify/[code]/`
- `frontend/src/routes/login/`
- `frontend/src/routes/forgot-password/`
- `frontend/src/routes/reset-password/[code]/`
- `frontend/src/routes/logout/`
- `frontend/src/routes/settings/profile/`
- `frontend/src/lib/server/api/session.ts`
- `frontend/src/lib/server/api/password-reset.ts`
- `frontend/package.json`
