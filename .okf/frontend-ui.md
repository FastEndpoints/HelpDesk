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
- **Primary text:** cool gray `#d1d5db`–`#d5d5d5`; muted/secondary around `#9ca3af` / soft gray `#aaa`.
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

- Prefer calm navy canvases and sparse accent—cyan for brand labels, active states, focus, and primary actions only.
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
| Current state | Landing page already leans slate/cyan-on-dark; new UI should tighten toward the FE-Docs navy/cyan tokens rather than invent a second palette |
| Auth/profile UI | Does not exist yet; when added, use this theme for forms, buttons, alerts, and shell |

## Non-goals

- Pixel-perfect clone of every docs page
- Shipping FE-Docs components/search/docs layout as HelpDesk product UI
- Light-mode parity by default
- Third-party component libraries that impose a conflicting design system (e.g. Material-style light themes)

## Sources

- `../FE-Docs/src/vars.css`
- `../FE-Docs/src/app.css`
- `../FE-Docs/tailwind.config.cjs`
- `../FE-Docs/src/lib/styles/fonts.css`
- `../FE-Docs/src/lib/components/site/`
- `../FE-Docs/src/routes/+page.svelte`
- `frontend/src/app.css`
- `frontend/src/routes/+page.svelte`
- `frontend/package.json`
