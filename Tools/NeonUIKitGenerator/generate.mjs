import sharp from 'sharp';
import { randomUUID } from 'crypto';
import fs from 'fs';
import path from 'path';

const OUT_ROOT = path.resolve(
  '../../Assets/_Game/Sprites/NeonUIKit'
);

const COLORS = {
  cyan: '#25F5FF',
  cyanBright: '#77FFFF',
  pink: '#FF2FB5',
  pinkBright: '#FF71D0',
  green: '#8DFF32',
  greenBright: '#BFFF63',
  yellow: '#FFE84D',
  orange: '#FFC43A',
  purple: '#B44BFF',
  panelBg: '#0B1018',
  darkBg: '#05070A',
  white: '#FFFFFF',
  text: '#BFD9FF',
};

const BORDER = 24;
const RADIUS = 18;

/** @type {{ path: string, svg: string, slice?: number, trim?: boolean }[]} */
const assets = [];

function hexRgb(hex) {
  const h = hex.replace('#', '');
  return {
    r: parseInt(h.slice(0, 2), 16),
    g: parseInt(h.slice(2, 4), 16),
    b: parseInt(h.slice(4, 6), 16),
  };
}

function glowFilter(id, color, blur = 4) {
  return `
    <filter id="${id}" x="-50%" y="-50%" width="200%" height="200%">
      <feGaussianBlur in="SourceGraphic" stdDeviation="${blur}" result="b1"/>
      <feGaussianBlur in="SourceGraphic" stdDeviation="${blur * 2}" result="b2"/>
      <feMerge>
        <feMergeNode in="b2"/>
        <feMergeNode in="b1"/>
        <feMergeNode in="SourceGraphic"/>
      </feMerge>
    </filter>`;
}

function cornerAccents(x, y, w, h, color, inset = 10) {
  const s = 14;
  return `
    <line x1="${x + inset}" y1="${y + inset}" x2="${x + inset + s}" y2="${y + inset}" stroke="${color}" stroke-width="2" stroke-linecap="round"/>
    <line x1="${x + inset}" y1="${y + inset}" x2="${x + inset}" y2="${y + inset + s}" stroke="${color}" stroke-width="2" stroke-linecap="round"/>
    <line x1="${x + w - inset}" y1="${y + inset}" x2="${x + w - inset - s}" y2="${y + inset}" stroke="${color}" stroke-width="2" stroke-linecap="round"/>
    <line x1="${x + w - inset}" y1="${y + inset}" x2="${x + w - inset}" y2="${y + inset + s}" stroke="${color}" stroke-width="2" stroke-linecap="round"/>
    <line x1="${x + inset}" y1="${y + h - inset}" x2="${x + inset + s}" y2="${y + h - inset}" stroke="${color}" stroke-width="2" stroke-linecap="round"/>
    <line x1="${x + inset}" y1="${y + h - inset}" x2="${x + inset}" y2="${y + h - inset - s}" stroke="${color}" stroke-width="2" stroke-linecap="round"/>
    <line x1="${x + w - inset}" y1="${y + h - inset}" x2="${x + w - inset - s}" y2="${y + h - inset}" stroke="${color}" stroke-width="2" stroke-linecap="round"/>
    <line x1="${x + w - inset}" y1="${y + h - inset}" x2="${x + w - inset}" y2="${y + h - inset - s}" stroke="${color}" stroke-width="2" stroke-linecap="round"/>
  `;
}

function panelSvg(w, h, color, withClose = false) {
  const pad = 8;
  const fx = pad;
  const fy = pad;
  const fw = w - pad * 2;
  const fh = h - pad * 2;
  const closeBtn = withClose
    ? `<circle cx="${fx + fw - 22}" cy="${fy + 22}" r="14" fill="none" stroke="${COLORS.pink}" stroke-width="2" filter="url(#glow)"/>
       <line x1="${fx + fw - 28}" y1="${fy + 16}" x2="${fx + fw - 16}" y2="${fy + 28}" stroke="${COLORS.pinkBright}" stroke-width="2.5" stroke-linecap="round"/>
       <line x1="${fx + fw - 16}" y1="${fy + 16}" x2="${fx + fw - 28}" y2="${fy + 28}" stroke="${COLORS.pinkBright}" stroke-width="2.5" stroke-linecap="round"/>`
    : '';
  return `
    <svg width="${w}" height="${h}" xmlns="http://www.w3.org/2000/svg">
      <defs>${glowFilter('glow', color)}</defs>
      <rect x="${fx}" y="${fy}" width="${fw}" height="${fh}" rx="${RADIUS}" ry="${RADIUS}" fill="${COLORS.panelBg}" fill-opacity="0.92"/>
      <rect x="${fx}" y="${fy}" width="${fw}" height="${fh}" rx="${RADIUS}" ry="${RADIUS}" fill="none" stroke="${color}" stroke-width="3" filter="url(#glow)"/>
      <rect x="${fx + 3}" y="${fy + 3}" width="${fw - 6}" height="${fh - 6}" rx="${RADIUS - 2}" ry="${RADIUS - 2}" fill="none" stroke="${color}" stroke-width="1" opacity="0.55"/>
      ${cornerAccents(fx, fy, fw, fh, color)}
      ${closeBtn}
    </svg>`;
}

function buttonSvg(w, h, color, ghost = false) {
  const pad = 8;
  const fx = pad;
  const fy = pad;
  const fw = w - pad * 2;
  const fh = h - pad * 2;
  const fill = ghost
    ? 'fill="none"'
    : `fill="${COLORS.panelBg}" fill-opacity="0.85"`;
  return `
    <svg width="${w}" height="${h}" xmlns="http://www.w3.org/2000/svg">
      <defs>${glowFilter('glow', color)}</defs>
      <rect x="${fx}" y="${fy}" width="${fw}" height="${fh}" rx="${fh / 2}" ry="${fh / 2}" ${fill}/>
      <rect x="${fx}" y="${fy}" width="${fw}" height="${fh}" rx="${fh / 2}" ry="${fh / 2}" fill="none" stroke="${color}" stroke-width="3" filter="url(#glow)"/>
    </svg>`;
}

function iconSvg(drawFn, size = 128) {
  return `
    <svg width="${size}" height="${size}" viewBox="0 0 ${size} ${size}" xmlns="http://www.w3.org/2000/svg">
      <defs>${glowFilter('glow', COLORS.cyan)}${glowFilter('glowPink', COLORS.pink)}${glowFilter('glowGreen', COLORS.green)}${glowFilter('glowYellow', COLORS.yellow)}</defs>
      ${drawFn(size)}
    </svg>`;
}

function addPanel(name, w, h, color, folder = 'Panels', withClose = false) {
  assets.push({
    path: `${folder}/${name}.png`,
    svg: panelSvg(w, h, color, withClose),
    slice: BORDER,
  });
}

function addButton(name, w, h, color, folder = 'Buttons', ghost = false) {
  assets.push({
    path: `${folder}/${name}.png`,
    svg: buttonSvg(w, h, color, ghost),
    slice: BORDER,
  });
}

function addIcon(name, drawFn, folder = 'Icons') {
  assets.push({
    path: `${folder}/${name}.png`,
    svg: iconSvg(drawFn),
    trim: true,
  });
}

// --- Panels ---
addPanel('Panel_Small_Cyan', 220, 140, COLORS.cyan);
addPanel('Panel_Small_Pink', 220, 140, COLORS.pink);
addPanel('Panel_Medium_Cyan', 340, 180, COLORS.cyan);
addPanel('Panel_Medium_Pink', 340, 180, COLORS.pink);
addPanel('Panel_Large_Cyan', 500, 260, COLORS.cyan);
addPanel('Panel_Large_Pink', 500, 260, COLORS.pink);
addPanel('Panel_ExtraLarge_Cyan', 660, 340, COLORS.cyan);
addPanel('Panel_ExtraLarge_Pink', 660, 340, COLORS.pink);
addPanel('Popup_Small', 280, 280, COLORS.pink, 'Panels', true);
addPanel('Popup_Medium', 420, 420, COLORS.pink, 'Panels', true);
addPanel('Popup_Large', 560, 560, COLORS.pink, 'Panels', true);

// --- Buttons ---
const btnColors = [
  ['Cyan', COLORS.cyan],
  ['Pink', COLORS.pink],
  ['Green', COLORS.green],
  ['Yellow', COLORS.yellow],
];
for (const [label, color] of btnColors) {
  addButton(`Button_Long_${label}`, 500, 100, color);
  addButton(`Button_Medium_${label}`, 340, 100, color);
  addButton(`Button_Small_${label}`, 220, 88, color);
  addButton(`Button_Square_${label}`, 100, 100, color);
  addButton(`Button_Long_Ghost_${label}`, 500, 100, color, 'Buttons', true);
}

// --- Icons ---
addIcon('Icon_Close', (s) => {
  const c = s / 2;
  return `
    <circle cx="${c}" cy="${c}" r="${s * 0.32}" fill="none" stroke="${COLORS.pink}" stroke-width="3" filter="url(#glowPink)"/>
    <line x1="${c - 14}" y1="${c - 14}" x2="${c + 14}" y2="${c + 14}" stroke="${COLORS.pinkBright}" stroke-width="3" stroke-linecap="round"/>
    <line x1="${c + 14}" y1="${c - 14}" x2="${c - 14}" y2="${c + 14}" stroke="${COLORS.pinkBright}" stroke-width="3" stroke-linecap="round"/>`;
});

addIcon('Icon_Refresh', (s) => {
  const c = s / 2;
  return `
    <circle cx="${c}" cy="${c}" r="${s * 0.28}" fill="none" stroke="${COLORS.cyan}" stroke-width="3" filter="url(#glow)"/>
    <path d="M ${c + 18} ${c - 8} A 22 22 0 1 0 ${c + 22} ${c + 10}" fill="none" stroke="${COLORS.cyanBright}" stroke-width="3" stroke-linecap="round"/>
    <polygon points="${c + 26},${c - 14} ${c + 34},${c - 2} ${c + 18},${c - 2}" fill="${COLORS.cyanBright}"/>`;
});

addIcon('Icon_Settings', (s) => {
  const c = s / 2;
  return `
    <circle cx="${c}" cy="${c}" r="12" fill="none" stroke="${COLORS.pink}" stroke-width="3" filter="url(#glowPink)"/>
    <path d="M ${c} ${c - 30} L ${c + 6} ${c - 24} L ${c + 14} ${c - 26} L ${c + 18} ${c - 18} L ${c + 26} ${c - 14} L ${c + 24} ${c - 6} L ${c + 30} ${c} L ${c + 24} ${c + 6} L ${c + 26} ${c + 14} L ${c + 18} ${c + 18} L ${c + 14} ${c + 26} L ${c + 6} ${c + 24} L ${c} ${c + 30} L ${c - 6} ${c + 24} L ${c - 14} ${c + 26} L ${c - 18} ${c + 18} L ${c - 26} ${c + 14} L ${c - 24} ${c + 6} L ${c - 30} ${c} L ${c - 24} ${c - 6} L ${c - 26} ${c - 14} L ${c - 18} ${c - 18} L ${c - 14} ${c - 26} L ${c - 6} ${c - 24} Z"
      fill="none" stroke="${COLORS.pink}" stroke-width="2.5" filter="url(#glowPink)"/>`;
});

addIcon('Icon_Sound', (s) => {
  const c = s / 2;
  return `
    <polygon points="${c - 22},${c - 8} ${c - 10},${c - 8} ${c + 2},${c - 18} ${c + 2},${c + 18} ${c - 10},${c + 8} ${c - 22},${c + 8}" fill="none" stroke="${COLORS.cyan}" stroke-width="3" filter="url(#glow)"/>
    <path d="M ${c + 12} ${c - 12} Q ${c + 28} ${c} ${c + 12} ${c + 12}" fill="none" stroke="${COLORS.cyanBright}" stroke-width="3" stroke-linecap="round"/>
    <path d="M ${c + 20} ${c - 18} Q ${c + 40} ${c} ${c + 20} ${c + 18}" fill="none" stroke="${COLORS.cyanBright}" stroke-width="2.5" stroke-linecap="round" opacity="0.7"/>`;
});

addIcon('Icon_Haptics', (s) => {
  const c = s / 2;
  return `
    <rect x="${c - 14}" y="${c - 24}" width="28" height="48" rx="6" fill="none" stroke="${COLORS.cyan}" stroke-width="3" filter="url(#glow)"/>
    <line x1="${c - 28}" y1="${c - 8}" x2="${c - 20}" y2="${c - 8}" stroke="${COLORS.cyanBright}" stroke-width="2.5" stroke-linecap="round"/>
    <line x1="${c - 32}" y1="${c}" x2="${c - 20}" y2="${c}" stroke="${COLORS.cyanBright}" stroke-width="2.5" stroke-linecap="round"/>
    <line x1="${c - 28}" y1="${c + 8}" x2="${c - 20}" y2="${c + 8}" stroke="${COLORS.cyanBright}" stroke-width="2.5" stroke-linecap="round"/>
    <line x1="${c + 20}" y1="${c - 8}" x2="${c + 28}" y2="${c - 8}" stroke="${COLORS.cyanBright}" stroke-width="2.5" stroke-linecap="round"/>
    <line x1="${c + 20}" y1="${c}" x2="${c + 32}" y2="${c}" stroke="${COLORS.cyanBright}" stroke-width="2.5" stroke-linecap="round"/>
    <line x1="${c + 20}" y1="${c + 8}" x2="${c + 28}" y2="${c + 8}" stroke="${COLORS.cyanBright}" stroke-width="2.5" stroke-linecap="round"/>`;
});

addIcon('Icon_Warning', (s) => {
  const c = s / 2;
  return `
    <polygon points="${c},${c - 30} ${c + 28},${c + 22} ${c - 28},${c + 22}" fill="none" stroke="${COLORS.pink}" stroke-width="3" filter="url(#glowPink)"/>
    <line x1="${c}" y1="${c - 8}" x2="${c}" y2="${c + 6}" stroke="${COLORS.pinkBright}" stroke-width="3" stroke-linecap="round"/>
    <circle cx="${c}" cy="${c + 14}" r="2.5" fill="${COLORS.pinkBright}"/>`;
});

function heartPath(c, scale = 1) {
  const sx = scale;
  return `M ${c} ${c + 14 * sx}
    C ${c} ${c - 2 * sx} ${c - 22 * sx} ${c - 10 * sx} ${c - 22 * sx} ${c + 6 * sx}
    C ${c - 22 * sx} ${c + 22 * sx} ${c} ${c + 30 * sx} ${c} ${c + 30 * sx}
    C ${c} ${c + 30 * sx} ${c + 22 * sx} ${c + 22 * sx} ${c + 22 * sx} ${c + 6 * sx}
    C ${c + 22 * sx} ${c - 10 * sx} ${c} ${c - 2 * sx} ${c} ${c + 14 * sx} Z`;
}

addIcon('Icon_Heart_Full', (s) => {
  const c = s / 2;
  return `<path d="${heartPath(c)}" fill="${COLORS.pink}" fill-opacity="0.35" stroke="${COLORS.pink}" stroke-width="3" filter="url(#glowPink)"/>`;
}, 'HeartsStars');

addIcon('Icon_Heart_Empty', (s) => {
  const c = s / 2;
  return `<path d="${heartPath(c)}" fill="none" stroke="${COLORS.pink}" stroke-width="3" filter="url(#glowPink)"/>`;
}, 'HeartsStars');

addIcon('Icon_Heart_Half', (s) => {
  const c = s / 2;
  return `
    <defs><clipPath id="half"><rect x="${c - 40}" y="${c - 40}" width="40" height="80"/></clipPath></defs>
    <path d="${heartPath(c)}" fill="${COLORS.pink}" fill-opacity="0.35" stroke="${COLORS.pink}" stroke-width="3" filter="url(#glowPink)" clip-path="url(#half)"/>
    <path d="${heartPath(c)}" fill="none" stroke="${COLORS.pink}" stroke-width="3" filter="url(#glowPink)"/>`;
}, 'HeartsStars');

function starPoints(c, outer, inner, points = 5) {
  let d = '';
  for (let i = 0; i < points * 2; i++) {
    const r = i % 2 === 0 ? outer : inner;
    const a = (Math.PI / 2 * -1) + (i * Math.PI / points);
    const x = c + Math.cos(a) * r;
    const y = c + Math.sin(a) * r;
    d += `${i === 0 ? 'M' : 'L'} ${x.toFixed(1)} ${y.toFixed(1)} `;
  }
  return d + 'Z';
}

addIcon('Icon_Star_Full', (s) => {
  const c = s / 2;
  return `<path d="${starPoints(c, 30, 12)}" fill="${COLORS.yellow}" fill-opacity="0.4" stroke="${COLORS.yellow}" stroke-width="3" filter="url(#glowYellow)"/>`;
}, 'HeartsStars');

addIcon('Icon_Star_Empty', (s) => {
  const c = s / 2;
  return `<path d="${starPoints(c, 30, 12)}" fill="none" stroke="${COLORS.yellow}" stroke-width="3" filter="url(#glowYellow)"/>`;
}, 'HeartsStars');

const arrowDirs = ['Right', 'Left', 'Up', 'Down'];
for (const dir of arrowDirs) {
  addIcon(`Icon_Arrow_${dir}`, (s) => {
    const c = s / 2;
    const rot = { Right: 0, Down: 90, Left: 180, Up: 270 }[dir];
    return `
      <g transform="rotate(${rot} ${c} ${c})">
        <line x1="${c - 20}" y1="${c}" x2="${c + 16}" y2="${c}" stroke="${COLORS.cyan}" stroke-width="4" stroke-linecap="round" filter="url(#glow)"/>
        <polygon points="${c + 20},${c} ${c + 6},${c - 12} ${c + 6},${c + 12}" fill="${COLORS.cyanBright}"/>
      </g>`;
  });
}

addIcon('Icon_Circle', (s) => {
  const c = s / 2;
  return `<circle cx="${c}" cy="${c}" r="24" fill="none" stroke="${COLORS.cyan}" stroke-width="3" filter="url(#glow)"/>`;
});

addIcon('Icon_Check', (s) => {
  const c = s / 2;
  return `
    <rect x="${c - 24}" y="${c - 24}" width="48" height="48" rx="8" fill="none" stroke="${COLORS.green}" stroke-width="3" filter="url(#glowGreen)"/>
    <polyline points="${c - 12},${c} ${c - 2},${c + 12} ${c + 16},${c - 12}" fill="none" stroke="${COLORS.greenBright}" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"/>`;
});

addIcon('Icon_Lock', (s) => {
  const c = s / 2;
  return `
    <rect x="${c - 20}" y="${c - 4}" width="40" height="30" rx="6" fill="none" stroke="${COLORS.purple}" stroke-width="3"/>
    <path d="M ${c - 14} ${c - 4} V ${c - 16} A 14 14 0 0 1 ${c + 14} ${c - 16} V ${c - 4}" fill="none" stroke="${COLORS.purple}" stroke-width="3"/>`;
});

addIcon('Icon_Unlock', (s) => {
  const c = s / 2;
  return `
    <rect x="${c - 20}" y="${c - 4}" width="40" height="30" rx="6" fill="none" stroke="${COLORS.cyan}" stroke-width="3" filter="url(#glow)"/>
    <path d="M ${c - 14} ${c - 4} V ${c - 16} A 14 14 0 0 1 ${c + 8} ${c - 20}" fill="none" stroke="${COLORS.cyan}" stroke-width="3" filter="url(#glow)"/>`;
});

// --- Toggles ---
function toggleSvg(on) {
  const w = 160;
  const h = 80;
  const color = on ? COLORS.green : COLORS.pink;
  const knobX = on ? 104 : 56;
  return `
    <svg width="${w}" height="${h}" xmlns="http://www.w3.org/2000/svg">
      <defs>${glowFilter('glow', color)}</defs>
      <rect x="8" y="20" width="144" height="40" rx="20" fill="${COLORS.panelBg}" stroke="${color}" stroke-width="3" filter="url(#glow)"/>
      <circle cx="${knobX}" cy="40" r="16" fill="${color}" filter="url(#glow)"/>
    </svg>`;
}

assets.push({ path: 'Toggles/Toggle_BG_Off.png', svg: toggleSvg(false), trim: true });
assets.push({ path: 'Toggles/Toggle_BG_On.png', svg: toggleSvg(true), trim: true });
assets.push({
  path: 'Toggles/Toggle_Knob.png',
  svg: iconSvg((s) => {
    const c = s / 2;
    return `<circle cx="${c}" cy="${c}" r="18" fill="${COLORS.cyan}" filter="url(#glow)"/>`;
  }, 64),
  trim: true,
});

assets.push({
  path: 'Toggles/Checkbox_Off.png',
  svg: iconSvg((s) => {
    const c = s / 2;
    return `<rect x="${c - 22}" y="${c - 22}" width="44" height="44" rx="8" fill="none" stroke="${COLORS.cyan}" stroke-width="3" filter="url(#glow)"/>`;
  }),
  trim: true,
});

assets.push({
  path: 'Toggles/Checkbox_On.png',
  svg: iconSvg((s) => {
    const c = s / 2;
    return `
      <rect x="${c - 22}" y="${c - 22}" width="44" height="44" rx="8" fill="none" stroke="${COLORS.green}" stroke-width="3" filter="url(#glowGreen)"/>
      <polyline points="${c - 10},${c} ${c - 2},${c + 10} ${c + 12},${c - 10}" fill="none" stroke="${COLORS.greenBright}" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"/>`;
  }),
  trim: true,
});

// --- Progress bars ---
function progressBarSvg(w, h, color, fillOnly = false) {
  const pad = 8;
  if (fillOnly) {
    return `
      <svg width="${w}" height="${h}" xmlns="http://www.w3.org/2000/svg">
        <defs>${glowFilter('glow', color)}</defs>
        <rect x="${pad}" y="${pad}" width="${w - pad * 2}" height="${h - pad * 2}" rx="${(h - pad * 2) / 2}" fill="${color}" fill-opacity="0.85" filter="url(#glow)"/>
      </svg>`;
  }
  return `
    <svg width="${w}" height="${h}" xmlns="http://www.w3.org/2000/svg">
      <rect x="${pad}" y="${pad}" width="${w - pad * 2}" height="${h - pad * 2}" rx="${(h - pad * 2) / 2}" fill="${COLORS.panelBg}" stroke="#1A2433" stroke-width="2"/>
    </svg>`;
}

assets.push({ path: 'ProgressBars/Progress_BG.png', svg: progressBarSvg(400, 48), slice: 20 });
for (const [label, color] of [['Cyan', COLORS.cyan], ['Pink', COLORS.pink], ['Green', COLORS.green], ['Yellow', COLORS.yellow]]) {
  assets.push({ path: `ProgressBars/Progress_Fill_${label}.png`, svg: progressBarSvg(400, 48, color, true), slice: 20 });
}

// --- Level cells ---
function levelCellSvg(state) {
  const w = 120;
  const h = 120;
  const colors = {
    Locked: COLORS.purple,
    Unlocked: COLORS.pink,
    Selected: COLORS.cyan,
  };
  const color = colors[state];
  const innerGlow = state === 'Selected'
    ? `<rect x="20" y="20" width="80" height="80" rx="12" fill="${COLORS.cyan}" fill-opacity="0.15"/>`
    : '';
  return `
    <svg width="${w}" height="${h}" xmlns="http://www.w3.org/2000/svg">
      <defs>${glowFilter('glow', color)}</defs>
      <rect x="8" y="8" width="104" height="104" rx="16" fill="${COLORS.panelBg}" fill-opacity="0.9"/>
      <rect x="8" y="8" width="104" height="104" rx="16" fill="none" stroke="${color}" stroke-width="3" filter="url(#glow)"/>
      ${innerGlow}
    </svg>`;
}

for (const state of ['Locked', 'Unlocked', 'Selected']) {
  assets.push({ path: `LevelCells/Level_Cell_${state}.png`, svg: levelCellSvg(state), slice: 20 });
}

// --- Frames ---
function frameSvg(w, h, color, thick = false) {
  const sw = thick ? 5 : 2;
  return `
    <svg width="${w}" height="${h}" xmlns="http://www.w3.org/2000/svg">
      <defs>${glowFilter('glow', color)}</defs>
      <rect x="8" y="8" width="${w - 16}" height="${h - 16}" rx="8" fill="none" stroke="${color}" stroke-width="${sw}" filter="url(#glow)"/>
      ${cornerAccents(8, 8, w - 16, h - 16, color, 4)}
    </svg>`;
}

assets.push({ path: 'Frames/Frame_Thin_Cyan.png', svg: frameSvg(260, 160, COLORS.cyan), slice: 16 });
assets.push({ path: 'Frames/Frame_Thin_Pink.png', svg: frameSvg(260, 160, COLORS.pink), slice: 16 });
assets.push({ path: 'Frames/Frame_Thin_Green.png', svg: frameSvg(260, 160, COLORS.green), slice: 16 });
assets.push({ path: 'Frames/Frame_Thick_Cyan.png', svg: frameSvg(260, 160, COLORS.cyan, true), slice: 16 });

for (const corner of ['TL', 'TR', 'BL', 'BR']) {
  assets.push({
    path: `Frames/Corner_${corner}.png`,
    svg: iconSvg((s) => {
      const flipX = corner.includes('R') ? -1 : 1;
      const flipY = corner.includes('B') ? -1 : 1;
      const c = s / 2;
      return `
        <g transform="translate(${c} ${c}) scale(${flipX} ${flipY}) translate(${-c} ${-c})">
          <line x1="24" y1="24" x2="54" y2="24" stroke="${COLORS.cyan}" stroke-width="3" stroke-linecap="round" filter="url(#glow)"/>
          <line x1="24" y1="24" x2="24" y2="54" stroke="${COLORS.cyan}" stroke-width="3" stroke-linecap="round" filter="url(#glow)"/>
          <line x1="24" y1="24" x2="44" y2="44" stroke="${COLORS.cyanBright}" stroke-width="1.5" opacity="0.6"/>
        </g>`;
    }, 80),
    trim: true,
  });
}

assets.push({
  path: 'Frames/Divider_H.png',
  svg: `<svg width="300" height="24" xmlns="http://www.w3.org/2000/svg"><defs>${glowFilter('glow', COLORS.cyan)}</defs><line x1="8" y1="12" x2="292" y2="12" stroke="${COLORS.cyan}" stroke-width="2" filter="url(#glow)"/><circle cx="8" cy="12" r="3" fill="${COLORS.cyanBright}"/><circle cx="292" cy="12" r="3" fill="${COLORS.cyanBright}"/></svg>`,
  slice: 12,
});

assets.push({
  path: 'Frames/Divider_V.png',
  svg: `<svg width="24" height="300" xmlns="http://www.w3.org/2000/svg"><defs>${glowFilter('glow', COLORS.cyan)}</defs><line x1="12" y1="8" x2="12" y2="292" stroke="${COLORS.cyan}" stroke-width="2" filter="url(#glow)"/></svg>`,
  slice: 12,
});

// --- HUD ---
assets.push({ path: 'HUD/HUD_Frame.png', svg: panelSvg(600, 140, COLORS.cyan), slice: BORDER });
assets.push({
  path: 'HUD/HUD_Circle.png',
  svg: iconSvg((s) => {
    const c = s / 2;
    return `<circle cx="${c}" cy="${c}" r="34" fill="none" stroke="${COLORS.cyan}" stroke-width="4" filter="url(#glow)"/>`;
  }, 96),
  trim: true,
});

assets.push({
  path: 'HUD/HUD_Bar.png',
  svg: `<svg width="200" height="48" xmlns="http://www.w3.org/2000/svg"><defs>${glowFilter('glow', COLORS.cyan)}</defs><rect x="8" y="12" width="184" height="24" rx="12" fill="${COLORS.panelBg}" stroke="${COLORS.cyan}" stroke-width="2" filter="url(#glow)"/></svg>`,
  slice: 16,
});

// --- Arrows (large) ---
for (const dir of arrowDirs) {
  assets.push({
    path: `Arrows/Arrow_${dir}.png`,
    svg: iconSvg((s) => {
      const c = s / 2;
      const rot = { Right: 0, Down: 90, Left: 180, Up: 270 }[dir];
      return `
        <g transform="rotate(${rot} ${c} ${c})">
          <line x1="${c - 28}" y1="${c}" x2="${c + 20}" y2="${c}" stroke="${COLORS.cyan}" stroke-width="5" stroke-linecap="round" filter="url(#glow)"/>
          <polygon points="${c + 28},${c} ${c + 8},${c - 16} ${c + 8},${c + 16}" fill="${COLORS.cyanBright}"/>
        </g>`;
    }, 96),
    trim: true,
  });
}

assets.push({
  path: 'Arrows/Arrow_DoubleRight.png',
  svg: iconSvg((s) => {
    const c = s / 2;
    return `
      <g>
        <polygon points="${c + 10},${c} ${c - 6},${c - 14} ${c - 6},${c + 14}" fill="${COLORS.cyanBright}" filter="url(#glow)"/>
        <polygon points="${c + 30},${c} ${c + 14},${c - 14} ${c + 14},${c + 14}" fill="${COLORS.cyanBright}" filter="url(#glow)"/>
      </g>`;
  }, 96),
  trim: true,
});

// --- Badges ---
for (const [label, color] of [['Cyan', COLORS.cyan], ['Pink', COLORS.pink], ['Green', COLORS.green]]) {
  assets.push({
    path: `Badges/Badge_${label}.png`,
    svg: iconSvg((s) => {
      const c = s / 2;
      return `<circle cx="${c}" cy="${c}" r="28" fill="${COLORS.panelBg}" stroke="${color}" stroke-width="3" filter="url(#glow)"/>`;
    }, 80),
    trim: true,
  });
}

assets.push({
  path: 'Badges/Marker_Diamond.png',
  svg: iconSvg((s) => {
    const c = s / 2;
    return `<polygon points="${c},${c - 24} ${c + 20},${c} ${c},${c + 24} ${c - 20},${c}" fill="none" stroke="${COLORS.yellow}" stroke-width="3" filter="url(#glowYellow)"/>`;
  }, 80),
  trim: true,
});

assets.push({
  path: 'Badges/Marker_Triangle.png',
  svg: iconSvg((s) => {
    const c = s / 2;
    return `<polygon points="${c},${c - 24} ${c + 22},${c + 18} ${c - 22},${c + 18}" fill="none" stroke="${COLORS.pink}" stroke-width="3" filter="url(#glowPink)"/>`;
  }, 80),
  trim: true,
});

// --- Sliders ---
assets.push({
  path: 'Sliders/Slider_Track.png',
  svg: `<svg width="400" height="32" xmlns="http://www.w3.org/2000/svg"><defs>${glowFilter('glow', COLORS.cyan)}</defs><line x1="16" y1="16" x2="384" y2="16" stroke="${COLORS.cyan}" stroke-width="3" filter="url(#glow)"/><circle cx="16" cy="16" r="4" fill="${COLORS.cyanBright}"/><circle cx="384" cy="16" r="4" fill="${COLORS.cyanBright}"/></svg>`,
  slice: 20,
});

assets.push({
  path: 'Sliders/Slider_Knob.png',
  svg: iconSvg((s) => {
    const c = s / 2;
    return `<circle cx="${c}" cy="${c}" r="16" fill="${COLORS.cyan}" stroke="${COLORS.cyanBright}" stroke-width="2" filter="url(#glow)"/>`;
  }, 64),
  trim: true,
});

// --- Effects ---
for (const [label, color] of [['Cyan', COLORS.cyan], ['Pink', COLORS.pink], ['Green', COLORS.green], ['Yellow', COLORS.yellow], ['Purple', COLORS.purple]]) {
  assets.push({
    path: `Effects/Glow_${label}.png`,
    svg: `<svg width="128" height="128" xmlns="http://www.w3.org/2000/svg"><defs><radialGradient id="g" cx="50%" cy="50%" r="50%"><stop offset="0%" stop-color="${color}" stop-opacity="0.9"/><stop offset="70%" stop-color="${color}" stop-opacity="0.25"/><stop offset="100%" stop-color="${color}" stop-opacity="0"/></radialGradient></defs><circle cx="64" cy="64" r="56" fill="url(#g)"/></svg>`,
    trim: true,
  });
}

assets.push({
  path: 'Effects/Light_Streak.png',
  svg: `<svg width="256" height="64" xmlns="http://www.w3.org/2000/svg"><defs><linearGradient id="ls" x1="0" y1="0" x2="1" y2="0"><stop offset="0%" stop-color="${COLORS.cyan}" stop-opacity="0"/><stop offset="50%" stop-color="${COLORS.cyanBright}" stop-opacity="0.9"/><stop offset="100%" stop-color="${COLORS.cyan}" stop-opacity="0"/></linearGradient></defs><rect x="0" y="28" width="256" height="8" fill="url(#ls)"/></svg>`,
  trim: true,
});

assets.push({
  path: 'Effects/Sparkle.png',
  svg: iconSvg((s) => {
    const c = s / 2;
    return `
      <line x1="${c}" y1="${c - 20}" x2="${c}" y2="${c + 20}" stroke="${COLORS.yellow}" stroke-width="2" filter="url(#glowYellow)"/>
      <line x1="${c - 20}" y1="${c}" x2="${c + 20}" y2="${c}" stroke="${COLORS.yellow}" stroke-width="2" filter="url(#glowYellow)"/>
      <line x1="${c - 14}" y1="${c - 14}" x2="${c + 14}" y2="${c + 14}" stroke="${COLORS.yellow}" stroke-width="1.5" opacity="0.7"/>
      <line x1="${c + 14}" y1="${c - 14}" x2="${c - 14}" y2="${c + 14}" stroke="${COLORS.yellow}" stroke-width="1.5" opacity="0.7"/>`;
  }, 64),
  trim: true,
});

// --- Decorations ---
assets.push({
  path: 'Decorations/Deco_Border_Bottom.png',
  svg: `<svg width="400" height="48" xmlns="http://www.w3.org/2000/svg"><defs>${glowFilter('glow', COLORS.cyan)}</defs><path d="M 8 8 L 60 8 L 80 32 L 320 32 L 340 8 L 392 8" fill="none" stroke="${COLORS.cyan}" stroke-width="2" filter="url(#glow)"/></svg>`,
  slice: 20,
});

assets.push({
  path: 'Decorations/Deco_Circuit_01.png',
  svg: iconSvg((s) => {
    const c = s / 2;
    return `
      <path d="M ${c - 30} ${c} H ${c - 10} V ${c - 20} H ${c + 10} V ${c + 20} H ${c + 30}" fill="none" stroke="${COLORS.cyan}" stroke-width="2" filter="url(#glow)"/>
      <circle cx="${c - 30}" cy="${c}" r="3" fill="${COLORS.cyanBright}"/>
      <circle cx="${c + 30}" cy="${c + 20}" r="3" fill="${COLORS.cyanBright}"/>`;
  }, 96),
  trim: true,
});

assets.push({
  path: 'Decorations/Deco_Dots.png',
  svg: `<svg width="200" height="24" xmlns="http://www.w3.org/2000/svg">${Array.from({ length: 8 }, (_, i) => `<circle cx="${16 + i * 24}" cy="12" r="3" fill="${COLORS.cyan}" opacity="0.8"/>`).join('')}</svg>`,
  trim: true,
});

// --- Misc ---
assets.push({
  path: 'Misc/Mask_Round.png',
  svg: iconSvg((s) => {
    const c = s / 2;
    return `<circle cx="${c}" cy="${c}" r="36" fill="#FFFFFF"/>`;
  }, 96),
  trim: true,
});

assets.push({
  path: 'Misc/Shadow_Soft.png',
  svg: `<svg width="128" height="64" xmlns="http://www.w3.org/2000/svg"><defs><radialGradient id="sh" cx="50%" cy="50%" r="50%"><stop offset="0%" stop-color="#000" stop-opacity="0.55"/><stop offset="100%" stop-color="#000" stop-opacity="0"/></radialGradient></defs><ellipse cx="64" cy="32" rx="56" ry="24" fill="url(#sh)"/></svg>`,
  trim: true,
});

assets.push({
  path: 'Misc/Shadow_Hard.png',
  svg: `<svg width="128" height="32" xmlns="http://www.w3.org/2000/svg"><rect x="8" y="8" width="112" height="16" rx="8" fill="#000" fill-opacity="0.45"/></svg>`,
  trim: true,
});

assets.push({
  path: 'Misc/Highlight.png',
  svg: `<svg width="256" height="32" xmlns="http://www.w3.org/2000/svg"><defs><linearGradient id="hl" x1="0" y1="0" x2="0" y2="1"><stop offset="0%" stop-color="#FFFFFF" stop-opacity="0.35"/><stop offset="100%" stop-color="#FFFFFF" stop-opacity="0"/></linearGradient></defs><rect x="0" y="0" width="256" height="32" fill="url(#hl)"/></svg>`,
  trim: true,
});

function makeMeta(guid, slice = 0) {
  const borderLine = slice > 0
    ? `  spriteBorder: {x: ${slice}, y: ${slice}, z: ${slice}, w: ${slice}}`
    : '  spriteBorder: {x: 0, y: 0, z: 0, w: 0}';
  return `fileFormatVersion: 2
guid: ${guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
${borderLine}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 5f3412102233445566778899aabbccdd
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`;
}

function folderMeta(guid) {
  return `fileFormatVersion: 2
guid: ${guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`;
}

async function main() {
  fs.mkdirSync(OUT_ROOT, { recursive: true });
  fs.writeFileSync(
    path.join(path.dirname(OUT_ROOT), 'NeonUIKit.meta'),
    folderMeta(randomUUID().replace(/-/g, ''))
  );

  const manifest = [];
  const folders = new Set(['']);

  for (const asset of assets) {
    const dir = path.dirname(asset.path);
    folders.add(dir);
  }

  const folderGuids = {};
  for (const folder of [...folders].sort()) {
    if (!folder) continue;
    const full = path.join(OUT_ROOT, folder);
    fs.mkdirSync(full, { recursive: true });
    const guid = randomUUID().replace(/-/g, '');
    folderGuids[folder] = guid;
    fs.writeFileSync(path.join(OUT_ROOT, `${folder}.meta`), folderMeta(guid));
  }

  for (const asset of assets) {
    const outPath = path.join(OUT_ROOT, asset.path);
    fs.mkdirSync(path.dirname(outPath), { recursive: true });

    let pipeline = sharp(Buffer.from(asset.svg)).png();
    if (asset.trim) {
      pipeline = pipeline.trim({ threshold: 1 });
    }

    await pipeline.toFile(outPath);

    const guid = randomUUID().replace(/-/g, '');
    fs.writeFileSync(`${outPath}.meta`, makeMeta(guid, asset.slice || 0));

    manifest.push({
      file: asset.path.replace(/\\/g, '/'),
      nineSlice: asset.slice || 0,
      trimmed: !!asset.trim,
    });
  }

  fs.writeFileSync(path.join(OUT_ROOT, 'manifest.json'), JSON.stringify(manifest, null, 2));

  const colorsDoc = {
    name: 'Go-Arrow Neon UI Kit',
    version: '1.0.0',
    pixelsPerUnit: 100,
    referenceResolution: { width: 1080, height: 1920 },
    colors: COLORS,
    legacyThemeMapping: {
      background: '#020208',
      panelFill: '#0F0A1F',
      cyanBorder: '#33F2FF',
      magentaBorder: '#FF33BF',
      success: '#59FF59',
      fail: '#FF4073',
    },
    nineSliceDefault: BORDER,
    importSettings: {
      textureType: 'Sprite (2D and UI)',
      spriteMode: 'Single',
      pixelsPerUnit: 100,
      filterMode: 'Point',
      compression: 'None',
      generateMipMaps: false,
      wrapMode: 'Clamp',
      alphaIsTransparency: true,
    },
  };

  fs.writeFileSync(path.join(OUT_ROOT, 'colors.json'), JSON.stringify(colorsDoc, null, 2));

  const colorsTxt = `# Go-Arrow Neon UI Kit — Color Reference
# Use these hex values in Unity (ColorUtility.TryParseHtmlString) or TMP styles.

Primary Cyan       ${COLORS.cyan}    rgb(37, 245, 255)
Bright Cyan        ${COLORS.cyanBright}    rgb(119, 255, 255)
Primary Pink       ${COLORS.pink}    rgb(255, 47, 181)
Bright Pink        ${COLORS.pinkBright}    rgb(255, 113, 208)
Neon Green         ${COLORS.green}    rgb(141, 255, 50)
Bright Green       ${COLORS.greenBright}    rgb(191, 255, 99)
Neon Yellow        ${COLORS.yellow}    rgb(255, 232, 77)
Orange Glow        ${COLORS.orange}    rgb(255, 196, 58)
Purple Neon        ${COLORS.purple}    rgb(180, 75, 255)
Dark Background    ${COLORS.darkBg}    rgb(5, 7, 10)
Panel Background   ${COLORS.panelBg}    rgb(11, 16, 24)
UI Text            ${COLORS.text}    rgb(191, 217, 255)

## Legacy Go-Arrow theme (UIThemePack/theme-colors.json)
Screen background  #020208
Panel fill         #0F0A1F
HUD / cyan border  #33F2FF
Magenta accent     #FF33BF
Success / win      #59FF59
Fail / lose        #FF4073
`;

  fs.writeFileSync(path.join(OUT_ROOT, 'colors.txt'), colorsTxt);

  console.log(`Generated ${assets.length} sprites in ${OUT_ROOT}`);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
