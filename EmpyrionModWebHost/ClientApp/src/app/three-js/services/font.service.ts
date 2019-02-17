import { Injectable } from '@angular/core';

import * as THREE from 'three';

class FontCache {
  name: string;
  font: THREE.Font
}

@Injectable({
  providedIn: 'root'
})
export class FontService {
  FontLoader = new THREE.FontLoader();

  mFonts: FontCache[] = [];

  async getFont(name: string) {
    let _this = this;

    return new Promise<THREE.Font>((resolve, reject) => {
      let Found = _this.mFonts.find(F => F.name == name);
      if (Found) resolve(Found.font);

      this.FontLoader.load("assets/Fonts/" + name + ".json", function (response) {
        _this.mFonts.push({ name: name, font: response });
        resolve(response);
      })
    });
  }

}
