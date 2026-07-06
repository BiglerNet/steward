# Steward — Design Tokens

> **Superseded.** The frontend's tokens now come from the `@biglernet/design-tokens` package
> (see `../../src/Steward.Web/src/index.css`), not this file. Kept for historical reference only.

> Bind these to `:root` in every page. They define the full visual system for the prototype.

## Colors

```
--bg:          #f7f7f5    (warm neutral page background)
--surface:     #ffffff    (white — cards, panels, modal backgrounds)
--fg:          #1c1b19    (primary text and headings)
--fg-soft:     #5b5a57    (secondary text, labels, metadata)
--border:      #e7e5e4    (hairline borders on cards and inputs)
--accent:      #2f9e44    (primary action, success, active states)
--accent-hover:#2b8a3e    (primary action hover)
--danger:      #dc2626    (overdue, errors, destructive actions)
--danger-bg:   #fef2f2    (light danger background for badges)
--warn:        #f59e0b    (due soon, warnings)
--warn-bg:     #fffbeb    (light warning background for badges)
--success:     #10b981    (positive stats, valid states)
--success-bg:  #f0fdf4    (light success background for badges)

--light-asset-types:
  boat:        #dff6ff    (light blue background for boat icons)
  car:         #fff3e0    (light amber background for car icons)
  truck:       #e8f5e9    (light green background for truck icons)
  snowmobile:  #e3f2fd    (light sky blue for snowmobile icons)
  utv:         #fce4ec    (light pink for utv icons)
  trailer:     #f3e5f5    (light purple for trailer icons)
  mower:       #e8f5e9    (light green for mower icons)
  powerscraper:#fff8e1    (light yellow for powerscraper icons)
  small-engine:#f5f5dc    (light khaki for small-engine icons)
```

## Typography

```
Font family: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif

Type scale:
  H1:   32px / 700 weight / line-height 1.2 / tracking -0.02em
  H2:   20px / 700 weight / line-height 1.3 / tracking 0
  H3:   16px / 600 weight / line-height 1.4 / tracking 0
  Body: 14px / 500 weight / line-height 1.5 / tracking 0
  Small: 13px / 400 weight / line-height 1.5 / tracking 0
  Caption: 12px / 400 weight / line-height 1.4 / tracking 0

  Large stat value: 36px / 700 / line-height 1 / tracking -0.02em

Caps rules:
  ALL CAPS (badges, labels): letter-spacing +0.06em

Line height by size:
  ≥32px: line-height 1.0–1.2
  14–18px: line-height 1.5
  ≤13px: line-height 1.5
```

## Spacing & Layout

```
Radius:
  12px — cards, panels, stat cards
  8px — buttons, inputs, dropdowns
  6px — small pills, chips
  999px — full pill (badges, avatars)

Grid gap:
  Stats row: 16px
  Asset cards: 16px
  Detail cards: 16px
  Section padding (card body): 16px–20px

Page padding:
  Desktop: 32px horizontal, 56px nav + 32px top margin
  Tablet (768–1024): 24px horizontal, 28px page padding
  Mobile (<768): 16–20px horizontal

Max content width:
  Dashboard page: 1200px (centered)
  Detail pages: 1100px (centered)
```

## Component Classes

```
.btn-primary       — bg: accent, text: white, border-radius: 8px, padding: 8px 16px, font-weight: 600
.btn-secondary     — bg: surface, border: 1px solid border, color: fg-soft, radius: 8px, padding: 8px 14px
.stat-card         — bg: surface, border: 1px solid border, radius: 12px, padding: 20px, text-align center
.asset-card        — bg: surface, border: 1px solid border, radius: 12px, padding 20px, hover: translateY(-2px)
.detail-card       — bg: surface, border: 1px solid border, radius: 12px, overflow hidden
  .detail-card-header — bg: bg (light), border-bottom: 1px solid border, padding 14px 20px, font-weight 600
  .detail-card-body     — padding 16px 20px
  .detail-row           — flex justify-space-between, padding 8px 0, border-bottom: 1px solid bg
.tabs              — border-bottom: 1px solid border
.tab               — padding 12px 20px, color: fg-soft, border-bottom: 2px solid transparent, hover color: fg
.tab.active        — color: accent, border-bottom-color: accent
.due-item          — flex, padding 14px 20px, border-bottom: 1px solid border
.due-status-dot    — 8px circle: red (overdue), amber (soon), green (upcoming)
.due-badge         — small pill: uppercase, letter-spacing +0.06em
.record-table      — border-collapse, th bg:bg, td padding 12px 20px
.engine-item       — flex, padding 14px 0, border-bottom: 1px solid bg
```

## Navigation

```
Top nav: height 56px, bg surface, border-bottom 1px solid border
Nav link: height 56px, padding 0 14px, color fg-soft, active border-bottom accent (1.5px)
Nav link hover: color fg
Nav brand: 24px icon + text, font-weight 600, letter-spacing -0.02em
Avatar: 28px circle, bg accent, text white, font-weight 600
Responsive: label text hidden, icons only below 768px
```

## Interaction States

```
Hover:
  Button: darken bg by 3% (accent-hover)
  Card: translateY(-2px), box-shadow: 0 4px 16px rgba(0,0,0,0.1)
  Nav link: color fg + border-bottom accent

Focus:
  Input: outline 2px solid accent
  Tab: border-bottom solid accent (same as active)

Disabled:
  opacity: 0.3, cursor: default
```
