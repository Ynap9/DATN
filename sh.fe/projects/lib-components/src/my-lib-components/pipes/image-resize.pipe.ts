import { Pipe, PipeTransform } from '@angular/core';
import * as CryptoJS from 'crypto-js';

@Pipe({
    name: 'imageResize',
    standalone: false
})
export class ImageResizePipe implements PipeTransform {

    transform(uri: string, resizingType?: string, width?: number, height?: number, gravity?: string): Promise<String> {
        const urlSafeBase64 = (string) => {
            return btoa(string)
                .replace(/=/g, '')
                .replace(/\+/g, '-')
                .replace(/\//g, '_')
        }

        const config = {
            key: '81d04ff7ceb59c154647cbb173751d137ec6bf059f30b37d37ec043e4c859639',
            salt: '5a71c31c96ec7650b883aa294d89d1b38e5918bee141b977e18db43fc09552bc',
            domainResize: '',
            domainOriginal: '',
        }

        const sign = (salt, target, secret) => {
            const saltWord = CryptoJS.enc.Hex.parse(salt);
            const targetWord = CryptoJS.enc.Utf8.parse(target);
            const message = saltWord.concat(targetWord);
            const keyWord = CryptoJS.enc.Hex.parse(secret);
            const hmac = CryptoJS.HmacSHA256(message, keyWord);
            const base64 = hmac.toString(CryptoJS.enc.Base64);
            return urlSafeBase64(base64);
        }

        const resizeBody = {
            uri: uri,
            resizingType: resizingType || 'fill',
            width: (typeof width === 'number' && width > 0) ? width : 100,
            height: (typeof height === 'number' && height > 0) ? height : 100,
            gravity: gravity || 'ce',
        }

        const extensionResizes = ['jpg', 'jpeg', 'png', 'bmp'];
        const extension = uri?.split('.')?.pop();
        const imageNone = 'shared/assets/layout/images/default-media-image/image-bg-default.png';

        const resizeOnFly = async (resizeBody) => {
            const { uri, resizingType, width, height, gravity } = resizeBody;
            if (!uri) return imageNone;
            if (!extensionResizes.includes(extension)) {
                return `${config.domainOriginal}${uri}`;
            }
            const resizeType = resizingType === 'crop' ? resizingType : `rs:${resizingType}`;

            const KEY = config.key
            const SALT = config.salt
            const domain = config.domainResize
            const sourceImage = `s3:/${uri}`

            const encodedUrl = urlSafeBase64(sourceImage)

            const pathGenerate = `/${resizeType}:${width}:${height}/g:${gravity}/${encodedUrl}.${extension}`
            const signature = sign(SALT, pathGenerate, KEY);
            const urlResize: string = `${domain}/${signature}${pathGenerate}`;
            return urlResize;
        }
        return resizeOnFly(resizeBody);
    }

}
