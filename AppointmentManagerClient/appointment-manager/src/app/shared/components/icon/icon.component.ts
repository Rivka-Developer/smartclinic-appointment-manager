// src/app/shared/components/icon/icon.component.ts
// ─────────────────────────────────────────────────────────────────────────────
// רכיב אייקון SVG (Icon Component).
//
// מרנדר אייקוני Heroicons (outline) לפי שם.
// כל האייקונים מוגדרים כ-path strings ב-ICONS map.
// ה-SVG נבנה דינמית ב-getter svg ומוזרק ל-DOM עם innerHTML.
//
// שימוש: <app-icon name="check" [size]="16" />
//         <app-icon name="warning" [size]="20" [strokeWidth]="1.5" />
//
// אבטחה: DomSanitizer.bypassSecurityTrustHtml משמש כי Angular חוסם innerHTML.
// הקוד בטוח כי ה-markup נבנה מ-ICONS map פנימי ולא מקלט משתמש.
// ─────────────────────────────────────────────────────────────────────────────

import { Component, Input, inject , ChangeDetectionStrategy} from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

/**
 * מילון אייקונים: שם → תוכן ה-<path> של SVG.
 * כל ערך הוא ה-innerHTML הפנימי של ה-SVG (ללא תג ה-<svg> עצמו).
 * מקור: Heroicons v2 (https://heroicons.com) – סגנון outline.
 */
const ICONS: Record<string, string> = {
  clipboard:  `<path stroke-linecap="round" stroke-linejoin="round" d="M9 5H7a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2h-2M9 5a2 2 0 0 0 2 2h2a2 2 0 0 0 2-2M9 5a2 2 0 0 0 2-2h2a2 2 0 0 0 2 2"/>`,
  star:       `<path stroke-linecap="round" stroke-linejoin="round" d="M11.48 3.499a.562.562 0 0 1 1.04 0l2.125 5.111a.563.563 0 0 0 .475.345l5.518.442c.499.04.701.663.321.988l-4.204 3.602a.563.563 0 0 0-.182.557l1.285 5.385a.562.562 0 0 1-.84.61l-4.725-2.885a.562.562 0 0 0-.586 0L6.982 20.54a.562.562 0 0 1-.84-.61l1.285-5.386a.562.562 0 0 0-.182-.557l-4.204-3.602a.562.562 0 0 1 .321-.988l5.518-.442a.563.563 0 0 0 .475-.345L11.48 3.5Z"/>`,
  phone:      `<path stroke-linecap="round" stroke-linejoin="round" d="M2.25 6.75c0 8.284 6.716 15 15 15h2.25a2.25 2.25 0 0 0 2.25-2.25v-1.372c0-.516-.351-.966-.852-1.091l-4.423-1.106c-.44-.11-.902.055-1.173.417l-.97 1.293c-.282.376-.769.542-1.21.38a12.035 12.035 0 0 1-7.143-7.143c-.162-.441.004-.928.38-1.21l1.293-.97c.363-.271.527-.734.417-1.173L6.963 3.102a1.125 1.125 0 0 0-1.091-.852H4.5A2.25 2.25 0 0 0 2.25 4.5v2.25Z"/>`,
  mail:       `<path stroke-linecap="round" stroke-linejoin="round" d="M21.75 6.75v10.5a2.25 2.25 0 0 1-2.25 2.25h-15a2.25 2.25 0 0 1-2.25-2.25V6.75m19.5 0A2.25 2.25 0 0 0 19.5 4.5h-15a2.25 2.25 0 0 0-2.25 2.25m19.5 0v.243a2.25 2.25 0 0 1-1.07 1.916l-7.5 4.615a2.25 2.25 0 0 1-2.36 0L3.32 8.91a2.25 2.25 0 0 1-1.07-1.916V6.75"/>`,
  trash:      `<path stroke-linecap="round" stroke-linejoin="round" d="m14.74 9-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 0 1-2.244 2.077H8.084a2.25 2.25 0 0 1-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 0 0-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 0 1 3.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 0 0-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 0 0-7.5 0"/>`,
  warning:    `<path stroke-linecap="round" stroke-linejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z"/>`,
  calendar:   `<path stroke-linecap="round" stroke-linejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 0 1 2.25-2.25h13.5A2.25 2.25 0 0 1 21 7.5v11.25m-18 0A2.25 2.25 0 0 0 5.25 21h13.5A2.25 2.25 0 0 0 21 18.75m-18 0v-7.5A2.25 2.25 0 0 1 5.25 9h13.5A2.25 2.25 0 0 1 21 11.25v7.5"/>`,
  clock:      `<path stroke-linecap="round" stroke-linejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"/>`,
  check:      `<path stroke-linecap="round" stroke-linejoin="round" d="m4.5 12.75 6 6 9-13.5"/>`,
  'x-mark':   `<path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6M6 6l12 12"/>`,
  info:         `<path stroke-linecap="round" stroke-linejoin="round" d="m11.25 11.25.041-.02a.75.75 0 0 1 1.063.852l-.708 2.836a.75.75 0 0 0 1.063.853l.041-.021M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Zm-9-3.75h.008v.008H12V8.25Z"/>`,
  'pencil-square': `<path stroke-linecap="round" stroke-linejoin="round" d="m16.862 4.487 1.687-1.688a1.875 1.875 0 1 1 2.652 2.652L10.582 16.07a4.5 4.5 0 0 1-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 0 1 1.13-1.897l8.932-8.931Zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0 1 15.75 21H5.25A2.25 2.25 0 0 1 3 18.75V8.25A2.25 2.25 0 0 1 5.25 6H10"/>`,
  search: `<path stroke-linecap="round" stroke-linejoin="round" d="m21 21-5.197-5.197m0 0A7.5 7.5 0 1 0 5.196 5.196a7.5 7.5 0 0 0 10.607 10.607Z"/>`,
  user:   `<path stroke-linecap="round" stroke-linejoin="round" d="M15.75 6a3.75 3.75 0 1 1-7.5 0 3.75 3.75 0 0 1 7.5 0ZM4.501 20.118a7.5 7.5 0 0 1 14.998 0A17.933 17.933 0 0 1 12 21.75c-2.676 0-5.216-.584-7.499-1.632Z"/>`,
};

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-icon',
  standalone: true,
  // innerHTML מוזרק ל-<span> – Angular מרנדר כ-SafeHtml
  template: `<span [innerHTML]="svg"></span>`,
  styles: [`:host { display: inline-flex; align-items: center; vertical-align: middle; line-height: 1; } svg { display: block; }`]
})
export class IconComponent {
  /** שם האייקון – חייב להיות מפתח ב-ICONS map */
  @Input() name = 'check';

  /** גודל האייקון בפיקסלים (width ו-height של ה-SVG) */
  @Input() size: number | string = 18;

  /** עובי קו ה-SVG (stroke-width); ברירת מחדל 2 */
  @Input() strokeWidth: number | string = 2;

  private sanitizer = inject(DomSanitizer);

  /**
   * בונה את ה-SVG המלא ומחזיר אותו כ-SafeHtml.
   *
   * אם name לא נמצא ב-ICONS → path ריק (SVG ריק, לא שגיאה).
   * bypassSecurityTrustHtml נדרש כי Angular חוסם innerHTML של HTML דינמי.
   * בטוח כאן כי המקור הוא ICONS הפנימי ולא קלט משתמש.
   */
  get svg(): SafeHtml {
    const path = ICONS[this.name] ?? ''; // '' = אייקון לא נמצא → SVG ריק
    const markup = `<svg xmlns="http://www.w3.org/2000/svg" width="${this.size}" height="${this.size}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="${this.strokeWidth}" stroke-linecap="round" stroke-linejoin="round">${path}</svg>`;
    return this.sanitizer.bypassSecurityTrustHtml(markup);
  }
}
