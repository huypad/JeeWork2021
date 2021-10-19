// Localization is based on '@ngx-translate/core';
// Please be familiar with official documentations first => https://github.com/ngx-translate/core

import { Injectable } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

export interface Locale {
  lang: string;
  data: any;
}

const LOCALIZATION_LOCAL_STORAGE_KEY = 'language';

@Injectable({
  providedIn: 'root',
})
export class TranslationService {
  // Private properties
  private langIds: any = [];

  constructor(private translate: TranslateService) {
    // add new langIds to the list
    this.translate.addLangs(['vi']);

    // this language will be used as a fallback when a translation isn't found in the current language
    this.translate.setDefaultLang('vi');
  }

  loadTranslations(...args: Locale[]): void {
    const locales = [...args];

    locales.forEach((locale) => {
      // use setTranslation() with the third argument set to true
      // to append translations instead of replacing them
      this.translate.setTranslation(locale.lang, locale.data, true);

      this.langIds.push(locale.lang);
    });

    // add new languages to the list
    this.translate.addLangs(this.langIds);
  }

  setLanguage(lang) {
    debugger
  	if (lang) {
			this.translate.use(this.translate.getDefaultLang());
			this.translate.use(lang);
			localStorage.setItem('language', lang);
		}
  }
  /**
   * Returns selected language
   */
   public getSelectedLanguage(): Observable<any> {
    debugger
		return of(localStorage.getItem('language') || this.translate.getDefaultLang());
	}
}
